//-----------------------------------------------------------------------
// <copyright file="BitcoinDataLayerValidation.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Data;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.DataSets;

    /// <summary>
    /// This code section contains the methods of class <see cref="BitcoinDataLayer" /> that retrieve validation datasets.
    /// </summary>
    public partial class BitcoinDataLayer : IDisposable
    {
        public ValidationBlockchainDataSet GetValidationBlockchainDataSet(int maxBlockFileId)
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
                WHERE BlockFileId <= @MaxBlockFileId";

            return this.GetDataSet<ValidationBlockchainDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.Int, maxBlockFileId));
        }

        public ValidationBlockFilesDataSet GetValidationBlockFilesDataSet(int maxBlockFileId)
        {
            const string sqlCommandText = @"
                SELECT 
                    BlockFile.BlockFileId,
                    BlockFile.FileName,
                    T1.BlockCount,
                    T1.TransactionCount,
                    T1.TransactionInputCount,
                    T1.TotalInputBtc,
                    T1.TransactionOutputCount,
                    T1.TotalOutputBtc,
                    T1.TransactionFeeBtc,
                    T1.TotalUnspentOutputBtc
                FROM BlockFile
                INNER JOIN (
                    SELECT 
                        BlockFileId,
                        COUNT(1) AS BlockCount,
                        SUM(TransactionCount) AS TransactionCount,
                        SUM(TransactionInputCount) AS TransactionInputCount,
                        SUM(TotalInputBtc) AS TotalInputBtc,
                        SUM(TransactionOutputCount) AS TransactionOutputCount,
                        SUM(TotalOutputBtc) AS TotalOutputBtc,
                        SUM(TransactionFeeBtc) AS TransactionFeeBtc,
                        SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
                    FROM View_BlockAggregated
                    GROUP BY BlockFileId
                    ) AS T1
                    ON T1.BlockFileId = BlockFile.BlockFileId
                WHERE BlockFile.BlockFileId <= @MaxBlockFileId
                ORDER BY BlockFile.BlockFileId";

            return this.GetDataSet<ValidationBlockFilesDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.Int, maxBlockFileId));
        }

        public ValidationBlockDataSet GetValidationBlockSampleDataSet(long maxBlockFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    BlockId,
                    BlockFileId,
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
                    BlockId <= (SELECT MAX(BlockId) FROM Block WHERE BlockFileId <= @MaxBlockFileId) 
                    AND BlockId % @SampleRatio = 0
                ORDER BY BlockId";

            return this.GetDataSet<ValidationBlockDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationTransactionDataSet GetValidationTransactionSampleDataSet(int maxBlockFileId, int sampleRatio)
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
                        WHERE Block.BlockFileId <= @MaxBlockFileId) 
                    AND BitcoinTransactionId % @SampleRatio = 0
                ORDER BY BitcoinTransactionId";

            return this.GetDataSet<ValidationTransactionDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationTransactionInputDataSet GetValidationTransactionInputSampleDataSet(int maxBlockFileId, int sampleRatio)
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
                        WHERE Block.BlockFileId <= @MaxBlockFileId) 
                    AND TransactionInput.TransactionInputId % @SampleRatio = 0
                ORDER BY TransactionInput.TransactionInputId";

            return this.GetDataSet<ValidationTransactionInputDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationTransactionOutputDataSet GetValidationTransactionOutputSampleDataSet(int maxBlockFileId, int sampleRatio)
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
                        WHERE Block.BlockFileId <= @MaxBlockFileId) 
                    AND TransactionOutput.TransactionOutputId % @SampleRatio = 0
                ORDER BY TransactionOutput.TransactionOutputId";

            return this.GetDataSet<ValidationTransactionOutputDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }
    }
}
