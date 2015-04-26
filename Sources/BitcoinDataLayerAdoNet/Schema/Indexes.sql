-------------------------------------------------------------------------
-- <copyright file="Indexes.sql">
-- Copyright © Ladislau Molnar. All rights reserved.
-- </copyright>
-------------------------------------------------------------------------

-- START SECTION
CREATE INDEX IX_BitcoinTransaction_TransactionHash ON BitcoinTransaction(TransactionHash)
CREATE INDEX IX_BitcoinTransaction_BlockId ON BitcoinTransaction(BlockId)

CREATE INDEX IX_TransactionInput_BitcoinTransactionId ON TransactionInput(BitcoinTransactionId)
CREATE INDEX IX_TransactionInput_SourceTransactionOutputId ON TransactionInput(SourceTransactionOutputId)

CREATE INDEX IX_TransactionOutput_BitcoinTransactionId ON TransactionOutput(BitcoinTransactionId)

