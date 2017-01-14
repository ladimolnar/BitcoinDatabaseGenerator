//-----------------------------------------------------------------------
// <copyright file="DatabaseGenerator.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BitcoinBlockchain.Parser;
    using BitcoinDataLayerAdoNet;
    using BitcoinDataLayerAdoNet.DataSets;
    using ZeroHelpers;
    using DBData = BitcoinDataLayerAdoNet.Data;
    using ParserData = BitcoinBlockchain.Data;

    public class DatabaseGenerator
    {
        public const long BtcToSatoshi = 100000000;

        private readonly IDatabaseGeneratorParameters parameters;
        private readonly DatabaseConnection databaseConnection;
        private readonly ProcessingStatistics processingStatistics;
        private readonly Func<IBlockchainParser> blockchainParserFactory;

        private int lastReportedPercentage;
        private string currentBlockchainFile;

        public DatabaseGenerator(IDatabaseGeneratorParameters parameters, DatabaseConnection databaseConnection, Func<IBlockchainParser> blockchainParserFactory = null)
        {
            this.parameters = parameters;
            this.blockchainParserFactory = blockchainParserFactory;

            this.databaseConnection = databaseConnection;
            this.processingStatistics = new ProcessingStatistics();
        }

        public async Task GenerateAndPopulateDatabase()
        {
            bool newDatabase = false;

            this.processingStatistics.PreprocessingStarting();

            this.PrepareDatabase();

            newDatabase = this.IsDatabaseEmpty();

            string lastKnownBlockchainFileName = null;
            lastKnownBlockchainFileName = this.GetLastKnownBlockchainFileName();

            if (lastKnownBlockchainFileName != null)
            {
                Console.WriteLine("Deleting from database the data about blockchain file: {0}", lastKnownBlockchainFileName);
                await this.DeleteLastBlockchainFileAsync();
            }

            if (newDatabase)
            {
                this.DisableAllHeavyIndexes();
            }

            Console.WriteLine();
            await this.TransferBlockchainDataAsync(lastKnownBlockchainFileName, newDatabase);

            this.processingStatistics.PostProcessingStarting();

            Console.WriteLine();

            if (newDatabase)
            {
                this.RebuildAllHeavyIndexes();
            }

            this.DeleteStaleBlocks();

            this.UpdateTransactionSourceOutputId();

            this.ShrinkDatabase();

            this.processingStatistics.ProcessingCompleted();

            this.processingStatistics.DisplayStatistics();
            this.DisplayDatabaseStatistics();
        }

        private static List<long> GetStaleBlockIds(BitcoinDataLayer bitcoinDataLayer)
        {
            SummaryBlockDataSet summaryBlockDataSet = bitcoinDataLayer.GetSummaryBlockDataSet();

            // KEY:     The 256-bit hash of the block
            // VALUE:   The summary block data as represented by an instance of DBData.SummaryBlockDataSet.BlockRow.
            Dictionary<ParserData.ByteArray, SummaryBlockDataSet.SummaryBlockRow> blockDictionary = summaryBlockDataSet.SummaryBlock.ToDictionary(
                b => new ParserData.ByteArray(b.BlockHash),
                b => b);

            SummaryBlockDataSet.SummaryBlockRow lastBlock = summaryBlockDataSet.SummaryBlock.OrderByDescending(b => b.BlockId).First();
            ParserData.ByteArray previousBlockHash = ParserData.ByteArray.Empty;
            ParserData.ByteArray currentBlockHash = new ParserData.ByteArray(lastBlock.BlockHash);

            // A hashset containing the IDs of all active blocks. Active as in not stale.
            HashSet<long> activeBlockIds = new HashSet<long>();

            // Loop through blocks starting from the last one and going from one block to the next as indicated by PreviousBlockHash.
            // Collect all the block IDs for the blocks that we go through in activeBlockIds.
            // After this loop, blocks that we did not loop through are stale blocks.
            while (currentBlockHash.IsZeroArray() == false)
            {
                SummaryBlockDataSet.SummaryBlockRow summaryBlockRow;
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

        private void DisableAllHeavyIndexes()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                bitcoinDataLayer.DisableAllHeavyIndexes();
            }

            Console.WriteLine("Database indexes were disabled.");
        }

        private void RebuildAllHeavyIndexes()
        {
            Stopwatch rebuildDatabaseIndexesWatch = new Stopwatch();
            rebuildDatabaseIndexesWatch.Start();

            Console.Write("Rebuilding database indexes ");

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString, BitcoinDataLayer.ExtendedDbCommandTimeout))
            {
                bitcoinDataLayer.RebuildAllHeavyIndexes(() => Console.Write("."));
            }

            rebuildDatabaseIndexesWatch.Stop();
            Console.WriteLine("\rDatabase indexes were rebuilt successfully in {0:0.000} seconds.", rebuildDatabaseIndexesWatch.Elapsed.TotalSeconds);
        }

        private void ShrinkDatabase()
        {
            Stopwatch shrinkDatabaseWatch = new Stopwatch();
            shrinkDatabaseWatch.Start();

            Console.Write("Shrinking database files...");

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                bitcoinDataLayer.ShrinkDatabase(this.parameters.SqlDbName);
            }

            shrinkDatabaseWatch.Stop();
            Console.WriteLine("\rShrinking database files completed in {0:0.000} seconds.", shrinkDatabaseWatch.Elapsed.TotalSeconds);
        }

        private void DisplayDatabaseStatistics()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                int blockchainFileCount;
                int blockCount;
                int transactionCount;
                int transactionInputCount;
                int transactionOutputCount;

                bitcoinDataLayer.GetDatabaseEntitiesCount(out blockchainFileCount, out blockCount, out transactionCount, out transactionInputCount, out transactionOutputCount);

                Console.WriteLine();
                Console.WriteLine("Database information:");
                Console.WriteLine();
                Console.WriteLine("                 Block Files: {0,14:n0}", blockchainFileCount);
                Console.WriteLine("                      Blocks: {0,14:n0}", blockCount);
                Console.WriteLine("                Transactions: {0,14:n0}", transactionCount);
                Console.WriteLine("          Transaction Inputs: {0,14:n0}", transactionInputCount);
                Console.WriteLine("         Transaction Outputs: {0,14:n0}", transactionOutputCount);
            }
        }

        /// <summary>
        /// Prepares the database ensuring it exists. 
        /// If the command line parameters so indicate, it will drop and recreate the database.
        /// </summary>
        private void PrepareDatabase()
        {
            DatabaseManager databaseManager = new DatabaseManager(this.databaseConnection);

            if (this.parameters.IsDropDbSpecified)
            {
                if (databaseManager.DatabaseExists())
                {
                    Console.Write("Deleting database \"{0}\"...", this.databaseConnection.DatabaseName);
                    databaseManager.DeleteDatabase();
                    Console.WriteLine("\rDatabase \"{0}\" was deleted.", this.databaseConnection.DatabaseName);
                }
            }

            if (databaseManager.DatabaseExists())
            {
                Console.WriteLine("Database \"{0}\" found.", this.databaseConnection.DatabaseName);
            }
            else
            {
                databaseManager.CreateNewDatabase();
                Console.WriteLine("Database \"{0}\" was created.", this.databaseConnection.DatabaseName);
            }

            if (this.IsSchemaSetup() == false)
            {
                databaseManager.ExecuteDatabaseSetupStatements();
                Console.WriteLine("Database schema was setup.");
            }
        }

        private bool IsSchemaSetup()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                return bitcoinDataLayer.IsSchemaSetup();
            }
        }

        private bool IsDatabaseEmpty()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                return bitcoinDataLayer.IsDatabaseEmpty();
            }
        }

        private void UpdateTransactionSourceOutputId()
        {
            const long maxBatchSize = 10000000;

            Stopwatch updateTransactionSourceOutputWatch = new Stopwatch();
            updateTransactionSourceOutputWatch.Start();

            Console.Write("Setting direct links: inputs to source outputs (this may take a long time)...");

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString, BitcoinDataLayer.ExtendedDbCommandTimeout))
            {
                long rowsToUpdateCommand = bitcoinDataLayer.GetTransactionSourceOutputRowsToUpdate();

                long batchSize = rowsToUpdateCommand / 10;
                batchSize = batchSize >= 1 ? batchSize : 1;
                batchSize = batchSize <= maxBatchSize ? batchSize : maxBatchSize;

                long totalRowsUpdated = bitcoinDataLayer.UpdateNullTransactionSources();
                Console.Write("\rSetting direct links: inputs to source outputs (this may take a long time)... {0}%", 95 * totalRowsUpdated / rowsToUpdateCommand);

                int rowsUpdated;
                while ((rowsUpdated = bitcoinDataLayer.UpdateTransactionSourceBatch(batchSize)) > 0)
                {
                    totalRowsUpdated += rowsUpdated;
                    Console.Write("\rSetting direct links: inputs to source outputs (this may take a long time)... {0}%", 95 * totalRowsUpdated / rowsToUpdateCommand);
                }

                bitcoinDataLayer.FixupTransactionSourceOutputIdForDuplicateTransactionHash();
            }

            updateTransactionSourceOutputWatch.Stop();

            Console.WriteLine("\rSetting direct links: inputs to source outputs completed in {0:0.000} seconds.          ", updateTransactionSourceOutputWatch.Elapsed.TotalSeconds);
        }

        private void DeleteStaleBlocks()
        {
            Stopwatch deleteStaleBlocksWatch = new Stopwatch();
            deleteStaleBlocksWatch.Start();

            Console.Write("Searching for stale blocks in the database...");

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                List<long> staleBlocksIds = GetStaleBlockIds(bitcoinDataLayer);

                if (staleBlocksIds.Count > 0)
                {
                    // Now delete all stale blocks
                    bitcoinDataLayer.DeleteBlocks(staleBlocksIds);

                    // Update the block IDs after deleting the stale blocks so that the block IDs are forming a consecutive sequence.
                    bitcoinDataLayer.CompactBlockIds(staleBlocksIds);
                }

                deleteStaleBlocksWatch.Stop();

                if (staleBlocksIds.Count == 0)
                {
                    Console.WriteLine("\rNo stale blocks were found. The search took {0:0.000} seconds.", deleteStaleBlocksWatch.Elapsed.TotalSeconds);
                }
                else
                {
                    string format = staleBlocksIds.Count == 1 ?
                        "\rOne stale block was found and deleted in {1:0.000} seconds" :
                        "\r{0} stale blocks were found and deleted in {1:0.000} seconds.";

                    Console.WriteLine(format, staleBlocksIds.Count, deleteStaleBlocksWatch.Elapsed.TotalSeconds);
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
        private async Task DeleteLastBlockchainFileAsync()
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                await bitcoinDataLayer.DeleteLastBlockchainFileAsync();
            }
        }

        private async Task TransferBlockchainDataAsync(string lastKnownBlockchainFileName, bool newDatabase)
        {
            DatabaseIdManager databaseIdManager = this.GetDatabaseIdManager();
            TaskDispatcher taskDispatcher = new TaskDispatcher(this.parameters.Threads); // What if we use 1 thread now that we use bulk copy?

            IBlockchainParser blockchainParser;
            if (this.blockchainParserFactory == null)
            {
                blockchainParser = new BlockchainParser(this.parameters.BlockchainPath, lastKnownBlockchainFileName);
            }
            else
            {
                blockchainParser = this.blockchainParserFactory();
            }

            if (this.parameters.BlockId != null)
            {
                blockchainParser.SetBlockId(this.parameters.BlockId.Value);
            }

            this.processingStatistics.ProcessingBlockchainStarting();

            Stopwatch currentBlockchainFileStopwatch = new Stopwatch();
            currentBlockchainFileStopwatch.Start();

            SourceDataPipeline sourceDataPipeline = new SourceDataPipeline();

            int blockFileId = -1;

            try
            {
                foreach (ParserData.Block block in blockchainParser.ParseBlockchain())
                {
                    if (this.currentBlockchainFile != block.BlockchainFileName)
                    {
                        if (this.currentBlockchainFile != null)
                        {
                            this.FinalizeBlockchainFileProcessing(currentBlockchainFileStopwatch);
                            currentBlockchainFileStopwatch.Restart();
                        }

                        this.lastReportedPercentage = -1;

                        blockFileId = databaseIdManager.GetNextBlockchainFileId(1);
                        this.ProcessBlockchainFile(blockFileId, block.BlockchainFileName);
                        this.currentBlockchainFile = block.BlockchainFileName;
                    }

                    this.ReportProgressReport(block.BlockchainFileName, block.PercentageOfCurrentBlockchainFile);

                    // We instantiate databaseIdSegmentManager on the main thread and by doing this we'll guarantee that 
                    // the database primary keys are generated in a certain order. The primary keys in our tables will be 
                    // in the same order as the corresponding entities appear in the blockchain. For example, with the 
                    // current implementation, the block ID will be the block depth as reported by http://blockchain.info/. 
                    DatabaseIdSegmentManager databaseIdSegmentManager = new DatabaseIdSegmentManager(databaseIdManager, 1, block.Transactions.Count, block.TransactionInputsCount, block.TransactionOutputsCount);

                    this.processingStatistics.AddBlocksCount(1);
                    this.processingStatistics.AddTransactionsCount(block.Transactions.Count);
                    this.processingStatistics.AddTransactionInputsCount(block.TransactionInputsCount);
                    this.processingStatistics.AddTransactionOutputsCount(block.TransactionOutputsCount);

                    int blockFileId2 = blockFileId;
                    ParserData.Block block2 = block;

                    // Dispatch the work of "filling the source pipeline" to an available background thread.
                    // Note: The await awaits only until the work is dispatched and not until the work is completed. 
                    //       Dispatching the work itself may take a while if all available background threads are busy. 
                    await taskDispatcher.DispatchWorkAsync(() => sourceDataPipeline.FillBlockchainPipeline(blockFileId2, block2, databaseIdSegmentManager));

                    await this.TransferAvailableData(taskDispatcher, sourceDataPipeline);
                }
            }
            finally
            {
                // Whatever we have in the pipeline we'll push to the DB. We do this in a finally block.
                // Otherwise an exception that occurs in blockchain file 100 may prevent data that was 
                // collected in blockchain file 99 to be saved to DB.

                // Wait for the last remaining background tasks if any that are still executing 
                // sourceDataPipeline.FillBlockchainPipeline or the SQL bulk copy to finish.
                await taskDispatcher.WaitForAllWorkToComplete();

                // Instruct sourceDataPipeline to transfer all remaining data to the available data queue.
                // IMPORTANT: do not call this while there could still be threads executing sourceDataPipeline.FillBlockchainPipeline.
                sourceDataPipeline.Flush();

                // Now trigger the SQL bulk copy for the data that remains.
                await this.TransferAvailableData(taskDispatcher, sourceDataPipeline);

                // Wait for the last remaining background tasks if any that are still executing 
                // the SQL bulk copy to finish.
                await taskDispatcher.WaitForAllWorkToComplete();
            }

            this.FinalizeBlockchainFileProcessing(currentBlockchainFileStopwatch);
        }

        private async Task TransferAvailableData(TaskDispatcher taskDispatcher, SourceDataPipeline sourceDataPipeline)
        {
            DataTable availableDataTable;
            while (sourceDataPipeline.TryGetNextAvailableDataSource(out availableDataTable))
            {
                DataTable availableDataTable2 = availableDataTable;

                // Dispatch the work of transferring data from the "source pipeline" int the database to an available background thread.
                // Note: The await awaits only until the work is dispatched and not until the work is completed. 
                //       Dispatching the work itself may take a while if all available background threads are busy. 
                await taskDispatcher.DispatchWorkAsync(() => this.TransferTable(availableDataTable2));
            }
        }

        private void TransferTable(DataTable dataTable)
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                bitcoinDataLayer.BulkCopyTable(dataTable);
            }
        }

        private void ReportProgressReport(string blockchainFileName, int percentage)
        {
            if (this.lastReportedPercentage != percentage)
            {
                Console.Write("\r    File: {0}. Transferring data: {1,3:n0}%", blockchainFileName, percentage);
                this.lastReportedPercentage = percentage;
            }
        }

        private void FinalizeBlockchainFileProcessing(Stopwatch currentBlockchainFileStopwatch)
        {
            currentBlockchainFileStopwatch.Stop();
            Console.WriteLine(
                "\r    File: {0}. Transferring data completed in {1,7:0.000} seconds.",
                this.currentBlockchainFile,
                currentBlockchainFileStopwatch.Elapsed.TotalSeconds);
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

                return new DatabaseIdManager(blockFileId + 1, blockId + 1, bitcoinTransactionId + 1, transactionInputId + 1, transactionOutputId + 1);
            }
        }

        private void ProcessBlockchainFile(int blockFileId, string blockchainFileName)
        {
            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                bitcoinDataLayer.AddBlockchainFile(new DBData.BlockchainFile(blockFileId, blockchainFileName));
                this.processingStatistics.AddBlockchainFilesCount(1);
            }
        }
    }
}
