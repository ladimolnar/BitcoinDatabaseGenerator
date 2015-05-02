//-----------------------------------------------------------------------
// <copyright file="SourceDataPipeline.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System.Collections.Concurrent;
    using System.Data;
    using BitcoinDataLayerAdoNet.DataSets;
    using DBData = BitcoinDataLayerAdoNet.Data;
    using ParserData = BitcoinBlockchain.Data;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "DataSet instances do not need to be disposed.")]
    public class SourceDataPipeline
    {
        public const int RowsLimit = 12000;

        private readonly object blockLockObject;
        private readonly object bitcoinTransactionLockObject;
        private readonly object transactionInputLockObject;
        private readonly object transactionOutputLockObject;

        private BlockDataSet blockDataSet;
        private BitcoinTransactionDataSet bitcoinTransactionDataSet;
        private TransactionInputDataSet transactionInputDataSet;
        private TransactionInputSourceDataSet transactionInputSourceDataSet;
        private TransactionOutputDataSet transactionOutputDataSet;

        private ConcurrentQueue<DataTable> availableDataTables;

        public SourceDataPipeline()
        {
            this.blockLockObject = new object();
            this.bitcoinTransactionLockObject = new object();
            this.transactionInputLockObject = new object();
            this.transactionOutputLockObject = new object();

            this.blockDataSet = new BlockDataSet();
            this.bitcoinTransactionDataSet = new BitcoinTransactionDataSet();
            this.transactionInputDataSet = new TransactionInputDataSet();
            this.transactionInputSourceDataSet = new TransactionInputSourceDataSet();
            this.transactionOutputDataSet = new TransactionOutputDataSet();

            this.availableDataTables = new ConcurrentQueue<DataTable>();
        }

        public bool HasDataSourceAvailable { get; set; }

        public bool TryGetNextAvailableDataSource(out DataTable dataTable)
        {
            return this.availableDataTables.TryDequeue(out dataTable);
        }

        public void FillBlockchainPipeline(int currentBlockFileId, ParserData.Block parserBlock, DatabaseIdSegmentManager databaseIdSegmentManager)
        {
            long blockId = databaseIdSegmentManager.GetNextBlockId();

            lock (this.blockLockObject)
            {
                this.blockDataSet.Block.AddBlockRow(
                    blockId,
                    currentBlockFileId,
                    (int)parserBlock.BlockHeader.BlockVersion,
                    parserBlock.BlockHeader.BlockHash.ToArray(),
                    parserBlock.BlockHeader.PreviousBlockHash.ToArray(),
                    parserBlock.BlockHeader.BlockTimestamp);

                if (this.MakeDataTableAvailableIfLarge(this.blockDataSet.Block))
                {
                    this.blockDataSet = new BlockDataSet();
                }
            }

            lock (this.bitcoinTransactionLockObject)
            {
                foreach (ParserData.Transaction parserTransaction in parserBlock.Transactions)
                {
                    long bitcoinTransactionId = databaseIdSegmentManager.GetNextTransactionId();

                    this.bitcoinTransactionDataSet.BitcoinTransaction.AddBitcoinTransactionRow(
                        bitcoinTransactionId,
                        blockId,
                        parserTransaction.TransactionHash.ToArray(),
                        (int)parserTransaction.TransactionVersion,
                        (int)parserTransaction.TransactionLockTime);
                }

                if (this.MakeDataTableAvailableIfLarge(this.bitcoinTransactionDataSet.BitcoinTransaction))
                {
                    this.bitcoinTransactionDataSet = new BitcoinTransactionDataSet();
                }
            }

            lock (this.transactionInputLockObject)
            {
                databaseIdSegmentManager.ResetNextTransactionId();
                foreach (ParserData.Transaction parserTransaction in parserBlock.Transactions)
                {
                    long bitcoinTransactionId = databaseIdSegmentManager.GetNextTransactionId();

                    foreach (ParserData.TransactionInput parserTransactionInput in parserTransaction.Inputs)
                    {
                        long transactionInput = databaseIdSegmentManager.GetNextTransactionInputId();

                        this.transactionInputDataSet.TransactionInput.AddTransactionInputRow(
                            transactionInput,
                            bitcoinTransactionId,
                            DBData.TransactionInput.SourceTransactionOutputIdUnknown);

                        this.transactionInputSourceDataSet.TransactionInputSource.AddTransactionInputSourceRow(
                            transactionInput,
                            parserTransactionInput.SourceTransactionHash.ToArray(),
                            (int)parserTransactionInput.SourceTransactionOutputIndex);
                    }

                    if (this.MakeDataTableAvailableIfLarge(this.transactionInputDataSet.TransactionInput))
                    {
                        this.transactionInputDataSet = new TransactionInputDataSet();
                    }

                    if (this.MakeDataTableAvailableIfLarge(this.transactionInputSourceDataSet.TransactionInputSource))
                    {
                        this.transactionInputSourceDataSet = new TransactionInputSourceDataSet();
                    }
                }
            }

            lock (this.transactionOutputLockObject)
            {
                databaseIdSegmentManager.ResetNextTransactionId();
                foreach (ParserData.Transaction parserTransaction in parserBlock.Transactions)
                {
                    long bitcoinTransactionId = databaseIdSegmentManager.GetNextTransactionId();

                    for (int outputIndex = 0; outputIndex < parserTransaction.Outputs.Count; outputIndex++)
                    {
                        ParserData.TransactionOutput parserTransactionOutput = parserTransaction.Outputs[outputIndex];
                        long transactionOutputId = databaseIdSegmentManager.GetNextTransactionOutputId();

                        this.transactionOutputDataSet.TransactionOutput.AddTransactionOutputRow(
                            transactionOutputId,
                            bitcoinTransactionId,
                            outputIndex,
                            (decimal)parserTransactionOutput.OutputValueSatoshi / DatabaseGenerator.BtcToSatoshi,
                            parserTransactionOutput.OutputScript.ToArray());
                    }
                }

                if (this.MakeDataTableAvailableIfLarge(this.transactionOutputDataSet.TransactionOutput))
                {
                    this.transactionOutputDataSet = new TransactionOutputDataSet();
                }
            }
        }

        public void Flush()
        {
            this.availableDataTables.Enqueue(this.blockDataSet.Block);
            this.availableDataTables.Enqueue(this.bitcoinTransactionDataSet.BitcoinTransaction);
            this.availableDataTables.Enqueue(this.transactionInputDataSet.TransactionInput);
            this.availableDataTables.Enqueue(this.transactionInputSourceDataSet.TransactionInputSource);
            this.availableDataTables.Enqueue(this.transactionOutputDataSet.TransactionOutput);
        }

        private bool MakeDataTableAvailableIfLarge(DataTable dataTable)
        {
            if (dataTable.Rows.Count > RowsLimit)
            {
                this.availableDataTables.Enqueue(dataTable);
                return true;
            }

            return false;
        }
    }
}
