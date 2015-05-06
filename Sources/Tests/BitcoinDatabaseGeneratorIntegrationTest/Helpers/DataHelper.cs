//-----------------------------------------------------------------------
// <copyright file="DataHelper.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Helpers
{
    using System;
    using System.Globalization;
    using BitcoinBlockchain.Data;
    using BitcoinDatabaseGenerator;

    public class DataHelper
    {
        private int blockFileIndex;
        private DateTime blockTimeStamp;
        private ByteArray previousBlockHash;

        public DataHelper()
        {
            this.blockTimeStamp = new DateTime(2010, 1, 1);
            this.blockFileIndex = 0;
            this.previousBlockHash = ByteArray.Empty;
        }

        public Block GenerateBlock(
            ByteArray blockHash,
            ByteArray transactionHash,
            InputInfo input,
            OutputInfo output)
        {
            return this.GenerateBlock(
                blockHash,
                transactionHash,
                new InputInfo[] { input },
                new OutputInfo[] { output });
        }

        public Block GenerateBlock(
            ByteArray blockHash,
            ByteArray transactionHash,
            InputInfo[] inputs,
            OutputInfo[] outputs)
        {
            BlockHeader blockHeader = new BlockHeader()
            {
                BlockHash = blockHash,
                BlockNonce = 0,
                BlockTargetDifficulty = 0,
                BlockTimestamp = this.GetCurrentBlockTimeStamp(),
                BlockTimestampUnix = 0,
                BlockVersion = 1,
                MerkleRootHash = ByteArray.Empty,
                PreviousBlockHash = this.previousBlockHash,
            };

            this.blockTimeStamp = this.blockTimeStamp.AddDays(1);

            string expectedFileName = string.Format(CultureInfo.InvariantCulture, "blk{0:00000}.dat", this.GetCurentBlockchainFileIndex());
            Block block = new Block(expectedFileName, blockHeader);

            Transaction transaction = new Transaction()
            {
                TransactionHash = transactionHash,
                TransactionLockTime = 0,
                TransactionVersion = 1,
            };

            foreach (InputInfo inputInfo in inputs)
            {
                TransactionInput transactionInput = new TransactionInput()
                {
                    InputScript = ByteArray.Empty,
                    SourceTransactionHash = inputInfo.SourceTransactionHash,
                    SourceTransactionOutputIndex = inputInfo.SourceTransactionOutputIndex,
                };
                transaction.AddInput(transactionInput);
            }

            foreach (OutputInfo outputInfo in outputs)
            {
                TransactionOutput transactionOutput = new TransactionOutput()
                {
                    OutputScript = ByteArray.Empty,
                    OutputValueSatoshi = (ulong)(outputInfo.OutputValueSatoshi * DatabaseGenerator.BtcToSatoshi),
                };

                transaction.AddOutput(transactionOutput);
            }

            block.AddTransaction(transaction);

            this.previousBlockHash = block.BlockHeader.BlockHash;

            return block;
        }

        private int GetCurentBlockchainFileIndex()
        {
            return this.blockFileIndex++;
        }

        private DateTime GetCurrentBlockTimeStamp()
        {
            DateTime currentBlockTimeStamp = this.blockTimeStamp;
            this.blockTimeStamp = this.blockTimeStamp.AddDays(1);
            return currentBlockTimeStamp;
        }
    }
}
