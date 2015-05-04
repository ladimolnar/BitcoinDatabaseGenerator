-------------------------------------------------------------------------
-- <copyright file="Tables.sql">
-- Copyright © Ladislau Molnar. All rights reserved.
-- </copyright>
-------------------------------------------------------------------------

-- START SECTION

-- ==========================================================================
-- Note about all hashes:
-- The hashes are stored in reverse order from what is the normal result 
-- of hashing. This is so to be consistent with sites like blockchain.info 
-- and blockexporer that display hashes in 'big endian' format.
-- ==========================================================================

-- ==========================================================================
-- TABLE: BlockFile
-- Contains information about the blockchain files that were processed.
-- ==========================================================================
CREATE TABLE BlockFile (
    BlockFileId                     INT PRIMARY KEY                         NOT NULL,
    [FileName]                      NVARCHAR (300)                          NOT NULL
);

ALTER TABLE BlockFile ADD CONSTRAINT BlockFile_FileName UNIQUE([FileName])


-- ==========================================================================
-- TABLE: BlockFile
-- Contains information about the Bitcoin blocks.
-- ==========================================================================
CREATE TABLE Block (
    BlockId                         BIGINT PRIMARY KEY                      NOT NULL,
    BlockFileId                     INT                                     NOT NULL,
    BlockVersion                    INT                                     NOT NULL,

    -- Note: hash is in reverse order.
    BlockHash                       VARBINARY (32)                          NOT NULL,
    
    -- Note: hash is in reverse order.
    PreviousBlockHash               VARBINARY (32)                          NOT NULL,

    BlockTimestamp                  DATETIME                                NOT NULL
);


-- ==========================================================================
-- TABLE: BitcoinTransaction
-- Contains information about the Bitcoin transactions.
-- ==========================================================================
CREATE TABLE BitcoinTransaction (
    BitcoinTransactionId            BIGINT PRIMARY KEY                      NOT NULL,
    BlockId                         BIGINT                                  NOT NULL,

    -- Note: hash is in reverse order.
    TransactionHash                 VARBINARY (32)                          NOT NULL,

    TransactionVersion              INT                                     NOT NULL,
    TransactionLockTime             INT                                     NOT NULL,
);

CREATE INDEX IX_BitcoinTransaction_TransactionHash ON BitcoinTransaction(TransactionHash)
CREATE INDEX IX_BitcoinTransaction_BlockId ON BitcoinTransaction(BlockId)


-- ==========================================================================
-- TABLE: TransactionInput
-- Contains information about the Bitcoin transaction inputs.
-- ==========================================================================
CREATE TABLE TransactionInput (
    TransactionInputId              BIGINT PRIMARY KEY                      NOT NULL,
    BitcoinTransactionId            BIGINT                                  NOT NULL,
    
    -- This is the Id of the source transaction output.
    -- Set to NULL when this input has no corresponding output.
    -- Set to -1 in initial stages of the data transfer before the data from 
    -- TransactionInputSource is processed.
    -- The values in this column are calculated based on the links presented in
    -- the original blockchain and that are saved in table TransactionInputSource.
    -- This column is provided as a way to optimize queries where a join is
    -- required between an input and its corresponding output. 
    SourceTransactionOutputId       BIGINT                                  NULL,
);

CREATE INDEX IX_TransactionInput_BitcoinTransactionId ON TransactionInput(BitcoinTransactionId)
CREATE INDEX IX_TransactionInput_SourceTransactionOutputId ON TransactionInput(SourceTransactionOutputId)


-- ==========================================================================
-- TABLE: TransactionInputSource
-- Contains information about the source of the Bitcoin transaction inputs.
-- This table contains links between transaction inputs and their
-- corresponding source outputs. The data here is obtained from the blockchain
-- files that are transferred in the database. However, the transfer includes 
-- a stage where data from this table is processed and a more direct link is 
-- calculated and saved in TransactionInput.SourceTransactionOutputId. Any 
-- queries where a join is required between an input and its corresponding 
-- output should use TransactionInput.SourceTransactionOutputId.
-- ==========================================================================
CREATE TABLE TransactionInputSource (
    TransactionInputId              BIGINT PRIMARY KEY                      NOT NULL,

    -- The hash of the transaction that contains the output that is the source
    -- of this input.
    -- Note: hash is in reverse order.
    SourceTransactionHash           VARBINARY (32)                          NOT NULL,

    -- The index of the output that will be consumed by this input.
    -- The index is a zero based index in the list of outputs of the 
    -- transaction that it belongs to.
    -- Set to -1 to indicate that this input refers to no previous output.
    SourceTransactionOutputIndex    INT                                     NULL,
);


-- ==========================================================================
-- TABLE: TransactionInput
-- Contains information about the Bitcoin transaction outputs.
-- ==========================================================================
CREATE TABLE TransactionOutput (
    TransactionOutputId             BIGINT PRIMARY KEY                      NOT NULL,
    BitcoinTransactionId            BIGINT                                  NOT NULL,
    OutputIndex                     INT                                     NOT NULL,
    OutputValueBtc                  NUMERIC(20,8)                           NOT NULL,
    OutputScript                    VARBINARY (MAX)                         NOT NULL
);

CREATE INDEX IX_TransactionOutput_BitcoinTransactionId ON TransactionOutput(BitcoinTransactionId)


CREATE TABLE BtcDbSettings (
    PropertyName                    NVARCHAR (32)                           NOT NULL,
    PropertyValue                   NVARCHAR (MAX)                          NOT NULL
)
