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
-- VIEW View_SummaryBlock
-- Use this view to regenerate the typed dataset UnspentOutputsDataSet.xsd
--=============================================================================
CREATE VIEW View_UnspentOutputs AS
SELECT TOP 1
    SourceTransaction.BitcoinTransactionId,
    SourceTransaction.TransactionHash,
    TransactionOutput.TransactionOutputId,
    TransactionOutput.OutputIndex
FROM TransactionOutput 
INNER JOIN BitcoinTransaction SourceTransaction ON SourceTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
GO
