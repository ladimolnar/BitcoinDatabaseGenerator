//-----------------------------------------------------------------------
// <copyright file="BitcoinDataLayer.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.Data;
    using ZeroHelpers;

    public partial class BitcoinDataLayer : IDisposable
    {
        /// <summary>
        /// The default timeout in seconds that is used for each SQL command.
        /// </summary>
        public const int DefaultDbCommandTimeout = 1200;

        private readonly SqlConnection sqlConnection;
        private readonly AdoNetLayer adoNetLayer;

        public BitcoinDataLayer(string connectionString, int commandTimeout = DefaultDbCommandTimeout)
        {
            this.sqlConnection = new SqlConnection(connectionString);
            this.sqlConnection.Open();

            this.adoNetLayer = new AdoNetLayer(this.sqlConnection, commandTimeout);
        }

        /// <summary>
        /// Implements the IDisposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string GetLastKnownBlockchainFileName()
        {
            return AdoNetLayer.ConvertDbValue<string>(this.adoNetLayer.ExecuteScalar("SELECT TOP 1 [FileName] FROM BlockFile ORDER BY BlockFileId DESC"));
        }

        public async Task DeleteLastBlockFileAsync()
        {
            await this.adoNetLayer.ExecuteStatementNoResultAsync(@"
                DELETE TransactionOutput FROM TransactionOutput
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                INNER JOIN BlockFile ON BlockFile.BlockFileId = Block.BlockFileId
                WHERE BlockFile.BlockFileId = (SELECT MAX(BlockFileId) from BlockFile)");

            await this.adoNetLayer.ExecuteStatementNoResultAsync(@"
                DELETE TransactionInputSource FROM TransactionInputSource
                INNER JOIN TransactionInput ON TransactionInput.TransactionInputId = TransactionInputSource.TransactionInputId
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                INNER JOIN BlockFile ON BlockFile.BlockFileId = Block.BlockFileId
                WHERE BlockFile.BlockFileId = (SELECT MAX(BlockFileId) from BlockFile)");

            await this.adoNetLayer.ExecuteStatementNoResultAsync(@"
                DELETE TransactionInput FROM TransactionInput
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                INNER JOIN BlockFile ON BlockFile.BlockFileId = Block.BlockFileId
                WHERE BlockFile.BlockFileId = (SELECT MAX(BlockFileId) from BlockFile)");

            await this.adoNetLayer.ExecuteStatementNoResultAsync(@"
                DELETE BitcoinTransaction FROM BitcoinTransaction
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                INNER JOIN BlockFile ON BlockFile.BlockFileId = Block.BlockFileId
                WHERE BlockFile.BlockFileId = (SELECT MAX(BlockFileId) from BlockFile)");

            await this.adoNetLayer.ExecuteStatementNoResultAsync(@"
                DELETE Block FROM Block
                INNER JOIN BlockFile ON BlockFile.BlockFileId = Block.BlockFileId
                WHERE BlockFile.BlockFileId = (SELECT MAX(BlockFileId) from BlockFile)");

            await this.adoNetLayer.ExecuteStatementNoResultAsync(@"
                DELETE FROM BlockFile WHERE BlockFile.BlockFileId = (SELECT MAX(BlockFileId) from BlockFile)");
        }

        public long GetTransactionSourceOutputRowsToUpdate()
        {
            const string sqlCountRowsToUpdateCommand = @"SELECT COUNT(1) FROM TransactionInput WHERE SourceTransactionOutputId = -1";

            return AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar(sqlCountRowsToUpdateCommand));
        }

        public long UpdateNullTransactionSources()
        {
            const string sqlUpdateNullSourceCommand = @"
                UPDATE TransactionInput SET SourceTransactionOutputId = NULL
                FROM TransactionInput 
                INNER JOIN TransactionInputSource ON TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                WHERE TransactionInputSource.SourceTransactionOutputIndex = -1";

            return this.adoNetLayer.ExecuteStatementNoResult(sqlUpdateNullSourceCommand);
        }

        public int UpdateTransactionSourceBatch()
        {
            const string sqlUpdateSourceCommand = @"
                UPDATE TransactionInput 
                SET SourceTransactionOutputId = TransactionOutput.TransactionOutputId
                FROM (
                    SELECT TOP 1000000
                        TransactionInput.TransactionInputId
                    FROM TransactionInput
                    WHERE TransactionInput.SourceTransactionOutputId = -1
                ) AS T1
                INNER JOIN TransactionInput ON TransactionInput.TransactionInputId = T1.TransactionInputId
                INNER JOIN TransactionInputSource ON TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                INNER JOIN BitcoinTransaction AS BitcoinTransactionSource ON BitcoinTransactionSource.TransactionHash = TransactionInputSource.SourceTransactionHash
                INNER JOIN TransactionOutput ON 
                    TransactionOutput.BitcoinTransactionId = BitcoinTransactionSource.BitcoinTransactionId 
                    AND TransactionOutput.OutputIndex = TransactionInputSource.SourceTransactionOutputIndex 
                WHERE TransactionInputSource.SourceTransactionOutputIndex != -1";

            return this.adoNetLayer.ExecuteStatementNoResult(sqlUpdateSourceCommand);
        }

        public void GetMaximumIdValues(out int blockFileId, out long blockId, out long bitcoinTransactionId, out long transactionInputId, out long transactionOutputId)
        {
            blockFileId = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BlockFileId) from BlockFile"), -1);
            blockId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BlockId) from Block"), -1);
            bitcoinTransactionId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BitcoinTransactionId) from BitcoinTransaction"), -1);
            transactionInputId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(TransactionInputId) from TransactionInput"), -1);
            transactionOutputId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(TransactionOutputId) from TransactionOutput"), -1);
        }

        public void GetDatabaseEntitiesCount(out int blockFileCount, out int blockCount, out int transactionCount, out int transactionInputCount, out int transactionOutputCount)
        {
            blockFileCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1) from BlockFile"));
            blockCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1)  from Block"));
            transactionCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1)  from BitcoinTransaction"));
            transactionInputCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1) from TransactionInput"));
            transactionOutputCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1)  from TransactionOutput"));
        }

        public void AddBlockFile(BlockchainFile blockchainFile)
        {
            this.adoNetLayer.ExecuteStatementNoResult(
                "INSERT INTO BlockFile(BlockFileId, FileName) VALUES (@BlockFileId, @FileName)",
                AdoNetLayer.CreateInputParameter("@BlockFileId", SqlDbType.Int, blockchainFile.BlockFileId),
                AdoNetLayer.CreateInputParameter("@FileName", SqlDbType.NVarChar, blockchainFile.FileName));
        }

        public void AddBlock(Block block)
        {
            this.adoNetLayer.ExecuteStatementNoResult(
                "INSERT INTO Block(BlockId, BlockFileId, BlockVersion, BlockHash, PreviousBlockHash, BlockTimestamp) VALUES (@BlockId, @BlockFileId, @BlockVersion, @BlockHash, @PreviousBlockHash, @BlockTimestamp)",
                AdoNetLayer.CreateInputParameter("@BlockId", SqlDbType.Int, block.BlockId),
                AdoNetLayer.CreateInputParameter("@BlockFileId", SqlDbType.Int, block.BlockFileId),
                AdoNetLayer.CreateInputParameter("@BlockVersion", SqlDbType.Int, block.BlockVersion),
                AdoNetLayer.CreateStoredParameter("@BlockHash", SqlDbType.VarBinary, 32, block.BlockHash.ToArray()),
                AdoNetLayer.CreateStoredParameter("@PreviousBlockHash", SqlDbType.VarBinary, 32, block.PreviousBlockHash.ToArray()),
                AdoNetLayer.CreateInputParameter("@BlockTimestamp", SqlDbType.DateTime, block.BlockTimestamp));
        }

        /// <summary>
        /// Bulk inserts in batches all given transactions.
        /// </summary>
        /// <param name="bitcoinTransactions">
        /// The transactions that will be inserted.
        /// </param>
        /// <returns>
        /// The number of rows that were inserted.
        /// </returns>
        public int AddTransactions(IEnumerable<BitcoinTransaction> bitcoinTransactions)
        {
            int rowsInserted = 0;

            foreach (string insertStatement in this.GetTransactionsInsertStatements(bitcoinTransactions))
            {
                rowsInserted += this.adoNetLayer.ExecuteStatementNoResult(insertStatement);
            }

            return rowsInserted;
        }

        /// <summary>
        /// Bulk inserts in batches all given transaction inputs.
        /// </summary>
        /// <param name="transactionInputs">
        /// The transaction inputs that will be inserted.
        /// </param>
        /// <returns>
        /// The number of rows that were inserted.
        /// </returns>
        public int AddTransactionInputs(IEnumerable<TransactionInput> transactionInputs)
        {
            int rowsInserted = 0;

            foreach (string insertStatement in this.GetTransactionInputsInsertStatements(transactionInputs))
            {
                rowsInserted += this.adoNetLayer.ExecuteStatementNoResult(insertStatement);
            }

            return rowsInserted;
        }

        /// <summary>
        /// Bulk inserts in batches all given transaction input sources.
        /// </summary>
        /// <param name="transactionInputSources">
        /// The transaction input sources that will be inserted.
        /// </param>
        /// <returns>
        /// The number of rows that were inserted.
        /// </returns>
        public int AddTransactionInputSources(IEnumerable<TransactionInputSource> transactionInputSources)
        {
            int rowsInserted = 0;

            foreach (string insertStatement in this.GetTransactionInputSourcesInsertStatements(transactionInputSources))
            {
                rowsInserted += this.adoNetLayer.ExecuteStatementNoResult(insertStatement);
            }

            return rowsInserted;
        }

        /// <summary>
        /// Bulk inserts in batches all given transaction inputs.
        /// </summary>
        /// <param name="transactionOutputs">
        /// The transaction outputs that will be inserted.
        /// </param>
        /// <returns>
        /// The number of rows that were inserted.
        /// </returns>
        public int AddTransactionOutputs(IEnumerable<TransactionOutput> transactionOutputs)
        {
            int rowsInserted = 0;

            foreach (string insertStatement in this.GetTransactionOutputsInsertStatements(transactionOutputs))
            {
                rowsInserted += this.adoNetLayer.ExecuteStatementNoResult(insertStatement);
            }

            return rowsInserted;
        }

        public SummaryBlockDataSet GetSummaryBlockDataSet()
        {
            SummaryBlockDataSet summaryBlockDataSet = new SummaryBlockDataSet();
            try
            {
                // @@@ Refactor to use GetDataSet?
                this.adoNetLayer.FillDataSetFromStatement(summaryBlockDataSet, "SELECT BlockId, BlockHash, PreviousBlockHash FROM Block");
            }
            catch (Exception)
            {
                summaryBlockDataSet.Dispose();
                throw;
            }

            return summaryBlockDataSet;
        }

        //// @@@ delete GetUnspentOutputsDataSet if not used.
        //// @@@ delete UnspentOutputsDataSet if not used.

        /// <summary>
        /// Retrieves information about all unspent outputs.
        /// </summary>
        /// <returns>
        /// A typed dataset of type <see cref="UnspentOutputsDataSet " /> 
        /// containing information about all unspent outputs.
        /// The rows are ordered by the BitcoinTransactionId.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "DataSet instances do not have to be disposed.")]
        public UnspentOutputsDataSet GetUnspentOutputsDataSet()
        {
            const string selectUnspentOutputs = @"
                SELECT 
                    SourceTransaction.BitcoinTransactionId,
                    SourceTransaction.TransactionHash,
                    TransactionOutput.TransactionOutputId,
                    TransactionOutput.OutputIndex
                FROM TransactionOutput 
                INNER JOIN BitcoinTransaction SourceTransaction ON SourceTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                LEFT OUTER JOIN TransactionInput ON TransactionInput.SourceTransactionOutputId = TransactionOutput.TransactionOutputId
                WHERE TransactionInput.TransactionInputId IS NULL
                ORDER BY SourceTransaction.BitcoinTransactionId";

            UnspentOutputsDataSet unspentOutputsDataSet = new UnspentOutputsDataSet();
            this.adoNetLayer.FillDataSetFromStatement(unspentOutputsDataSet, selectUnspentOutputs);
            return unspentOutputsDataSet;
        }

        public void DeleteBlocks(IEnumerable<long> blocksToDelete)
        {
            foreach (IEnumerable<long> batch in blocksToDelete.GetBatches(100))
            {
                this.DeleteBatchOfBlocks(batch);
            }
        }

        /// <summary>
        /// Applied after a series of blocks were deleted. 
        /// This method will update the block IDs so that they are forming a consecutive sequence.
        /// </summary>
        /// <param name="blocksDeleted">
        /// The list of IDs for blocks that were deleted.
        /// </param>
        public void CompactBlockIds(IEnumerable<long> blocksDeleted)
        {
            const string sqlCommandUpdateBlockBlockIdSection = @"UPDATE Block SET BlockId = BlockId - @DecrementAmount WHERE BlockId BETWEEN @BlockId1 AND @BlockId2";
            const string sqlCommandUpdateTransactionBlockIdSection = @"UPDATE BitcoinTransaction SET BlockId = BlockId - @DecrementAmount WHERE BlockId BETWEEN @BlockId1 AND @BlockId2";

            const string sqlCommandUpdateBlockBlockIdLastSection = @"UPDATE Block SET BlockId = BlockId - @DecrementAmount WHERE BlockId > @BlockId";
            const string sqlCommandUpdateTransactionBlockIdLastSection = @"UPDATE BitcoinTransaction SET BlockId = BlockId - @DecrementAmount WHERE BlockId > @BlockId";

            List<long> orderedBlocksDeleted = blocksDeleted.OrderBy(id => id).ToList();

            for (int i = 0; i < orderedBlocksDeleted.Count - 1; i++)
            {
                long blockId1 = orderedBlocksDeleted[i];
                long blockId2 = orderedBlocksDeleted[i + 1];

                this.adoNetLayer.ExecuteStatementNoResult(
                    sqlCommandUpdateBlockBlockIdSection,
                    AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.Int, i),
                    AdoNetLayer.CreateInputParameter("@BlockId1", SqlDbType.BigInt, blockId1),
                    AdoNetLayer.CreateInputParameter("@BlockId2", SqlDbType.BigInt, blockId2));

                this.adoNetLayer.ExecuteStatementNoResult(
                    sqlCommandUpdateTransactionBlockIdSection,
                    AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.Int, i),
                    AdoNetLayer.CreateInputParameter("@BlockId1", SqlDbType.BigInt, blockId1),
                    AdoNetLayer.CreateInputParameter("@BlockId2", SqlDbType.BigInt, blockId2));
            }

            long blockId = orderedBlocksDeleted[orderedBlocksDeleted.Count - 1];

            this.adoNetLayer.ExecuteStatementNoResult(
                sqlCommandUpdateBlockBlockIdLastSection,
                AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.BigInt, orderedBlocksDeleted.Count),
                AdoNetLayer.CreateInputParameter("@BlockId", SqlDbType.BigInt, blockId));

            this.adoNetLayer.ExecuteStatementNoResult(
                sqlCommandUpdateTransactionBlockIdLastSection,
                AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.BigInt, orderedBlocksDeleted.Count),
                AdoNetLayer.CreateInputParameter("@BlockId", SqlDbType.BigInt, blockId));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.sqlConnection.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "DataSet instances do not have to be disposed.")]
        private T GetDataSet<T>(string sqlCommandText, params SqlParameter[] sqlParameters) where T : DataSet, new()
        {
            T dataSet = new T() { Locale = CultureInfo.InvariantCulture };
            this.adoNetLayer.FillDataSetFromStatement(dataSet, sqlCommandText, sqlParameters);
            return dataSet;
        }

        private void DeleteBatchOfBlocks(IEnumerable<long> batchOfBlockIds)
        {
            string inClause = "(" + string.Join(", ", batchOfBlockIds) + ")";

            this.adoNetLayer.ExecuteStatementNoResult(@"
                DELETE TransactionOutput FROM TransactionOutput
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockId IN " + inClause);

            this.adoNetLayer.ExecuteStatementNoResult(@"
                DELETE TransactionInputSource FROM TransactionInputSource
                INNER JOIN TransactionInput ON TransactionInput.TransactionInputId = TransactionInputSource.TransactionInputId
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockId IN " + inClause);

            this.adoNetLayer.ExecuteStatementNoResult(@"
                DELETE TransactionInput FROM TransactionInput
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockId IN " + inClause);

            this.adoNetLayer.ExecuteStatementNoResult(@"
                DELETE BitcoinTransaction FROM BitcoinTransaction
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockId IN " + inClause);

            this.adoNetLayer.ExecuteStatementNoResult(@"DELETE FROM Block WHERE Block.BlockId IN " + inClause);
        }

        private IEnumerable<string> GetTransactionsInsertStatements(IEnumerable<BitcoinTransaction> bitcoinTransactions)
        {
            int rowCount = 0;
            StringBuilder sb = new StringBuilder();

            foreach (BitcoinTransaction bitcoinTransaction in bitcoinTransactions)
            {
                if (rowCount == 0)
                {
                    sb.Append("INSERT INTO BitcoinTransaction(BitcoinTransactionId, BlockId, TransactionHash, TransactionVersion, TransactionLockTime) VALUES ");
                }
                else if (rowCount > 0)
                {
                    sb.Append(", ");
                }

                sb.AppendFormat(
                    "({0}, {1}, 0x{2}, {3}, {4})",
                    bitcoinTransaction.BitcoinTransactionId,
                    bitcoinTransaction.BlockId,
                    bitcoinTransaction.TransactionHash.ToString(),
                    bitcoinTransaction.TransactionVersion,
                    bitcoinTransaction.TransactionLockTime);
                rowCount++;

                if (rowCount == 1000)
                {
                    yield return sb.ToString();

                    sb.Clear();
                    rowCount = 0;
                }
            }

            if (rowCount > 0)
            {
                yield return sb.ToString();
            }
        }

        private IEnumerable<string> GetTransactionInputsInsertStatements(IEnumerable<TransactionInput> transactionInputs)
        {
            int rowCount = 0;
            StringBuilder sb = new StringBuilder();

            foreach (TransactionInput transactionInput in transactionInputs)
            {
                if (rowCount == 0)
                {
                    sb.Append("INSERT INTO TransactionInput(TransactionInputId, BitcoinTransactionId, SourceTransactionOutputId) VALUES ");
                }
                else if (rowCount > 0)
                {
                    sb.Append(", ");
                }

                sb.AppendFormat(
                    "({0}, {1}, {2})",
                    transactionInput.TransactionInputId,
                    transactionInput.BitcoinTransactionId,
                    transactionInput.SourceTransactionOutputId != null ? transactionInput.SourceTransactionOutputId.ToString() : "null");

                rowCount++;

                if (rowCount == 1000)
                {
                    yield return sb.ToString();

                    sb.Clear();
                    rowCount = 0;
                }
            }

            if (rowCount > 0)
            {
                yield return sb.ToString();
            }
        }

        private IEnumerable<string> GetTransactionInputSourcesInsertStatements(IEnumerable<TransactionInputSource> transactionInputSources)
        {
            int rowCount = 0;
            StringBuilder sb = new StringBuilder();

            foreach (TransactionInputSource transactionInputSource in transactionInputSources)
            {
                if (rowCount == 0)
                {
                    sb.Append("INSERT INTO TransactionInputSource(TransactionInputId, SourceTransactionHash, SourceTransactionOutputIndex) VALUES ");
                }
                else if (rowCount > 0)
                {
                    sb.Append(", ");
                }

                sb.AppendFormat(
                    "({0}, 0x{1}, {2})",
                    transactionInputSource.TransactionInputId,
                    transactionInputSource.SourceTransactionHash,
                    transactionInputSource.SourceTransactionOutputIndex);

                rowCount++;

                if (rowCount == 1000)
                {
                    yield return sb.ToString();

                    sb.Clear();
                    rowCount = 0;
                }
            }

            if (rowCount > 0)
            {
                yield return sb.ToString();
            }
        }

        private IEnumerable<string> GetTransactionOutputsInsertStatements(IEnumerable<TransactionOutput> transactionOutputs)
        {
            int rowCount = 0;
            StringBuilder sb = new StringBuilder();

            foreach (TransactionOutput transactionOutput in transactionOutputs)
            {
                if (rowCount == 0)
                {
                    sb.Append("INSERT INTO TransactionOutput(TransactionOutputId, BitcoinTransactionId, OutputIndex, OutputValueBtc, OutputScript) VALUES ");
                }
                else if (rowCount > 0)
                {
                    sb.Append(", ");
                }

                sb.AppendFormat(
                    "({0}, {1}, {2}, {3}, 0x{4})",
                    transactionOutput.TransactionOutputId,
                    transactionOutput.BitcoinTransactionId,
                    transactionOutput.OutputIndex,
                    transactionOutput.OutputValueBtc,
                    transactionOutput.OutputScript.ToString());

                rowCount++;

                if (rowCount == 1000)
                {
                    yield return sb.ToString();

                    sb.Clear();
                    rowCount = 0;
                }
            }

            if (rowCount > 0)
            {
                yield return sb.ToString();
            }
        }
    }
}
