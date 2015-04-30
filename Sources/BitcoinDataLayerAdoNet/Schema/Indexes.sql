-------------------------------------------------------------------------
-- <copyright file="Indexes.sql">
-- Copyright © Ladislau Molnar. All rights reserved.
-- </copyright>
-------------------------------------------------------------------------

-- START SECTION
CREATE INDEX IX_BitcoinTransaction_TransactionHash ON BitcoinTransaction(TransactionHash)

-- START SECTION
CREATE INDEX IX_BitcoinTransaction_BlockId ON BitcoinTransaction(BlockId)

-- START SECTION
CREATE INDEX IX_TransactionInput_BitcoinTransactionId ON TransactionInput(BitcoinTransactionId)

-- START SECTION
CREATE INDEX IX_TransactionInput_SourceTransactionOutputId ON TransactionInput(SourceTransactionOutputId)

-- START SECTION
CREATE INDEX IX_TransactionOutput_BitcoinTransactionId ON TransactionOutput(BitcoinTransactionId)

