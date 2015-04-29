//-----------------------------------------------------------------------
// <copyright file="DatabaseGenerator.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BitcoinBlockchain.Parser;
    using BitcoinDataLayerAdoNet;
    using ZeroHelpers;
    using ZeroHelpers.Exceptions;
    using DBData = BitcoinDataLayerAdoNet.Data;
    using ParserData = BitcoinBlockchain.Data;

    public class DatabaseGenerator
    {
        private const decimal BtcToSatoshi = 100000000;

        private readonly DatabaseGeneratorParameters parameters;
        private readonly DatabaseConnection databaseConnection;
        private readonly ProcessingStatistics processingStatistics;
        private readonly Stopwatch currentBlockchainFileStopwatch;

        private int lastReportedPercentage;
        private string currentBlockchainFile;

        public DatabaseGenerator(DatabaseGeneratorParameters parameters)
        {
            this.parameters = parameters;
            this.databaseConnection = new DatabaseConnection(this.parameters.SqlServerName, this.parameters.DatabaseName, this.parameters.SqlUserName, this.parameters.SqlPassword);
            this.processingStatistics = new ProcessingStatistics();
            this.currentBlockchainFileStopwatch = new Stopwatch();
        }

        public async Task GenerateAndPopulateDatabase()
        {
            bool newDatabase = false;

            this.processingStatistics.PreprocessingStarting();

            if (this.parameters.SkipDbManagement == false)
            {
                newDatabase = this.PrepareDatabase();
            }

            string lastKnownBlockchainFileName = null;
            lastKnownBlockchainFileName = this.GetLastKnownBlockchainFileName();

            if (lastKnownBlockchainFileName != null)
            {
                Console.WriteLine("Deleting from database information about blockchain file: {0}", lastKnownBlockchainFileName);
                await this.DeleteLastBlockFileAsync();
            }

            Console.WriteLine();
            await this.TransferBlockchainDataAsync(lastKnownBlockchainFileName, newDatabase);

            this.processingStatistics.PostProcessingStarting();

            Console.WriteLine();

            this.DeleteOrphanBlocks();

            if (newDatabase)
            {
                this.CreateDatabaseIndexes();
            }

            this.UpdateTransactionSourceOutputId();

            this.processingStatistics.ProcessingCompleted();

            this.processingStatistics.DisplayStatistics();
            this.DisplayDatabaseStatistics();
        }

        private static BlockInfo ConvertParserBlockToBlockInfo(DatabaseIdManager databaseIdManager, ParserData.Block parserBlock)
        {
            BlockInfo blockInfo = new BlockInfo();

            long blockId = databaseIdManager.GetNextBlockId();

            blockInfo.Block = new DBData.Block(
                blockId,
                databaseIdManager.CurrentBlockFileId,
                (int)parserBlock.BlockHeader.BlockVersion,
                parserBlock.BlockHeader.BlockHash,
                parserBlock.BlockHeader.PreviousBlockHash,
                parserBlock.BlockHeader.BlockTimestamp);

            foreach (ParserData.Transaction parserTransaction in parserBlock.Transactions)
            {
                long bitcoinTransactionId = databaseIdManager.GetNextTransactionId();

                blockInfo.BitcoinTransactions.Add(new DBData.BitcoinTransaction(
                    bitcoinTransactionId,
                    blockId,
                    parserTransaction.TransactionHash,
                    (int)parserTransaction.TransactionVersion,
                    (int)parserTransaction.TransactionLockTime));

                foreach (ParserData.TransactionInput parserTransactionInput in parserTransaction.Inputs)
                {
                    long transactionInput = databaseIdManager.GetNextTransactionInputId();

                    blockInfo.TransactionInputs.Add(
                        new DBData.TransactionInput(
                            transactionInput,
                            bitcoinTransactionId,
                            DBData.TransactionInput.SourceTransactionOutputIdUnknown));

                    blockInfo.TransactionInputSources.Add(
                        new DBData.TransactionInputSource(
                            transactionInput,
                            parserTransactionInput.SourceTransactionHash,
                            (int)parserTransactionInput.SourceTransactionOutputIndex));
                }

                for (int outputIndex = 0; outputIndex < parserTransaction.Outputs.Count; outputIndex++)
                {
                    ParserData.TransactionOutput parserTransactionOutput = parserTransaction.Outputs[outputIndex];
                    long transactionOutputId = databaseIdManager.GetNextTransactionOutputId();

                    blockInfo.TransactionOutputs.Add(new DBData.TransactionOutput(
                        transactionOutputId,
                        bitcoinTransactionId,
                        outputIndex,
                        (decimal)parserTransactionOutput.OutputValueSatoshi / BtcToSatoshi,
                        parserTransactionOutput.OutputScript));
                }
            }

            return blockInfo;
        }

        private static List<long> GetOrphanBlockIds(BitcoinDataLayer bitcoinDataLayer)
        {
            DBData.SummaryBlockDataSet summaryBlockDataSet = bitcoinDataLayer.GetSummaryBlockDataSet();

            // KEY:     The 256-bit hash of the block
            // VALUE:   The summary block data as represented by an instance of DBData.SummaryBlockDataSet.BlockRow.
            Dictionary<ParserData.ByteArray, DBData.SummaryBlockDataSet.SummaryBlockRow> blockDictionary = summaryBlockDataSet.SummaryBlock.ToDictionary(
                b => new ParserData.ByteArray(b.BlockHash),
                b => b);

            DBData.SummaryBlockDataSet.SummaryBlockRow lastBlock = summaryBlockDataSet.SummaryBlock.OrderByDescending(b => b.BlockId).First();
            ParserData.ByteArray previousBlockHash = ParserData.ByteArray.Empty;
            ParserData.ByteArray currentBlockHash = new ParserData.ByteArray(lastBlock.BlockHash);

            // A hashset containing the IDs of all active blocks. Active as in non orphan.
            HashSet<long> activeBlockIds = new HashSet<long>();

            // Loop through blocks starting from the last one and going from one block to the next as indicated by PreviousBlockHash.
            // Collect all the block IDs for the blocks that we go through in activeBlockIds.
            // After this loop, blocks that we did not loop through are orphan blocks.
            while (currentBlockHash.IsZeroArray() == false)
            {
                DBData.SummaryBlockDataSet.SummaryBlockRow summaryBlockRow;
                if (blockDictionary.TryGetValue(currentBlockHash, out summaryBlockRow))
                {
                    // The current block was found in the list of blocks. 
                    activeBlockIds.Add(summaryBlockRow.BlockId);
                    previousBlockHash = currentBlockHash;
                    currentBlockHash = new ParserData.ByteArray(summaryBlockRow.PreviousBlockHash);
                }
                else
                {
                    // The current block was not found in the list of blocks. 
                    // This should never happen for a valid blockchain content.
                    throw new InvalidBlockchainContentException(string.Format(CultureInfo.InvariantCulture, "Block with hash [{0}] makes a reference to an unknown block with hash: [{1}]", previousBlockHash, currentBlockHash));
                }
            }

            // At this point activeBlockIds  contains the IDs of all active blocks.
            // Parse the list of all blocks and collect those whose IDs are not in activeBlockIds.
            return (from sumaryBlockRow in summaryBlockDataSet.SummaryBlock
                    where activeBlockIds.Contains(sumaryBlockRow.BlockId) == false
                    select sumaryBlockRow.BlockId).ToList();
        }

        private void DisplayDatabaseStatistics()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                int blockFileCount;
                int blockCount;
                int transactionCount;
                int transactionInputCount;
                int transactionOutputCount;

                bitcoinDataLayer.GetDatabaseEntitiesCount(out blockFileCount, out blockCount, out transactionCount, out transactionInputCount, out transactionOutputCount);

                Console.WriteLine();
                Console.WriteLine("Database information:");
                Console.WriteLine();
                Console.WriteLine("               Block Files: {0,14:n0}", blockFileCount);
                Console.WriteLine("                    Blocks: {0,14:n0}", blockCount);
                Console.WriteLine("              Transactions: {0,14:n0}", transactionCount);
                Console.WriteLine("        Transaction Inputs: {0,14:n0}", transactionInputCount);
                Console.WriteLine("       Transaction Outputs: {0,14:n0}", transactionOutputCount);
            }
        }

        /// <summary>
        /// Prepares the database ensuring it exists. 
        /// If the command line parameters so indicate, it will drop and recreate the database.
        /// </summary>
        /// <returns>
        /// True  - We start from a new database.
        /// False - We start from an existing database.
        /// </returns>
        private bool PrepareDatabase()
        {
            DatabaseManager databaseManager = new DatabaseManager(this.databaseConnection);

            if (this.parameters.DropDb)
            {
                if (databaseManager.DeleteDatabaseIfExists())
                {
                    Console.WriteLine("Database {0} was deleted.", this.databaseConnection.DatabaseName);
                }
            }

            if (databaseManager.EnsureDatabaseExists())
            {
                Console.WriteLine("Database {0} was created.", this.databaseConnection.DatabaseName);
                return true;
            }
            else
            {
                Console.WriteLine("Database {0} will be updated.", this.databaseConnection.DatabaseName);
                return false;
            }
        }

        private void CreateDatabaseIndexes()
        {
            Stopwatch createDatabaseIndexesWatch = new Stopwatch();
            createDatabaseIndexesWatch.Start();

            Console.Write("Create database indexes...");

            DatabaseManager databaseManager = new DatabaseManager(this.databaseConnection);
            databaseManager.CreateDatabaseIndexes();

            createDatabaseIndexesWatch.Stop();

            Console.WriteLine("\rDatabase indexes created successfully in {0:#.000} seconds", createDatabaseIndexesWatch.Elapsed.TotalSeconds);
        }

        private void UpdateTransactionSourceOutputId()
        {
            Stopwatch updateTransactionSourceOutputWatch = new Stopwatch();
            updateTransactionSourceOutputWatch.Start();

            Console.Write("Updating Transaction input source information...");

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                long rowsToUpdateCommand = bitcoinDataLayer.GetTransactionSourceOutputRowsToUpdate();

                long totalRowsUpdated = bitcoinDataLayer.UpdateNullTransactionSources();
                Console.Write("\rUpdating Transaction Input Source information... {0}%", 100 * totalRowsUpdated / rowsToUpdateCommand);

                int rowsUpdated;
                while ((rowsUpdated = bitcoinDataLayer.UpdateTransactionSourceBatch()) > 0)
                {
                    totalRowsUpdated += rowsUpdated;
                    Console.Write("\rUpdating Transaction Input Source information... {0}%", 100 * totalRowsUpdated / rowsToUpdateCommand);
                }
            }

            updateTransactionSourceOutputWatch.Stop();

            Console.WriteLine("\rUpdating Transaction Input Source information completed in {0:#.000} seconds", updateTransactionSourceOutputWatch.Elapsed.TotalSeconds);
        }

        private void DeleteOrphanBlocks()
        {
            Stopwatch deleteOrphanBlocksWatch = new Stopwatch();
            deleteOrphanBlocksWatch.Start();

            Console.Write("Searching for orphan blocks in the database...");

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                List<long> orphanBlocksIds = GetOrphanBlockIds(bitcoinDataLayer);

                if (orphanBlocksIds.Count > 0)
                {
                    // Now delete all orphan blocks
                    bitcoinDataLayer.DeleteBlocks(orphanBlocksIds);

                    // Update the block IDs after deleting the orphan blocks so that the block IDs are forming a consecutive sequence.
                    bitcoinDataLayer.CompactBlockIds(orphanBlocksIds);
                }

                deleteOrphanBlocksWatch.Stop();

                if (orphanBlocksIds.Count == 0)
                {
                    Console.WriteLine("\rNo orphan blocks were found. The search took {0:#.000} seconds.", deleteOrphanBlocksWatch.Elapsed.TotalSeconds);
                }
                else
                {
                    Console.WriteLine("\r{0} orphan blocks were found and deleted in {1:#.000} seconds.", orphanBlocksIds.Count, deleteOrphanBlocksWatch.Elapsed.TotalSeconds);
                }
            }
        }

        private string GetLastKnownBlockchainFileName()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                return bitcoinDataLayer.GetLastKnownBlockchainFileName();
            }
        }

        /// <summary>
        /// Deletes asynchronously from the database all the information associated with the last blockchain file.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        private async Task DeleteLastBlockFileAsync()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                await bitcoinDataLayer.DeleteLastBlockFileAsync();
            }
        }

        private async Task TransferBlockchainDataAsync(string lastKnownBlockchainFileName, bool newDatabase)
        {
            DatabaseIdManager databaseIdManager = this.GetDatabaseIdManager();
            TaskDispatcher taskDispatcher = new TaskDispatcher(this.parameters.Threads);
            IBlockchainParser blockchainParser = new BlockchainParser(this.parameters.BlockchainPath, lastKnownBlockchainFileName);

            this.processingStatistics.ProcessingBlockchainStarting();
            this.currentBlockchainFileStopwatch.Start();

            foreach (ParserData.Block block in blockchainParser.ParseBlockchain())
            {
                if (this.currentBlockchainFile != block.BlockchainFileName)
                {
                    if (this.currentBlockchainFile != null)
                    {
                        await this.FinalizeBlockchainFileProcessing(taskDispatcher);
                        this.currentBlockchainFileStopwatch.Restart();
                    }

                    this.lastReportedPercentage = -1;

                    this.ProcessBlockchainFile(block.BlockchainFileName, databaseIdManager);
                    this.currentBlockchainFile = block.BlockchainFileName;
                }

                this.ReportProgressReport(block.BlockchainFileName, block.PercentageOfCurrentBlockchainFile);

                // Note: We need to keep the call to ConvertParserBlockToBlockInfo outside of the
                //       parallel threads that are used to actually transfer the data to the DB.
                //       This is important if we want to ensure that the database primary keys are generated in a certain order.
                //       For example, with the current implementation, the block ID will be the block depth as reported
                //       by http://blockchain.info/. If we moved ConvertParserBlockToBlockInfo in the thread that 
                //       does the actually transfer to the DB, the IDs for the DB primary keys will be generated with a different pattern.
                BlockInfo blockInfo = ConvertParserBlockToBlockInfo(databaseIdManager, block);

                // At this point we have an instance: blockInfo that needs to be transferred into the DB.
                // We will dispatch the processing of that blockInfo on one of the threads managed by taskDispatcher.
                await taskDispatcher.DispatchWorkAsync(() => this.ProcessBlock(blockInfo));
            }

            await this.FinalizeBlockchainFileProcessing(taskDispatcher);
        }

        private void ReportProgressReport(string fileName, int percentage)
        {
            if (this.lastReportedPercentage != percentage)
            {
                Console.Write("\r    File: {0}. Processing: {1,3:n0}%", fileName, percentage);
                this.lastReportedPercentage = percentage;
            }
        }

        private async Task FinalizeBlockchainFileProcessing(TaskDispatcher taskDispatcher)
        {
            await taskDispatcher.WaitForAllWorkToComplete();

            this.currentBlockchainFileStopwatch.Stop();
            Console.Write(
                "\r    File: {0}. Processing: {1,3:n0}%. Completed in {2,7:0.000} seconds.",
                this.currentBlockchainFile,
                100,
                this.currentBlockchainFileStopwatch.Elapsed.TotalSeconds);

            Console.WriteLine();
        }

        private DatabaseIdManager GetDatabaseIdManager()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                int blockFileId;
                long blockId;
                long bitcoinTransactionId;
                long transactionInputId;
                long transactionOutputId;

                bitcoinDataLayer.GetMaximumIdValues(out blockFileId, out blockId, out bitcoinTransactionId, out transactionInputId, out transactionOutputId);

                return new DatabaseIdManager(blockFileId, blockId, bitcoinTransactionId, transactionInputId, transactionOutputId);
            }
        }

        private void ProcessBlockchainFile(string blockchainFileName, DatabaseIdManager databaseIdManager)
        {
            int blockFileId = databaseIdManager.GetNextBlockFileId();

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                bitcoinDataLayer.AddBlockFile(new DBData.BlockchainFile(blockFileId, blockchainFileName));
                this.processingStatistics.AddBlockFilesCount(1);
            }
        }

        private void ProcessBlock(BlockInfo blockInfo)
        {
            int transactionsCount = 0;
            int inputsCount = 0;
            int outputsCount = 0;

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                bitcoinDataLayer.AddBlock(blockInfo.Block);
                this.processingStatistics.AddBlocksCount(1);

                int transactionsInserted = bitcoinDataLayer.AddTransactions(blockInfo.BitcoinTransactions);
                this.processingStatistics.AddTransactionsCount(transactionsInserted);
                transactionsCount += transactionsInserted;

                //// Database optimization:
                //// In an initial version, we had code that looped over transactions. For each transaction we bulk inserted
                //// all inputs and then bulk inserted all outputs. However, on average, a transaction has a relatively low number
                //// of inputs and outputs. Now we bulk insert all inputs from all transactions of a block and then bulk insert
                //// all outputs from all transactions of a block. In this way we benefit a lot more from the bulk insert.

                int inputsInserted = bitcoinDataLayer.AddTransactionInputs(blockInfo.TransactionInputs);
                this.processingStatistics.AddTransactionInputsCount(inputsInserted);
                inputsCount += inputsInserted;

                int inputSourcesInserted = bitcoinDataLayer.AddTransactionInputSources(blockInfo.TransactionInputSources);

                if (inputsInserted != inputSourcesInserted)
                {
                    throw new InternalErrorException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Tables TransactionInput and TransactionInputSource should contain the same number of entries. A mismatch was detected when processing block: {0}. Rows inserted in TransactionInput: {1}. Rows inserted in TransactionInputSource: {2}.",
                        blockInfo.Block.BlockHash,
                        inputsInserted,
                        inputSourcesInserted));
                }

                int outputsInserted = bitcoinDataLayer.AddTransactionOutputs(blockInfo.TransactionOutputs);
                this.processingStatistics.AddTransactionOutputsCount(outputsInserted);
                outputsCount += outputsInserted;

                Debug.WriteLine(
                    "Block {0} was processed. Transactions: {1}. Inputs: {2}. Outputs: {3}",
                    blockInfo.Block.BlockId,
                    transactionsCount,
                    inputsCount,
                    outputsCount);
            }
        }
    }
}
