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
    using System.Threading.Tasks;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.Data;
    using BitcoinDataLayerAdoNet.DataSets;
    using ZeroHelpers;

    public partial class BitcoinDataLayer : IDisposable
    {
        /// <summary>
        /// The timeout in seconds that is used for SQL commands that 
        /// are expected to take a very long time.
        /// </summary>
        public const int ExtendedDbCommandTimeout = 3600;

        /// <summary>
        /// The default timeout in seconds that is used for each SQL command.
        /// </summary>
        public const int DefaultDbCommandTimeout = 1800;

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
            return AdoNetLayer.ConvertDbValue<string>(this.adoNetLayer.ExecuteScalar("SELECT TOP 1 BlockchainFileName FROM BlockchainFile ORDER BY BlockchainFileId DESC"));
        }

        public async Task DeleteLastBlockchainFileAsync()
        {
            const string deleteFromTransactionOutput = @"
                DELETE TransactionOutput FROM TransactionOutput
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockchainFileId >= @MaxBlockchainFileId";

            const string deleteFromTransactionInputSource = @"
                DELETE TransactionInputSource FROM TransactionInputSource
                INNER JOIN TransactionInput ON TransactionInput.TransactionInputId = TransactionInputSource.TransactionInputId
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockchainFileId >= @MaxBlockchainFileId";

            const string deleteFromTransactionInput = @"
                DELETE TransactionInput FROM TransactionInput
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockchainFileId >= @MaxBlockchainFileId";

            const string deleteFromBitcoinTransaction = @"
                DELETE BitcoinTransaction FROM BitcoinTransaction
                INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                WHERE Block.BlockchainFileId >= @MaxBlockchainFileId";

            const string deleteFromBlock = @" DELETE Block FROM Block WHERE Block.BlockchainFileId >= @MaxBlockchainFileId";

            const string deleteFromBlockchainFile = "DELETE FROM BlockchainFile WHERE BlockchainFile.BlockchainFileId >= @MaxBlockchainFileId";

            int lastBlockchainFileId = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BlockchainFileId) from BlockchainFile"));

            await this.adoNetLayer.ExecuteStatementNoResultAsync(deleteFromTransactionOutput, AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, lastBlockchainFileId));
            await this.adoNetLayer.ExecuteStatementNoResultAsync(deleteFromTransactionInputSource, AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, lastBlockchainFileId));
            await this.adoNetLayer.ExecuteStatementNoResultAsync(deleteFromTransactionInput, AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, lastBlockchainFileId));
            await this.adoNetLayer.ExecuteStatementNoResultAsync(deleteFromBitcoinTransaction, AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, lastBlockchainFileId));
            await this.adoNetLayer.ExecuteStatementNoResultAsync(deleteFromBlock, AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, lastBlockchainFileId));
            await this.adoNetLayer.ExecuteStatementNoResultAsync(deleteFromBlockchainFile, AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, lastBlockchainFileId));
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
                WHERE TransactionInputSource.SourceTransactionOutputIndex = -1 AND TransactionInput.SourceTransactionOutputId = -1";

            return this.adoNetLayer.ExecuteStatementNoResult(sqlUpdateNullSourceCommand);
        }

        /// <summary>
        /// Sets the values in column TransactionInput.SourceTransactionOutputId 
        /// for a batch of rows where that column was not yet set.
        /// Does not account for the case where two transactions have the same transaction hash. 
        /// FixUpTransactionSourceOutputIdForDuplicateTransactionHash will handle that case.
        /// </summary>
        /// <param name="batchSize">
        /// The batch Size.
        /// </param>
        /// <returns>
        /// The number of rows affected in table TransactionInput.
        /// </returns>
        public int UpdateTransactionSourceBatch(long batchSize)
        {
            //// Here is where we set the column TransactionInput.SourceTransactionOutputId
            //// For each transaction input we know what transaction output constitutes its source:
            ////      1. We know the transaction hash of the source transaction. 
            ////      2. We know the output index of the corresponding output in the source transaction. 
            //// Knowing this we need to calculate the Id of that output and assign it to TransactionInput.SourceTransactionOutputId
            ////
            //// Note: this select does not account for the case where two transactions have the same transaction hash. 
            ////       A select that would account for that would be quite more expensive than what we have here.
            ////       The duplicate transaction hash is covered by FixUpTransactionSourceOutputIdForDuplicateTransactionHash

            const string formattedStatement = @"
                UPDATE TransactionInput 
                SET SourceTransactionOutputId = TransactionOutput.TransactionOutputId
                FROM TransactionInput 
                INNER JOIN TransactionInputSource ON TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                INNER JOIN BitcoinTransaction ON BitcoinTransaction.TransactionHash = TransactionInputSource.SourceTransactionHash
                INNER JOIN TransactionOutput ON 
                    TransactionOutput.BitcoinTransactionId = BitcoinTransaction.BitcoinTransactionId 
                    AND TransactionOutput.OutputIndex = TransactionInputSource.SourceTransactionOutputIndex 
                INNER JOIN (
                    SELECT TOP {0}
                        TransactionInput.TransactionInputId
                    FROM TransactionInput
                    WHERE TransactionInput.SourceTransactionOutputId = -1
                ) AS T1 ON T1.TransactionInputId = TransactionInput.TransactionInputId
                WHERE TransactionInputSource.SourceTransactionOutputIndex != -1";

            string sqlUpdateSourceTransactionOutputIdCommand = string.Format(CultureInfo.InvariantCulture, formattedStatement, batchSize);

            return this.adoNetLayer.ExecuteStatementNoResult(sqlUpdateSourceTransactionOutputIdCommand);
        }

        /// <summary>
        /// Sets the values in column TransactionInput.SourceTransactionOutputId for rows that are
        /// referencing an output that belongs to a transaction that has a duplicate transaction hash.
        /// </summary>
        /// <remarks>
        /// Note about the duplicate transaction hash scenario:
        ///     As far as I know, having two or more transactions that have the same transaction hash 
        ///     is not illegal as long as the first transaction is fully spent.
        ///     See: <see href="https://bitcointalk.org/index.php?topic=67738.0" />
        ///     and <see href="http://sourceforge.net/p/bitcoin/mailman/bitcoin-development/thread/CAPg+sBhmGHnMResVxPDZdfpmWTb9uqD0RrQD7oSXBQq7oHpm8g@mail.gmail.com/" />
        ///     UpdateTransactionSourceBatch does not account for this case. We could write a version of it 
        ///     that does but that would have a significantly lower performance compared 
        ///     with the current implementation of UpdateTransactionSourceBatch.
        ///     For the record, that solution is in <c>Git commit 3cf54fc5901054f03b5a55410665596cea4c96db</c>
        ///     in repository: <see href="https://github.com/ladimolnar/BitcoinDatabaseGenerator.git" />.
        ///     To fix the case where we have duplicate transaction hashes we call this method: 
        ///     FixUpTransactionSourceOutputIdForDuplicateTransactionHash.
        ///     At the time of this writing there are four transactions that use a duplicate value for the transaction hash. 
        ///     There are two transactions with hash 0xD5D27987D2A3DFC724E359870C6644B40E497BDC0589A033220FE15429D88599 and
        ///     two transactions with hash 0xE3BF3D07D4B0375638D5F1DB5255FE07BA2C4CB067CD81B84EE974B6585FB468. 
        ///     There are no transactions that are spending outputs in those four transactions but when they appear 
        ///     (if they appear) FixUpTransactionSourceOutputIdForDuplicateTransactionHash will make sure that their 
        ///     inputs refer to the correct outputs.
        /// Test note: 
        ///     We have a test automation method that covers this case. That test automation fails as expected if 
        ///     FixUpTransactionSourceOutputIdForDuplicateTransactionHash is commented out.
        /// </remarks>
        public void FixupTransactionSourceOutputIdForDuplicateTransactionHash()
        {
            const string sqlUpdateSourceTransactionOutputIdCommand = @"
                UPDATE TransactionInput
                SET SourceTransactionOutputId = (
                    SELECT TOP 1 TransactionOutput.TransactionOutputId
                    FROM TransactionOutput 
                    INNER JOIN TransactionInputSource ON TransactionInputSource.SourceTransactionOutputIndex = TransactionOutput.OutputIndex 
                    INNER JOIN BitcoinTransaction ON 
                        BitcoinTransaction.TransactionHash = TransactionInputSource.SourceTransactionHash
                        AND BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                    WHERE 
                        TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                        AND TransactionInputSource.SourceTransactionOutputIndex != -1
                        AND TransactionOutput.BitcoinTransactionId < TransactionInput.BitcoinTransactionId
                    ORDER BY TransactionOutput.TransactionOutputId DESC 
                )
                FROM TransactionInput 
                INNER JOIN TransactionInputSource ON TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                WHERE 
                    TransactionInputSource.SourceTransactionHash IN (
                        SELECT TransactionHash
                        FROM BitcoinTransaction
                        GROUP BY TransactionHash
                        HAVING COUNT(1) > 1)";

            this.adoNetLayer.ExecuteStatementNoResult(sqlUpdateSourceTransactionOutputIdCommand);
        }

        public void GetMaximumIdValues(out int blockchainFileId, out long blockId, out long bitcoinTransactionId, out long transactionInputId, out long transactionOutputId)
        {
            blockchainFileId = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BlockchainFileId) from BlockchainFile"), -1);
            blockId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BlockId) from Block"), -1);
            bitcoinTransactionId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(BitcoinTransactionId) from BitcoinTransaction"), -1);
            transactionInputId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(TransactionInputId) from TransactionInput"), -1);
            transactionOutputId = AdoNetLayer.ConvertDbValue<long>(this.adoNetLayer.ExecuteScalar("SELECT MAX(TransactionOutputId) from TransactionOutput"), -1);
        }

        public void GetDatabaseEntitiesCount(out int blockchainFileCount, out int blockCount, out int transactionCount, out int transactionInputCount, out int transactionOutputCount)
        {
            blockchainFileCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1) from BlockchainFile"));
            blockCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1)  from Block"));
            transactionCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1)  from BitcoinTransaction"));
            transactionInputCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1) from TransactionInput"));
            transactionOutputCount = AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT COUNT(1)  from TransactionOutput"));
        }

        public void AddBlockchainFile(BlockchainFile blockchainFile)
        {
            this.adoNetLayer.ExecuteStatementNoResult(
                "INSERT INTO BlockchainFile(BlockchainFileId, BlockchainFileName) VALUES (@BlockchainFileId, @BlockchainFileName)",
                AdoNetLayer.CreateInputParameter("@BlockchainFileId", SqlDbType.Int, blockchainFile.BlockchainFileId),
                AdoNetLayer.CreateInputParameter("@BlockchainFileName", SqlDbType.NVarChar, blockchainFile.BlockchainFileName));
        }

        public void BulkCopyTable(DataTable dataTable)
        {
            this.adoNetLayer.BulkCopyTable(dataTable.TableName, dataTable, DefaultDbCommandTimeout);
        }

        ////public void AddBlock(BlockchainDataSet.BlockDataTable blockDataTable)
        ////{
        ////    this.adoNetLayer.BulkCopyTable("Block", blockDataTable);
        ////}

        /////// <summary>
        /////// Bulk inserts all given transactions.
        /////// </summary>
        /////// <param name="bitcoinTransactionDataTable">
        /////// A table containing data for all transactions that will be inserted.
        /////// </param>
        ////public void AddTransactions(BlockchainDataSet.BitcoinTransactionDataTable bitcoinTransactionDataTable)
        ////{
        ////    this.adoNetLayer.BulkCopyTable("BitcoinTransaction", bitcoinTransactionDataTable);
        ////}

        /////// <summary>
        /////// Bulk inserts in batches all given transaction inputs.
        /////// </summary>
        /////// <param name="transactionInputDataTable">
        /////// A table containing data for all transaction inputs that will be inserted.
        /////// </param>
        ////public void AddTransactionInputs(BlockchainDataSet.TransactionInputDataTable transactionInputDataTable)
        ////{
        ////    this.adoNetLayer.BulkCopyTable("TransactionInput", transactionInputDataTable);
        ////}

        /////// <summary>
        /////// Bulk inserts all given transaction input sources.
        /////// </summary>
        /////// <param name="transactionInputSourceDataTable">
        /////// A table containing data for all transaction input sources that will be inserted.
        /////// </param>
        ////public void AddTransactionInputSources(BlockchainDataSet.TransactionInputSourceDataTable transactionInputSourceDataTable)
        ////{
        ////    this.adoNetLayer.BulkCopyTable("TransactionInputSource", transactionInputSourceDataTable);
        ////}

        /////// <summary>
        /////// Bulk inserts all given transaction inputs.
        /////// </summary>
        /////// <param name="transactionOutputTable">
        /////// A table containing data for all transaction outputs that will be inserted.
        /////// </param>
        ////public void AddTransactionOutputs(DataTable transactionOutputTable)
        ////{
        ////    this.adoNetLayer.BulkCopyTable("TransactionOutput", transactionOutputTable);
        ////}

        public SummaryBlockDataSet GetSummaryBlockDataSet()
        {
            return this.GetDataSet<SummaryBlockDataSet>("SELECT BlockId, BlockHash, PreviousBlockHash FROM Block");
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
            int decrementAmount = 1;

            for (int i = 0; i < orderedBlocksDeleted.Count - 1; i++)
            {
                long blockId1 = orderedBlocksDeleted[i];
                long blockId2 = orderedBlocksDeleted[i + 1];

                this.adoNetLayer.ExecuteStatementNoResult(
                    sqlCommandUpdateBlockBlockIdSection,
                    AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.Int, decrementAmount),
                    AdoNetLayer.CreateInputParameter("@BlockId1", SqlDbType.BigInt, blockId1),
                    AdoNetLayer.CreateInputParameter("@BlockId2", SqlDbType.BigInt, blockId2));

                this.adoNetLayer.ExecuteStatementNoResult(
                    sqlCommandUpdateTransactionBlockIdSection,
                    AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.Int, decrementAmount),
                    AdoNetLayer.CreateInputParameter("@BlockId1", SqlDbType.BigInt, blockId1),
                    AdoNetLayer.CreateInputParameter("@BlockId2", SqlDbType.BigInt, blockId2));

                decrementAmount++;
            }

            long blockId = orderedBlocksDeleted[orderedBlocksDeleted.Count - 1];

            this.adoNetLayer.ExecuteStatementNoResult(
                sqlCommandUpdateBlockBlockIdLastSection,
                AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.BigInt, decrementAmount),
                AdoNetLayer.CreateInputParameter("@BlockId", SqlDbType.BigInt, blockId));

            this.adoNetLayer.ExecuteStatementNoResult(
                sqlCommandUpdateTransactionBlockIdLastSection,
                AdoNetLayer.CreateInputParameter("@DecrementAmount", SqlDbType.BigInt, decrementAmount),
                AdoNetLayer.CreateInputParameter("@BlockId", SqlDbType.BigInt, blockId));
        }

        public void DisableAllHeavyIndexes()
        {
            foreach (IndexInfoDataSet.IndexInfoRow indexInfoRow in this.GetAllHeavyIndexes().IndexInfo)
            {
                string disableIndexStatement = string.Format(
                    CultureInfo.InvariantCulture,
                    "ALTER INDEX {0} ON {1} DISABLE",
                    indexInfoRow.IndexName,
                    indexInfoRow.TableName);

                this.adoNetLayer.ExecuteStatementNoResult(disableIndexStatement);
            }
        }

        public void RebuildAllHeavyIndexes(Action onSectionExecuted)
        {
            foreach (IndexInfoDataSet.IndexInfoRow indexInfoRow in this.GetAllHeavyIndexes().IndexInfo)
            {
                if (onSectionExecuted != null)
                {
                    onSectionExecuted();
                }

                string disableIndexStatement = string.Format(
                    CultureInfo.InvariantCulture,
                    "ALTER INDEX {0} ON {1} REBUILD",
                    indexInfoRow.IndexName,
                    indexInfoRow.TableName);

                this.adoNetLayer.ExecuteStatementNoResult(disableIndexStatement);
            }
        }

        public void ShrinkDatabase(string databaseName)
        {
            string shrinkStatement = string.Format(CultureInfo.InvariantCulture, "DBCC SHRINKDATABASE ([{0}])", databaseName);
            this.adoNetLayer.ExecuteStatementNoResult(shrinkStatement);
        }

        public bool IsDatabaseEmpty()
        {
            return AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar("SELECT CASE WHEN EXISTS (SELECT 1 FROM Block) THEN 0 ELSE 1 END AS IsEmpty")) == 1;
        }

        public bool IsSchemaSetup()
        {
            return AdoNetLayer.ConvertDbValue<int>(this.adoNetLayer.ExecuteScalar(
                "SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BtcDbSettings') THEN 1 ELSE 0 END AS IsSchemaSetup")) == 1;
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

        private IndexInfoDataSet GetAllHeavyIndexes()
        {
            return this.GetDataSet<IndexInfoDataSet>(@"
                SELECT 
                    sys.tables.name as TableName, 
                    sys.indexes.name AS IndexName 
                FROM sys.indexes
                INNER JOIN sys.tables ON sys.tables.object_id = sys.indexes.object_id
                WHERE 
                    sys.indexes.type_desc = 'NONCLUSTERED'
                    AND sys.tables.name != 'BlockchainFile'");
        }
    }
}
