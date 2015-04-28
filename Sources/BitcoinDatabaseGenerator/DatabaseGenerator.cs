//-----------------------------------------------------------------------
// <copyright file="DatabaseGenerator.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
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
        private const string FirstBlockChainFileName = "blk00000.dat";

        private const decimal BtcToSatoshi = 100000000;

        private readonly DatabaseGeneratorParameters parameters;
        private readonly DatabaseConnection databaseConnection;
        private readonly ProcessingStatistics processingStatistics;
        private readonly ProcessingWarnings processingWarnings;
        private readonly Stopwatch currentBlockchainFileStopwatch;

        private int lastReportedPercentage;
        private string currentBlockchainFile;

        public DatabaseGenerator(DatabaseGeneratorParameters parameters)
        {
            this.parameters = parameters;
            this.databaseConnection = new DatabaseConnection(this.parameters.SqlServerName, this.parameters.DatabaseName, this.parameters.SqlUserName, this.parameters.SqlPassword);
            this.processingStatistics = new ProcessingStatistics();
            this.processingWarnings = new ProcessingWarnings();
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

            // Collect the hashes of all orphan blocks starting from lastKnownBlockchainFileName.
            List<ParserData.ByteArray> orphanBlockHashes = this.CollectOrphanBlockHashes(lastKnownBlockchainFileName);

            if (lastKnownBlockchainFileName != null)
            {
                Console.WriteLine();
                Console.WriteLine("Deleting from database information about blockchain file: {0}", lastKnownBlockchainFileName);
                await this.DeleteLastBlockFileAsync();
            }

            Console.WriteLine();
            UnspentTransactionLookup unspentTransactionLookup = await this.TransferBlockchainDataAsync(lastKnownBlockchainFileName, newDatabase, orphanBlockHashes);

            this.processingStatistics.PostProcessingStarting();

            Console.WriteLine();

            this.ValidateUnspentTransactionLookup(unspentTransactionLookup);

            if (newDatabase)
            {
                this.CreateDatabaseIndexes();
            }

            this.processingStatistics.ProcessingCompleted();

            this.processingStatistics.DisplayStatistics();
            this.DisplayDatabaseStatistics();
            this.processingWarnings.DisplayWarnings();

            DisplayFinalDebugInformation();
        }

        private static BlockInfo ConvertParserBlockToBlockInfo(
            DatabaseIdManager databaseIdManager,
            UnspentTransactionLookup unspentTransactionLookup,
            ParserData.Block parserBlock)
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

                List<UnspentOutputInfo> unspentOutputInfoList = new List<UnspentOutputInfo>();

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

                    unspentOutputInfoList.Add(new UnspentOutputInfo(transactionOutputId, outputIndex));
                }

                unspentTransactionLookup.AddUnspentTransactionInfo(
                    new UnspentTransactionInfo(
                        bitcoinTransactionId,
                        parserTransaction.TransactionHash,
                        unspentOutputInfoList),
                    DataOrigin.Blockchain);

                foreach (ParserData.TransactionInput parserTransactionInput in parserTransaction.Inputs)
                {
                    long? sourceTransactionOutputId = null;
                    if (parserTransactionInput.SourceTransactionOutputIndex != ParserData.TransactionInput.OutputIndexNotUsed)
                    {
                        sourceTransactionOutputId = unspentTransactionLookup.SpendTransactionOutput(
                            parserTransaction.TransactionHash,
                            parserTransactionInput.SourceTransactionHash,
                            (int)parserTransactionInput.SourceTransactionOutputIndex);
                    }

                    blockInfo.TransactionInputs.Add(
                        new DBData.TransactionInput(
                            databaseIdManager.GetNextTransactionInputId(),
                            bitcoinTransactionId,
                            sourceTransactionOutputId));
                }
            }

            return blockInfo;
        }

        private static List<BlockSummaryInfo> ExtractOrphanBlocks(
            Dictionary<ParserData.ByteArray, BlockSummaryInfo> blockSummaryInfoDictionary,
            bool parseEntireBlockchain,
            ParserData.ByteArray lastBlockHash)
        {
            ParserData.ByteArray previousBlockHash = ParserData.ByteArray.Empty;
            ParserData.ByteArray currentBlockHash = lastBlockHash;

            while (currentBlockHash.IsZeroArray() == false)
            {
                BlockSummaryInfo blockSummaryInfo;
                if (blockSummaryInfoDictionary.TryGetValue(currentBlockHash, out blockSummaryInfo))
                {
                    // The current block was found in the list of blocks. 
                    blockSummaryInfo.IsActive = true;
                    previousBlockHash = currentBlockHash;
                    currentBlockHash = blockSummaryInfo.PreviousBlockHash;
                }
                else
                {
                    if (parseEntireBlockchain)
                    {
                        // The current block was not found in the list of blocks. 
                        // This should never happen for a valid blockchain content.
                        throw new InvalidBlockchainContentException(string.Format(
                            CultureInfo.InvariantCulture,
                            "Block with hash [{0}] makes a reference to an unknown block with hash: [{1}]",
                            previousBlockHash,
                            currentBlockHash));
                    }
                    else
                    {
                        // We did not parse the entire blockchain. We must have reached to a block pointing 
                        // to a block that is located in a file that we did not parsed.
                        // Note: As a criteria to stop the loop we cannot just stop once we hit the first 
                        //       block in the first file we parsed. That block may be orphan case in which 
                        //       we'd never hit it in this loop.
                        break;
                    }
                }
            }

            return blockSummaryInfoDictionary.Values.Where(b => b.IsActive == false).ToList();
        }

        [Conditional("DEBUG")]
        private static void DisplayFinalDebugInformation()
        {
            using (Process proc = Process.GetCurrentProcess())
            {
                Console.WriteLine();
                Console.WriteLine("DEBUG: Peak memory: {0:n0} bytes.", proc.PeakWorkingSet64);
            }
        }

        /// <summary>
        /// Validates the unspent transaction information that was accumulated during the processing of the blockchain
        /// against the same information retrieved from the database.
        /// </summary>
        /// <param name="unspentTransactionLookupAfterProcessing">
        /// The unspent transaction information that was accumulated during the processing of the blockchain.
        /// </param>
        [Conditional("DEBUG")]
        private void ValidateUnspentTransactionLookup(UnspentTransactionLookup unspentTransactionLookupAfterProcessing)
        {
            Console.WriteLine();
            Console.Write("DEBUG VALIDATION: Validating unspent transactions information...");

            try
            {
                UnspentTransactionLookup databaseUnspentTransactionLookup = this.GetUnspentTransactionsFromDatabase();

                if (databaseUnspentTransactionLookup.Equals(unspentTransactionLookupAfterProcessing))
                {
                    Console.WriteLine("\rDEBUG VALIDATION: Unspent transactions information was validated successfully.");
                }
                else
                {
                    throw new InternalErrorException("Unspent transactions information failed an internal validation.");
                }
            }
            catch (OutOfMemoryException)
            {
                Console.Write("\rDEBUG VALIDATION: Unable to validate unspent transactions information. Not enough memory.");
            }
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

            Console.WriteLine();
            Console.Write("Create database indexes...");

            DatabaseManager databaseManager = new DatabaseManager(this.databaseConnection);
            databaseManager.CreateDatabaseIndexes();

            createDatabaseIndexesWatch.Stop();

            Console.WriteLine("\rDatabase indexes created successfully in {0:#.000} seconds", createDatabaseIndexesWatch.Elapsed.TotalSeconds);
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

        private List<ParserData.ByteArray> CollectOrphanBlockHashes(string lastKnownBlockchainFileName)
        {
            Console.WriteLine();

            bool parseEntireBlockchain = lastKnownBlockchainFileName == null || lastKnownBlockchainFileName == FirstBlockChainFileName;

            Stopwatch orphanBlocksSearchWatch = new Stopwatch();
            orphanBlocksSearchWatch.Start();

            ParserData.ByteArray lastBlockHash;
            Dictionary<ParserData.ByteArray, BlockSummaryInfo> blockSummaryInfoDictionary = this.CollectAllBlockSummaryInfo(lastKnownBlockchainFileName, out lastBlockHash);

            if (lastBlockHash == null)
            {
                return new List<ParserData.ByteArray>();
            }

            List<BlockSummaryInfo> orphanBlocsInfo = ExtractOrphanBlocks(blockSummaryInfoDictionary, parseEntireBlockchain, lastBlockHash);

            orphanBlocksSearchWatch.Stop();

            Console.WriteLine("\rSearching the new blockchain files for orphan blocks completed in {0:#.000} seconds.", orphanBlocksSearchWatch.Elapsed.TotalSeconds);

            if (orphanBlocsInfo.Count > 0)
            {
                Console.WriteLine("{0} orphan blocks found.", orphanBlocsInfo.Count);
                foreach (BlockSummaryInfo orphanBlock in orphanBlocsInfo)
                {
                    Console.WriteLine("File: {0}. Block hash: {1}.", orphanBlock.BlockchainFileName, orphanBlock.BlockHash);
                }
            }
            else
            {
                Console.WriteLine("No orphan blocks were found.");
            }

            return orphanBlocsInfo.Select(b => b.BlockHash).ToList();
        }

        private Dictionary<ParserData.ByteArray, BlockSummaryInfo> CollectAllBlockSummaryInfo(string lastKnownBlockchainFileName, out ParserData.ByteArray lastBlockHash)
        {
            Dictionary<ParserData.ByteArray, BlockSummaryInfo> blockSummaryInfoDictionary = new Dictionary<ParserData.ByteArray, BlockSummaryInfo>();
            IBlockchainParser blockchainParser = new BlockchainParser(this.parameters.BlockchainPath, lastKnownBlockchainFileName);

            string currentBlockchainFileName = null;

            lastBlockHash = null;

            foreach (ParserData.Block block in blockchainParser.ParseBlockchain())
            {
                if (currentBlockchainFileName != block.BlockchainFileName)
                {
                    currentBlockchainFileName = block.BlockchainFileName;
                    Console.Write("\rSearching the new blockchain files for orphan blocks. Searching in {0}", currentBlockchainFileName);
                }

                BlockSummaryInfo blockSummaryInfo = new BlockSummaryInfo(block.BlockchainFileName, block.BlockHeader.BlockHash, block.BlockHeader.PreviousBlockHash);
                lastBlockHash = blockSummaryInfo.BlockHash;
                blockSummaryInfoDictionary.Add(blockSummaryInfo.BlockHash, blockSummaryInfo);
            }

            return blockSummaryInfoDictionary;
        }

        private async Task<UnspentTransactionLookup> TransferBlockchainDataAsync(string lastKnownBlockchainFileName, bool newDatabase, List<ParserData.ByteArray> orphanBlockHashes)
        {
            DatabaseIdManager databaseIdManager = this.GetDatabaseIdManager();
            TaskDispatcher taskDispatcher = new TaskDispatcher(this.parameters.Threads);
            IBlockchainParser blockchainParser = new BlockchainParser(this.parameters.BlockchainPath, lastKnownBlockchainFileName);

            UnspentTransactionLookup unspentTransactionLookup;
            if (newDatabase == false)
            {
                Console.Write("Retrieve information about unspent transactions stored in the database...");
                unspentTransactionLookup = this.GetUnspentTransactionsFromDatabase();
                Console.WriteLine("\rThe database contains {0:n0} unspent transactions with {1:n0} unspent outputs.", unspentTransactionLookup.UnspentTransactionsCount, unspentTransactionLookup.UnspentTransactionOutputsCount);
                Console.WriteLine();
            }
            else
            {
                unspentTransactionLookup = new UnspentTransactionLookup(this.processingWarnings);
            }

            this.processingStatistics.ProcessingBlockchainStarting();
            this.currentBlockchainFileStopwatch.Start();

            foreach (ParserData.Block block in blockchainParser.ParseBlockchain())
            {
                // We will ignore any orphan blocks.
                if (orphanBlockHashes.Contains(block.BlockHeader.BlockHash) == false)
                {
                    if (this.currentBlockchainFile != block.BlockchainFileName)
                    {
                        if (this.currentBlockchainFile != null)
                        {
                            await this.FinalizeBlockchainFileProcessing(taskDispatcher, unspentTransactionLookup);
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
                    BlockInfo blockInfo = ConvertParserBlockToBlockInfo(databaseIdManager, unspentTransactionLookup, block);

                    // At this point we have an instance: blockInfo that needs to be transferred into the DB.
                    // We will dispatch the processing of that blockInfo on one of the threads managed by taskDispatcher.
                    await taskDispatcher.DispatchWorkAsync(() => this.ProcessBlock(blockInfo));
                }
            }

            await this.FinalizeBlockchainFileProcessing(taskDispatcher, unspentTransactionLookup);

            return unspentTransactionLookup;
        }

        private UnspentTransactionLookup GetUnspentTransactionsFromDatabase()
        {
            UnspentTransactionLookup unspentTransactionLookup = new UnspentTransactionLookup(this.processingWarnings);

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString))
            {
                long bitcoinUnspentTransactionId = -1;
                ParserData.ByteArray unspendTransactionHash = null;
                List<UnspentOutputInfo> unspentOutputInfoList = new List<UnspentOutputInfo>();

                using (SqlDataReader unspentOutputsReader = bitcoinDataLayer.GetUnspentOutputsReader())
                {
                    while (unspentOutputsReader.Read())
                    {
                        long bitcoinTransactionId = unspentOutputsReader.GetInt64(0);
                        byte[] transactionHash = unspentOutputsReader.GetSqlBinary(1).Value;
                        long transactionOutputId = unspentOutputsReader.GetInt64(2);
                        int outputIndex = unspentOutputsReader.GetInt32(3);

                        if (bitcoinUnspentTransactionId != bitcoinTransactionId)
                        {
                            if (unspentOutputInfoList.Count > 0)
                            {
                                unspentTransactionLookup.AddUnspentTransactionInfo(
                                    new UnspentTransactionInfo(
                                        bitcoinUnspentTransactionId,
                                        unspendTransactionHash,
                                        unspentOutputInfoList),
                                    DataOrigin.Database);
                            }

                            bitcoinUnspentTransactionId = bitcoinTransactionId;

                            unspendTransactionHash = new ParserData.ByteArray(transactionHash);
                            unspentOutputInfoList = new List<UnspentOutputInfo>();
                        }

                        unspentOutputInfoList.Add(new UnspentOutputInfo(transactionOutputId, outputIndex));
                    }

                    if (unspentOutputInfoList.Count > 0)
                    {
                        unspentTransactionLookup.AddUnspentTransactionInfo(new UnspentTransactionInfo(bitcoinUnspentTransactionId, unspendTransactionHash, unspentOutputInfoList), DataOrigin.Database);
                    }
                }
            }

            return unspentTransactionLookup;
        }

        private void ReportProgressReport(string fileName, int percentage)
        {
            if (this.lastReportedPercentage != percentage)
            {
                Console.Write("\r    File: {0}. Processing: {1,3:n0}%", fileName, percentage);
                this.lastReportedPercentage = percentage;
            }
        }

        private async Task FinalizeBlockchainFileProcessing(TaskDispatcher taskDispatcher, UnspentTransactionLookup unspentTransactionLookup)
        {
            await taskDispatcher.WaitForAllWorkToComplete();

            this.currentBlockchainFileStopwatch.Stop();
            Console.Write(
                "\r    File: {0}. Processing: {1,3:n0}%. Completed in {2,7:0.000} seconds.",
                this.currentBlockchainFile,
                100,
                this.currentBlockchainFileStopwatch.Elapsed.TotalSeconds);

            this.ReportDebugProcessingInfo(unspentTransactionLookup);

            Console.WriteLine();
        }

        [Conditional("DEBUG")]
        private void ReportDebugProcessingInfo(UnspentTransactionLookup unspentTransactionLookup)
        {
            using (Process proc = Process.GetCurrentProcess())
            {
                Console.Write(
                    " DEBUG: W: {0:n0}  UT: {1:n0}  UO: {2:n0} MEM: {3,14:n0} bytes.",
                    this.processingWarnings.Count,
                    unspentTransactionLookup.UnspentTransactionsCount,
                    unspentTransactionLookup.UnspentTransactionOutputsCount,
                    proc.PrivateMemorySize64);
            }
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
