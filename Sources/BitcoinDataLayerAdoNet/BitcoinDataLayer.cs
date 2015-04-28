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
        /// The default timeout in seconds that is used for each SQL command created internally 
        /// by this instance of <see cref="AdoNetLayer"/>.
        /// </summary>
        public const int DefaultBitcoinCommandTimeout = 1200;

        private readonly SqlConnection sqlConnection;
        private readonly AdoNetLayer adoNetLayer;

        public BitcoinDataLayer(string connectionString, int commandTimeout = DefaultBitcoinCommandTimeout)
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

        /// <summary>
        /// Provides information about all unspent outputs that are stored in the database.
        /// </summary>
        /// <returns>
        /// A <see cref="SqlDataReader"/> that can be used to retrieve information about all unspent outputs that are stored in the database.
        /// </returns>
        public SqlDataReader GetUnspentOutputsReader()
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

            return this.adoNetLayer.ExecuteStatementReader(selectUnspentOutputs);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.sqlConnection.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "DataSet instances do not have to be disposed.")]
        private DataSet GetDataSet(string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            DataSet dataSet = new DataSet { Locale = CultureInfo.InvariantCulture };
            this.adoNetLayer.FillDataSetFromStatement(dataSet, sqlCommandText, sqlParameters);
            return dataSet;
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
