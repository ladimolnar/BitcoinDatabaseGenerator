--=============================================================================
-- <copyright file="Views.sql">
-- Copyright © Ladislau Molnar. All rights reserved.
-- </copyright>
--=============================================================================

--=============================================================================
-- This file contains SQL views that are added to the database in a 
-- production environment when the database is created.
--=============================================================================

-- START SECTION
--=============================================================================
-- VIEW View_TransactionAggregated
-- Use this view retrieve aggregated data for a transaction including the 
-- total input, output and transaction fees. 
-- Example: 
--      SELECT * FROM View_TransactionAggregated WHERE BitcoinTransactionId = 914224
--=============================================================================
CREATE VIEW View_TransactionAggregated AS 
SELECT 
    BitcoinTransactionId,
    BlockId,
    TransactionHash,
    TransactionVersion,
    TransactionLockTime,
    ISNULL(TransactionInputCount, 0) AS TransactionInputCount,
    ISNULL(TotalInputBtc, 0) AS TotalInputBtc,
    ISNULL(TransactionOutputCount, 0) AS TransactionOutputCount,
    ISNULL(TotalOutputBtc, 0) AS TotalOutputBtc,
    ISNULL(TotalInputBtc - TotalOutputBtc, 0) AS TransactionFeeBtc,
    ISNULL(TotalUnspentOutputBtc, 0) AS TotalUnspentOutputBtc
FROM (
    SELECT 
        BitcoinTransaction.BitcoinTransactionId,
        BitcoinTransaction.BlockId,
        BitcoinTransaction.TransactionHash,
        BitcoinTransaction.TransactionVersion,
        BitcoinTransaction.TransactionLockTime,
        (   SELECT COUNT(1) 
            FROM TransactionInput
            WHERE BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId 
        ) AS TransactionInputCount,
        (   SELECT SUM(TransactionOutput.OutputValueBtc)
            FROM TransactionInput 
            INNER JOIN TransactionOutput ON TransactionOutput.TransactionOutputId = TransactionInput.SourceTransactionOutputId
            WHERE TransactionInput.BitcoinTransactionId = BitcoinTransaction.BitcoinTransactionId
        ) AS TotalInputBtc,
        (   SELECT COUNT(1) 
            FROM TransactionOutput
            WHERE BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId 
        ) AS TransactionOutputCount,
        (   SELECT SUM(TransactionOutput.OutputValueBtc)
            FROM TransactionOutput
            WHERE TransactionOutput.BitcoinTransactionId = BitcoinTransaction.BitcoinTransactionId
        ) AS TotalOutputBtc,
        (   SELECT SUM(TransactionOutput.OutputValueBtc)
            FROM TransactionOutput
            LEFT OUTER JOIN TransactionInput ON TransactionInput.SourceTransactionOutputId = TransactionOutput.TransactionOutputId
            WHERE 
                TransactionOutput.BitcoinTransactionId = BitcoinTransaction.BitcoinTransactionId
                AND TransactionInput.TransactionInputId IS NULL
        ) AS TotalUnspentOutputBtc
    FROM BitcoinTransaction) AS TransactionAggregated

-- START SECTION
--=============================================================================
-- VIEW View_BlockAggregated
-- Use this view retrieve aggregated data for a block including the 
-- total input, output and transaction fees. 
-- Example: 
--      SELECT * FROM View_BlockAggregated WHERE BlockId = 134181
--=============================================================================
CREATE VIEW View_BlockAggregated AS 
SELECT 
    Block.BlockId,
    Block.BlockchainFileId,
    Block.BlockVersion,
    Block.BlockHash,
    Block.PreviousBlockHash,
    Block.BlockTimestamp,
    BlockAggregated.TransactionCount,
    BlockAggregated.TransactionInputCount,
    BlockAggregated.TotalInputBtc,
    BlockAggregated.TransactionOutputCount,
    BlockAggregated.TotalOutputBtc,
    BlockAggregated.TransactionFeeBtc,
    BlockAggregated.TotalUnspentOutputBtc
FROM Block
INNER JOIN (
    SELECT 
        Block.BlockId,
        SUM(1) AS TransactionCount,
        SUM(TransactionInputCount) AS TransactionInputCount,
        SUM(TotalInputBtc) AS TotalInputBtc,
        SUM(TransactionOutputCount) AS TransactionOutputCount,
        SUM(TotalOutputBtc) AS TotalOutputBtc,
        SUM(TransactionFeeBtc) AS TransactionFeeBtc,
        SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
    FROM Block
    INNER JOIN View_TransactionAggregated ON Block.BlockId = View_TransactionAggregated.BlockId
    GROUP BY Block.BlockId
    ) AS BlockAggregated ON BlockAggregated.BlockId = Block.BlockId

-- START SECTION
--=============================================================================
-- VIEW View_BlockchainFileCounts
-- Use this view retrieve data about a blockchain file.
-- Example: 
--      SELECT * FROM View_BlockchainFileCounts WHERE BlockchainFileId = 100
--=============================================================================
CREATE VIEW View_BlockchainFileCounts AS 
SELECT 
    BlockchainFile.BlockchainFileId,
    BlockchainFileName,
    ( SELECT COUNT(1) FROM Block WHERE Block.BlockchainFileId = BlockchainFile.BlockchainFileId ) AS BlockCount,
    ( SELECT COUNT(1) 
      FROM BitcoinTransaction 
      INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
      WHERE Block.BlockchainFileId = BlockchainFile.BlockchainFileId 
    ) AS TransactionCount,
    ( SELECT COUNT(1) 
      FROM TransactionInput
      INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
      INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
      WHERE Block.BlockchainFileId = BlockchainFile.BlockchainFileId 
    ) AS TransactionInputCount,
    ( SELECT COUNT(1) 
      FROM TransactionOutput
      INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
      INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
      WHERE Block.BlockchainFileId = BlockchainFile.BlockchainFileId 
    ) AS TransactionOutputCount
FROM BlockchainFile

