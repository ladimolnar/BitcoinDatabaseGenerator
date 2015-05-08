//-----------------------------------------------------------------------
// <copyright file="BitcoinDataLayerValidation.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.DataSets;

    /// <summary>
    /// This code section contains the methods of class <see cref="BitcoinDataLayer" /> that retrieve validation datasets.
    /// </summary>
    public partial class BitcoinDataLayer : IDisposable
    {
        public ValidationDataSetInfo<ValidationBlockchainDataSet> GetValidationBlockchainDataSet(int maxBlockchainFileId)
        {
            const string sqlCommandText = @"
                SELECT 
                    COUNT(1) AS BlockCount,
                    SUM(TransactionCount) AS TransactionCount,
                    SUM(TransactionInputCount) AS TransactionInputCount,
                    SUM(TotalInputBtc) AS TotalInputBtc,
                    SUM(TransactionOutputCount) AS TransactionOutputCount,
                    SUM(TotalOutputBtc) AS TotalOutputBtc,
                    SUM(TransactionFeeBtc) AS TransactionFeeBtc,
                    SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
                FROM View_BlockAggregated
                WHERE BlockchainFileId <= @MaxBlockchainFileId";

            return this.GetValidationDataSetInfo<ValidationBlockchainDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, maxBlockchainFileId));
        }

        public ValidationDataSetInfo<ValidationBlockchainFilesDataSet> GetValidationBlockchainFilesDataSet(int maxBlockchainFileId)
        {
            const string sqlCommandText = @"
                SELECT 
                    BlockchainFile.BlockchainFileId,
                    BlockchainFile.BlockchainFileName,
                    T1.BlockCount,
                    T1.TransactionCount,
                    T1.TransactionInputCount,
                    T1.TotalInputBtc,
                    T1.TransactionOutputCount,
                    T1.TotalOutputBtc,
                    T1.TransactionFeeBtc,
                    T1.TotalUnspentOutputBtc
                FROM BlockchainFile
                INNER JOIN (
                    SELECT 
                        BlockchainFileId,
                        COUNT(1) AS BlockCount,
                        SUM(TransactionCount) AS TransactionCount,
                        SUM(TransactionInputCount) AS TransactionInputCount,
                        SUM(TotalInputBtc) AS TotalInputBtc,
                        SUM(TransactionOutputCount) AS TransactionOutputCount,
                        SUM(TotalOutputBtc) AS TotalOutputBtc,
                        SUM(TransactionFeeBtc) AS TransactionFeeBtc,
                        SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
                    FROM View_BlockAggregated
                    GROUP BY BlockchainFileId
                    ) AS T1
                    ON T1.BlockchainFileId = BlockchainFile.BlockchainFileId
                WHERE BlockchainFile.BlockchainFileId <= @MaxBlockchainFileId
                ORDER BY BlockchainFile.BlockchainFileId";

            return this.GetValidationDataSetInfo<ValidationBlockchainFilesDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.Int, maxBlockchainFileId));
        }

        public ValidationDataSetInfo<ValidationBlockDataSet> GetValidationBlockSampleDataSet(long maxBlockchainFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    BlockId,
                    BlockchainFileId,
                    BlockVersion,
                    BlockHash,
                    PreviousBlockHash,
                    BlockTimestamp,
                    TransactionCount,
                    TransactionInputCount,
                    TotalInputBtc,
                    TransactionOutputCount,
                    TotalOutputBtc,
                    TransactionFeeBtc,
                    TotalUnspentOutputBtc
                FROM View_BlockAggregated
                WHERE 
                    BlockId <= (SELECT MAX(BlockId) FROM Block WHERE BlockchainFileId <= @MaxBlockchainFileId) 
                    AND BlockId % @SampleRatio = 0
                ORDER BY BlockId";

            return this.GetValidationDataSetInfo<ValidationBlockDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.BigInt, maxBlockchainFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationDataSetInfo<ValidationTransactionDataSet> GetValidationTransactionSampleDataSet(int maxBlockchainFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    BitcoinTransactionId,
                    BlockId,
                    TransactionHash,
                    TransactionVersion,
                    TransactionLockTime,
                    TransactionInputCount,
                    TotalInputBtc,
                    TransactionOutputCount,
                    TotalOutputBtc,
                    TransactionFeeBtc,
                    TotalUnspentOutputBtc
                FROM View_TransactionAggregated 
                WHERE 
                    BitcoinTransactionId <= (
                        SELECT MAX(BitcoinTransactionId) 
                        FROM BitcoinTransaction 
                        INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                        WHERE Block.BlockchainFileId <= @MaxBlockchainFileId) 
                    AND BitcoinTransactionId % @SampleRatio = 0
                ORDER BY BitcoinTransactionId";

            return this.GetValidationDataSetInfo<ValidationTransactionDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.BigInt, maxBlockchainFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationDataSetInfo<ValidationTransactionInputDataSet> GetValidationTransactionInputSampleDataSet(int maxBlockchainFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    TransactionInput.TransactionInputId,
                    TransactionInput.BitcoinTransactionId,
                    TransactionInput.SourceTransactionOutputId,
                    (   SELECT SUM(TransactionOutput.OutputValueBtc)
                        FROM TransactionOutput
                        WHERE TransactionOutput.TransactionOutputId = TransactionInput.SourceTransactionOutputId
                    ) AS TransactionInputValueBtc,
                    TransactionInputSource.SourceTransactionHash,
                    TransactionInputSource.SourceTransactionOutputIndex
                FROM TransactionInput
                INNER JOIN TransactionInputSource ON TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                WHERE 
                    TransactionInput.TransactionInputId <= (
                        SELECT MAX(TransactionInputId) 
                        FROM TransactionInput
                        INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                        INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                        WHERE Block.BlockchainFileId <= @MaxBlockchainFileId) 
                    AND TransactionInput.TransactionInputId % @SampleRatio = 0
                ORDER BY TransactionInput.TransactionInputId";

            return this.GetValidationDataSetInfo<ValidationTransactionInputDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.BigInt, maxBlockchainFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationDataSetInfo<ValidationTransactionOutputDataSet> GetValidationTransactionOutputSampleDataSet(int maxBlockchainFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    TransactionOutput.TransactionOutputId,
                    TransactionOutput.BitcoinTransactionId,
                    TransactionOutput.OutputIndex,
                    TransactionOutput.OutputValueBtc,
                    TransactionOutput.OutputScript,
                    CASE 
                        WHEN EXISTS (SELECT * FROM TransactionInput WHERE SourceTransactionOutputId = TransactionOutput.OutputIndex)
                        THEN 1
                        ELSE 0
                        END
                    AS IsSpent
                FROM TransactionOutput
                WHERE 
                    TransactionOutput.TransactionOutputId <= (
                        SELECT MAX(TransactionOutputId) 
                        FROM TransactionOutput
                        INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                        INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                        WHERE Block.BlockchainFileId <= @MaxBlockchainFileId) 
                    AND TransactionOutput.TransactionOutputId % @SampleRatio = 0
                ORDER BY TransactionOutput.TransactionOutputId";

            return this.GetValidationDataSetInfo<ValidationTransactionOutputDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockchainFileId", SqlDbType.BigInt, maxBlockchainFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        private ValidationDataSetInfo<T> GetValidationDataSetInfo<T>(string sqlCommandText, params SqlParameter[] sqlParameters) where T : DataSet, new()
        {
            T dataset = this.GetDataSet<T>(sqlCommandText, sqlParameters);
            return new ValidationDataSetInfo<T>(dataset, sqlCommandText, sqlParameters);
        }
    }
}
