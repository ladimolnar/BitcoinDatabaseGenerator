//-----------------------------------------------------------------------
// <copyright file="BitcoinDatabaseGeneratorTest.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BitcoinBlockchain.Data;
    using BitcoinDatabaseGenerator;
    using BitcoinDatabaseGeneratorIntegrationTest.Helpers;
    using BitcoinDataLayerAdoNet;
    using BitcoinDataLayerAdoNet.DataSets;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BitcoinDatabaseGeneratorTest
    {
        [TestMethod]
        public async Task SimpleCaseTest()
        {
            FakeDatabaseGeneratorParameters parameters = new FakeDatabaseGeneratorParameters(true, FakeDatabaseGeneratorParameters.AutoThreads);
            DatabaseConnection databaseConnection = DatabaseConnection.CreateLocalDbConnection(parameters.DatabaseName);

            DatabaseGenerator databaseGenerator = new DatabaseGenerator(
                parameters,
                databaseConnection,
                () => new FakeBlockchainParser(this.GetBlocksForSimpleScenario()));

            await databaseGenerator.GenerateAndPopulateDatabase();

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(databaseConnection.ConnectionString))
            {
                // ValidationBlockchainDataSet will give us the aggregate values per the entire blockchain.
                ValidationBlockchainDataSet validationBlockchainDataSet = bitcoinDataLayer.GetValidationBlockchainDataSet(100);
                Assert.AreEqual(1, validationBlockchainDataSet.ValidationBlockchain.Count);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].BlockCount);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].TransactionCount);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].TransactionInputCount);
                Assert.AreEqual(50, validationBlockchainDataSet.ValidationBlockchain[0].TotalInputBtc);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].TransactionOutputCount);
                Assert.AreEqual(99, validationBlockchainDataSet.ValidationBlockchain[0].TotalOutputBtc);
                Assert.AreEqual(1, validationBlockchainDataSet.ValidationBlockchain[0].TransactionFeeBtc);
                Assert.AreEqual(49, validationBlockchainDataSet.ValidationBlockchain[0].TotalUnspentOutputBtc);

                // @@@ Implement these.
                // bitcoinDataLayer.GetValidationBlockFilesDataSet(100);
                // bitcoinDataLayer.GetValidationBlockSampleDataSet(100, 1);
                // bitcoinDataLayer.GetValidationTransactionSampleDataSet(100, 1);
                // bitcoinDataLayer.GetValidationTransactionInputSampleDataSet(100, 1);
                // bitcoinDataLayer.GetValidationTransactionOutputSampleDataSet(100, 1);
            }
        }

        /// <summary>
        /// Simulates a case where there are two transactions with the same transaction hash value.
        /// The first transaction becomes is spent and then the second transaction with the same hash
        /// is placed in the blockchain and spent.
        /// The test makes sure that transaction inputs that refers to the duplicated hash will point
        /// to the correct output of the correct transaction.
        /// </summary>
        [TestMethod]
        public async Task DuplicateTransactionHashTest()
        {
            ByteArray duplicateTransactionHash = SampleByteArray.GetSampleByteArray(100);

            FakeDatabaseGeneratorParameters parameters = new FakeDatabaseGeneratorParameters(true, 1);
            DatabaseConnection databaseConnection = DatabaseConnection.CreateLocalDbConnection(parameters.DatabaseName);

            DatabaseGenerator databaseGenerator = new DatabaseGenerator(
                parameters,
                databaseConnection,
                () => new FakeBlockchainParser(this.GetBlocksForDuplicateTransactionHashScenario(duplicateTransactionHash)));

            await databaseGenerator.GenerateAndPopulateDatabase();

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(databaseConnection.ConnectionString))
            {
                // ValidationBlockchainDataSet will give us the aggregate values per the entire blockchain.
                ValidationBlockchainDataSet validationBlockchainDataSet = bitcoinDataLayer.GetValidationBlockchainDataSet(100);
                Assert.AreEqual(1, validationBlockchainDataSet.ValidationBlockchain.Count);

                Assert.AreEqual(8, validationBlockchainDataSet.ValidationBlockchain[0].BlockCount);
                Assert.AreEqual(8, validationBlockchainDataSet.ValidationBlockchain[0].TransactionCount);
                Assert.AreEqual(8, validationBlockchainDataSet.ValidationBlockchain[0].TransactionInputCount);
                Assert.AreEqual(39, validationBlockchainDataSet.ValidationBlockchain[0].TotalInputBtc);
                Assert.AreEqual(11, validationBlockchainDataSet.ValidationBlockchain[0].TransactionOutputCount);
                Assert.AreEqual(69, validationBlockchainDataSet.ValidationBlockchain[0].TotalOutputBtc);
                Assert.AreEqual(0, validationBlockchainDataSet.ValidationBlockchain[0].TransactionFeeBtc);
                Assert.AreEqual(30, validationBlockchainDataSet.ValidationBlockchain[0].TotalUnspentOutputBtc);

                // ValidationBlockFilesDataSet will give us the aggregate values per block files. 
                // In this setup we have one block per block file. 
                ValidationBlockFilesDataSet validationBlockFilesDataSet = bitcoinDataLayer.GetValidationBlockFilesDataSet(100);
                Assert.AreEqual(8, validationBlockFilesDataSet.ValidationBlockFiles.Count);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile0 = validationBlockFilesDataSet.ValidationBlockFiles[0];
                Assert.AreEqual(0, blockFile0.BlockFileId);
                Assert.AreEqual("blk00000.dat", blockFile0.FileName);
                Assert.AreEqual(1, blockFile0.BlockCount);
                Assert.AreEqual(1, blockFile0.TransactionCount);
                Assert.AreEqual(1, blockFile0.TransactionInputCount);
                Assert.AreEqual(true, blockFile0.IsTotalInputBtcNull());
                Assert.AreEqual(1, blockFile0.TransactionOutputCount);
                Assert.AreEqual(10, blockFile0.TotalOutputBtc);
                Assert.AreEqual(0, blockFile0.TransactionFeeBtc);
                Assert.AreEqual(0, blockFile0.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile1 = validationBlockFilesDataSet.ValidationBlockFiles[1];
                Assert.AreEqual(1, blockFile1.BlockFileId);
                Assert.AreEqual("blk00001.dat", blockFile1.FileName);
                Assert.AreEqual(1, blockFile1.BlockCount);
                Assert.AreEqual(1, blockFile1.TransactionCount);
                Assert.AreEqual(1, blockFile1.TransactionInputCount);
                Assert.AreEqual(true, blockFile1.IsTotalInputBtcNull());
                Assert.AreEqual(1, blockFile1.TransactionOutputCount);
                Assert.AreEqual(10, blockFile1.TotalOutputBtc);
                Assert.AreEqual(0, blockFile1.TransactionFeeBtc);
                Assert.AreEqual(0, blockFile1.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile2 = validationBlockFilesDataSet.ValidationBlockFiles[2];
                Assert.AreEqual(2, blockFile2.BlockFileId);
                Assert.AreEqual("blk00002.dat", blockFile2.FileName);
                Assert.AreEqual(1, blockFile2.BlockCount);
                Assert.AreEqual(1, blockFile2.TransactionCount);
                Assert.AreEqual(1, blockFile2.TransactionInputCount);
                Assert.AreEqual(10, blockFile2.TotalInputBtc);
                Assert.AreEqual(2, blockFile2.TransactionOutputCount);
                Assert.AreEqual(10, blockFile2.TotalOutputBtc);
                Assert.AreEqual(0, blockFile2.TransactionFeeBtc);
                Assert.AreEqual(7, blockFile2.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile3 = validationBlockFilesDataSet.ValidationBlockFiles[3];
                Assert.AreEqual(3, blockFile3.BlockFileId);
                Assert.AreEqual("blk00003.dat", blockFile3.FileName);
                Assert.AreEqual(1, blockFile3.BlockCount);
                Assert.AreEqual(1, blockFile3.TransactionCount);
                Assert.AreEqual(1, blockFile3.TransactionInputCount);
                Assert.AreEqual(3, blockFile3.TotalInputBtc);
                Assert.AreEqual(1, blockFile3.TransactionOutputCount);
                Assert.AreEqual(3, blockFile3.TotalOutputBtc);
                Assert.AreEqual(0, blockFile3.TransactionFeeBtc);
                Assert.AreEqual(3, blockFile3.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile4 = validationBlockFilesDataSet.ValidationBlockFiles[4];
                Assert.AreEqual(4, blockFile4.BlockFileId);
                Assert.AreEqual("blk00004.dat", blockFile4.FileName);
                Assert.AreEqual(1, blockFile4.BlockCount);
                Assert.AreEqual(1, blockFile4.TransactionCount);
                Assert.AreEqual(1, blockFile4.TransactionInputCount);
                Assert.AreEqual(10, blockFile4.TotalInputBtc);
                Assert.AreEqual(2, blockFile4.TransactionOutputCount);
                Assert.AreEqual(10, blockFile4.TotalOutputBtc);
                Assert.AreEqual(0, blockFile4.TransactionFeeBtc);
                Assert.AreEqual(4, blockFile4.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile5 = validationBlockFilesDataSet.ValidationBlockFiles[5];
                Assert.AreEqual(5, blockFile5.BlockFileId);
                Assert.AreEqual("blk00005.dat", blockFile5.FileName);
                Assert.AreEqual(1, blockFile5.BlockCount);
                Assert.AreEqual(1, blockFile5.TransactionCount);
                Assert.AreEqual(1, blockFile5.TransactionInputCount);
                Assert.AreEqual(6, blockFile5.TotalInputBtc);
                Assert.AreEqual(1, blockFile5.TransactionOutputCount);
                Assert.AreEqual(6, blockFile5.TotalOutputBtc);
                Assert.AreEqual(0, blockFile5.TransactionFeeBtc);
                Assert.AreEqual(6, blockFile5.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile6 = validationBlockFilesDataSet.ValidationBlockFiles[6];
                Assert.AreEqual(6, blockFile6.BlockFileId);
                Assert.AreEqual("blk00006.dat", blockFile6.FileName);
                Assert.AreEqual(1, blockFile6.BlockCount);
                Assert.AreEqual(1, blockFile6.TransactionCount);
                Assert.AreEqual(1, blockFile6.TransactionInputCount);
                Assert.AreEqual(true, blockFile6.IsTotalInputBtcNull());
                Assert.AreEqual(1, blockFile6.TransactionOutputCount);
                Assert.AreEqual(10, blockFile6.TotalOutputBtc);
                Assert.AreEqual(0, blockFile6.TransactionFeeBtc);
                Assert.AreEqual(0, blockFile6.TotalUnspentOutputBtc);

                ValidationBlockFilesDataSet.ValidationBlockFilesRow blockFile7 = validationBlockFilesDataSet.ValidationBlockFiles[7];
                Assert.AreEqual(7, blockFile7.BlockFileId);
                Assert.AreEqual("blk00007.dat", blockFile7.FileName);
                Assert.AreEqual(1, blockFile7.BlockCount);
                Assert.AreEqual(1, blockFile7.TransactionCount);
                Assert.AreEqual(1, blockFile7.TransactionInputCount);
                Assert.AreEqual(10, blockFile7.TotalInputBtc);
                Assert.AreEqual(2, blockFile7.TransactionOutputCount);
                Assert.AreEqual(10, blockFile7.TotalOutputBtc);
                Assert.AreEqual(0, blockFile7.TransactionFeeBtc);
                Assert.AreEqual(10, blockFile7.TotalUnspentOutputBtc);

                ValidationTransactionInputDataSet validationTransactionInputDataSet = bitcoinDataLayer.GetValidationTransactionInputSampleDataSet(100, 1);
                Assert.AreEqual(8, validationTransactionInputDataSet.ValidationTransactionInput.Count);

                ValidationTransactionInputDataSet.ValidationTransactionInputRow transactionInput3 = validationTransactionInputDataSet.ValidationTransactionInput[3];
                Assert.AreEqual(3, transactionInput3.TransactionInputId);
                Assert.AreEqual(3, transactionInput3.BitcoinTransactionId);
                Assert.AreEqual(2, transactionInput3.SourceTransactionOutputId);
                Assert.AreEqual(3, transactionInput3.TransactionInputValueBtc);
                Assert.AreEqual(duplicateTransactionHash, new ByteArray(transactionInput3.SourceTransactionHash));
                Assert.AreEqual(0, transactionInput3.SourceTransactionOutputIndex);

                ValidationTransactionInputDataSet.ValidationTransactionInputRow transactionInput5 = validationTransactionInputDataSet.ValidationTransactionInput[5];
                Assert.AreEqual(5, transactionInput5.TransactionInputId);
                Assert.AreEqual(5, transactionInput5.BitcoinTransactionId);
                Assert.AreEqual(6, transactionInput5.SourceTransactionOutputId);
                Assert.AreEqual(6, transactionInput5.TransactionInputValueBtc);
                Assert.AreEqual(duplicateTransactionHash, new ByteArray(transactionInput5.SourceTransactionHash));
                Assert.AreEqual(1, transactionInput5.SourceTransactionOutputIndex);
            }
        }

        private IEnumerable<Block> GetBlocksForSimpleScenario()
        {
            DataHelper dataHelper = new DataHelper();

            Block block1 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(1),
                transactionHash: SampleByteArray.GetSampleByteArray(1),
                input: new InputInfo(ByteArray.Empty, TransactionInput.OutputIndexNotUsed),
                output: new OutputInfo(50));

            Block block2 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(2),
                transactionHash: SampleByteArray.GetSampleByteArray(2),
                input: new InputInfo(block1.Transactions[0].TransactionHash, 0),
                output: new OutputInfo(49));

            return new List<Block>()
            {
                block1,
                block2,
            };
        }

        private IEnumerable<Block> GetBlocksForDuplicateTransactionHashScenario(ByteArray duplicateTransactionHash)
        {
            DataHelper dataHelper = new DataHelper();

            Block block1 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(1),
                transactionHash: SampleByteArray.GetSampleByteArray(1),
                input: new InputInfo(ByteArray.Empty, TransactionInput.OutputIndexNotUsed),
                output: new OutputInfo(10));

            Block block2 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(2),
                transactionHash: SampleByteArray.GetSampleByteArray(2),
                input: new InputInfo(ByteArray.Empty, TransactionInput.OutputIndexNotUsed),
                output: new OutputInfo(10));

            // The block that contains the first instance of a duplicate transaction hash.
            Block block3 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(3),
                transactionHash: duplicateTransactionHash,
                inputs: new InputInfo[] { new InputInfo(block1.Transactions[0].TransactionHash, 0) },
                outputs: new OutputInfo[] { new OutputInfo(3), new OutputInfo(7) });

            // This block spends the first output of the first transaction that has the duplicate hash.
            Block block4 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(4),
                transactionHash: SampleByteArray.GetSampleByteArray(4),
                input: new InputInfo(duplicateTransactionHash, 0),
                output: new OutputInfo(3));

            // The block that contains the second instance of a duplicate transaction hash.
            // Technically this makes the unspent outputs of the first transaction that has the same hash unspendable.
            Block block5 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(5),
                transactionHash: duplicateTransactionHash,
                inputs: new InputInfo[] { new InputInfo(block2.Transactions[0].TransactionHash, 0) },
                outputs: new OutputInfo[] { new OutputInfo(4), new OutputInfo(6) });

            // This block spends the second output of the second transaction that has the duplicate hash.
            Block block6 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(6),
                transactionHash: SampleByteArray.GetSampleByteArray(6),
                input: new InputInfo(duplicateTransactionHash, 1),
                output: new OutputInfo(6));

            Block block7 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(7),
                transactionHash: SampleByteArray.GetSampleByteArray(7),
                input: new InputInfo(ByteArray.Empty, TransactionInput.OutputIndexNotUsed),
                output: new OutputInfo(10));

            // The block that contains the third instance of a duplicate transaction hash.
            // Technically this makes the unspent outputs of the first two transaction that have the same hash unspendable.
            Block block8 = dataHelper.GenerateBlock(
                blockHash: SampleByteArray.GetSampleByteArray(8),
                transactionHash: duplicateTransactionHash,
                inputs: new InputInfo[] { new InputInfo(block7.Transactions[0].TransactionHash, 0) },
                outputs: new OutputInfo[] { new OutputInfo(2), new OutputInfo(8) });

            return new List<Block>()
            {
                block1,
                block2,
                block3,
                block4,
                block5,
                block6,
                block7,
                block8,
            };
        }
    }
}
