--=============================================================================
-- <copyright file="DataSetViews.sql">
-- Copyright © Ladislau Molnar. All rights reserved.
-- </copyright>
--=============================================================================

--=============================================================================
-- This file contains views that are not added to the database in a 
-- production environment but that can be useful in a development 
-- environment. These views will have to be created manually.
--=============================================================================

--=============================================================================
-- VIEW View_SummaryBlock
-- Use this view to regenerate the typed dataset SummaryBlockDataSet.xsd
--=============================================================================
CREATE VIEW View_SummaryBlock AS SELECT BlockId, BlockHash, PreviousBlockHash FROM Block
GO

--=============================================================================
-- VIEW View_ValidationBlockchain
-- Use this view to regenerate the typed dataset ValidationBlockchainDataSet.xsd
--=============================================================================
CREATE VIEW View_ValidationBlockchain AS
SELECT TOP 1
    COUNT(1) AS BlockCount,
    SUM(TransactionCount) AS TransactionCount,
    SUM(TransactionInputCount) AS TransactionInputCount,
    SUM(TotalInputBtc) AS TotalInputBtc,
    SUM(TransactionOutputCount) AS TransactionOutputCount,
    SUM(TotalOutputBtc) AS TotalOutputBtc,
    SUM(TransactionFeeBtc) AS TransactionFeeBtc,
    SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
FROM View_BlockAggregated
GO

--=============================================================================
-- VIEW View_ValidationBlockchainFiles
-- Use this view to regenerate the typed dataset ValidationBlockchainFilesDataSet.xsd
--=============================================================================
CREATE VIEW View_ValidationBlockchainFiles AS
SELECT TOP 1
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
GO

--=============================================================================
-- VIEW View_ValidationBlock
-- Use this view to regenerate the typed dataset ValidationBlockDataSet.xsd
--=============================================================================
CREATE VIEW View_ValidationBlock AS
SELECT TOP 1
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
GO

--=============================================================================
-- VIEW View_ValidationTransaction
-- Use this view to regenerate the typed dataset ValidationTransactionDataSet.xsd
--=============================================================================
CREATE VIEW View_ValidationTransaction AS
SELECT TOP 1
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
GO

--=============================================================================
-- VIEW View_ValidationTransactionInput
-- Use this view to regenerate the typed dataset ValidationTransactionInputDataSet.xsd
--=============================================================================
CREATE VIEW View_ValidationTransactionInput AS
SELECT TOP 1
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
GO

--=============================================================================
-- VIEW View_ValidationTransactionOutput
-- Use this view to regenerate the typed dataset ValidationTransactionOutputDataSet.xsd
--=============================================================================
CREATE VIEW View_ValidationTransactionOutput AS
SELECT TOP 1
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
GO

--=============================================================================
-- VIEW View_IndexInfo
-- Use this view to regenerate the typed dataset IndexInfoDataSet.xsd
--=============================================================================
CREATE VIEW View_IndexInfo AS
SELECT TOP 1
    sys.indexes.name AS IndexName,
    sys.tables.name as TableName
FROM sys.indexes
INNER JOIN sys.tables ON sys.tables.object_id = sys.indexes.object_id
WHERE sys.indexes.type_desc = 'NONCLUSTERED'
GO

