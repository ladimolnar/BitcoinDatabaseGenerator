-------------------------------------------------------------------------
-- <copyright file="Tables.sql">
-- Copyright © Ladislau Molnar. All rights reserved.
-- </copyright>
-------------------------------------------------------------------------

-- START SECTION
CREATE TABLE BlockFile (
    BlockFileId                     INT PRIMARY KEY                         NOT NULL,
    [FileName]                      NVARCHAR (300)                          NOT NULL
);

ALTER TABLE BlockFile ADD CONSTRAINT BlockFile_FileName UNIQUE([FileName])


CREATE TABLE Block (
    BlockId                         BIGINT PRIMARY KEY                      NOT NULL,
    BlockFileId                     INT                                     NOT NULL,
    BlockVersion                    INT                                     NOT NULL,
    BlockHash                       VARBINARY (32)                          NOT NULL,
    PreviousBlockHash               VARBINARY (32)                          NOT NULL,
    BlockTimestamp                  DATETIME                                NOT NULL
);


CREATE TABLE BitcoinTransaction (
    BitcoinTransactionId            BIGINT PRIMARY KEY                      NOT NULL,
    BlockId                         BIGINT                                  NOT NULL,
    TransactionHash                 VARBINARY (32)                          NOT NULL,
    TransactionVersion              INT                                     NOT NULL,
    TransactionLockTime             INT                                     NOT NULL,
);


CREATE TABLE TransactionInput (
    TransactionInputId              BIGINT PRIMARY KEY                      NOT NULL,
    BitcoinTransactionId            BIGINT                                  NOT NULL,
    SourceTransactionOutputId       BIGINT                                  NULL,
);


CREATE TABLE TransactionOutput (
    TransactionOutputId             BIGINT PRIMARY KEY                      NOT NULL,
    BitcoinTransactionId            BIGINT                                  NOT NULL,
    OutputIndex                     INT                                     NOT NULL,
    OutputValueBtc                  NUMERIC(20,8)                           NOT NULL,
    OutputScript                    VARBINARY (MAX)                         NOT NULL
);


CREATE TABLE BtcDbSettings (
    PropertyName                    NVARCHAR (32)                           NOT NULL,
    PropertyValue                   NVARCHAR (MAX)                          NOT NULL
)
