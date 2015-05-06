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
            DatabaseConnection databaseConnection = DatabaseConnection.CreateLocalDbConnection(parameters.SqlDbName);

            DatabaseGenerator databaseGenerator = new DatabaseGenerator(
                parameters,
                databaseConnection,
                () => new FakeBlockchainParser(this.GetBlocksForSimpleScenario()));

            await databaseGenerator.GenerateAndPopulateDatabase();

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(databaseConnection.ConnectionString))
            {
                // ValidationBlockchainDataSet will give us the aggregate values per the entire blockchain.
                ValidationBlockchainDataSet validationBlockchainDataSet = bitcoinDataLayer.GetValidationBlockchainDataSet(100).DataSet;
                Assert.AreEqual(1, validationBlockchainDataSet.ValidationBlockchain.Count);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].BlockCount);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].TransactionCount);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].TransactionInputCount);
                Assert.AreEqual(50, validationBlockchainDataSet.ValidationBlockchain[0].TotalInputBtc);
                Assert.AreEqual(2, validationBlockchainDataSet.ValidationBlockchain[0].TransactionOutputCount);
                Assert.AreEqual(99, validationBlockchainDataSet.ValidationBlockchain[0].TotalOutputBtc);
                Assert.AreEqual(1, validationBlockchainDataSet.ValidationBlockchain[0].TransactionFeeBtc);
                Assert.AreEqual(49, validationBlockchainDataSet.ValidationBlockchain[0].TotalUnspentOutputBtc);

                // TODO: Implement the next part of the validation.
                // bitcoinDataLayer.GetValidationBlockchainFilesDataSet(100);
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
            DatabaseConnection databaseConnection = DatabaseConnection.CreateLocalDbConnection(parameters.SqlDbName);

            DatabaseGenerator databaseGenerator = new DatabaseGenerator(
                parameters,
                databaseConnection,
                () => new FakeBlockchainParser(this.GetBlocksForDuplicateTransactionHashScenario(duplicateTransactionHash)));

            await databaseGenerator.GenerateAndPopulateDatabase();

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(databaseConnection.ConnectionString))
            {
                // ValidationBlockchainDataSet will give us the aggregate values per the entire blockchain.
                ValidationBlockchainDataSet validationBlockchainDataSet = bitcoinDataLayer.GetValidationBlockchainDataSet(100).DataSet;
                Assert.AreEqual(1, validationBlockchainDataSet.ValidationBlockchain.Count);

                Assert.AreEqual(8, validationBlockchainDataSet.ValidationBlockchain[0].BlockCount);
                Assert.AreEqual(8, validationBlockchainDataSet.ValidationBlockchain[0].TransactionCount);
                Assert.AreEqual(8, validationBlockchainDataSet.ValidationBlockchain[0].TransactionInputCount);
                Assert.AreEqual(39, validationBlockchainDataSet.ValidationBlockchain[0].TotalInputBtc);
                Assert.AreEqual(11, validationBlockchainDataSet.ValidationBlockchain[0].TransactionOutputCount);
                Assert.AreEqual(69, validationBlockchainDataSet.ValidationBlockchain[0].TotalOutputBtc);
                Assert.AreEqual(0, validationBlockchainDataSet.ValidationBlockchain[0].TransactionFeeBtc);
                Assert.AreEqual(30, validationBlockchainDataSet.ValidationBlockchain[0].TotalUnspentOutputBtc);

                // ValidationBlockchainFilesDataSet will give us the aggregate values per block files. 
                // In this setup we have one block per block file. 
                ValidationBlockchainFilesDataSet validationBlockchainFilesDataSet = bitcoinDataLayer.GetValidationBlockchainFilesDataSet(100).DataSet;
                Assert.AreEqual(8, validationBlockchainFilesDataSet.ValidationBlockchainFiles.Count);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile0 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[0];
                Assert.AreEqual(0, blockchainFile0.BlockchainFileId);
                Assert.AreEqual("blk00000.dat", blockchainFile0.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile0.BlockCount);
                Assert.AreEqual(1, blockchainFile0.TransactionCount);
                Assert.AreEqual(1, blockchainFile0.TransactionInputCount);
                Assert.AreEqual(0, blockchainFile0.TotalInputBtc);
                Assert.AreEqual(1, blockchainFile0.TransactionOutputCount);
                Assert.AreEqual(10, blockchainFile0.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile0.TransactionFeeBtc);
                Assert.AreEqual(0, blockchainFile0.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile1 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[1];
                Assert.AreEqual(1, blockchainFile1.BlockchainFileId);
                Assert.AreEqual("blk00001.dat", blockchainFile1.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile1.BlockCount);
                Assert.AreEqual(1, blockchainFile1.TransactionCount);
                Assert.AreEqual(1, blockchainFile1.TransactionInputCount);
                Assert.AreEqual(0, blockchainFile1.TotalInputBtc);
                Assert.AreEqual(1, blockchainFile1.TransactionOutputCount);
                Assert.AreEqual(10, blockchainFile1.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile1.TransactionFeeBtc);
                Assert.AreEqual(0, blockchainFile1.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile2 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[2];
                Assert.AreEqual(2, blockchainFile2.BlockchainFileId);
                Assert.AreEqual("blk00002.dat", blockchainFile2.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile2.BlockCount);
                Assert.AreEqual(1, blockchainFile2.TransactionCount);
                Assert.AreEqual(1, blockchainFile2.TransactionInputCount);
                Assert.AreEqual(10, blockchainFile2.TotalInputBtc);
                Assert.AreEqual(2, blockchainFile2.TransactionOutputCount);
                Assert.AreEqual(10, blockchainFile2.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile2.TransactionFeeBtc);
                Assert.AreEqual(7, blockchainFile2.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile3 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[3];
                Assert.AreEqual(3, blockchainFile3.BlockchainFileId);
                Assert.AreEqual("blk00003.dat", blockchainFile3.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile3.BlockCount);
                Assert.AreEqual(1, blockchainFile3.TransactionCount);
                Assert.AreEqual(1, blockchainFile3.TransactionInputCount);
                Assert.AreEqual(3, blockchainFile3.TotalInputBtc);
                Assert.AreEqual(1, blockchainFile3.TransactionOutputCount);
                Assert.AreEqual(3, blockchainFile3.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile3.TransactionFeeBtc);
                Assert.AreEqual(3, blockchainFile3.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile4 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[4];
                Assert.AreEqual(4, blockchainFile4.BlockchainFileId);
                Assert.AreEqual("blk00004.dat", blockchainFile4.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile4.BlockCount);
                Assert.AreEqual(1, blockchainFile4.TransactionCount);
                Assert.AreEqual(1, blockchainFile4.TransactionInputCount);
                Assert.AreEqual(10, blockchainFile4.TotalInputBtc);
                Assert.AreEqual(2, blockchainFile4.TransactionOutputCount);
                Assert.AreEqual(10, blockchainFile4.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile4.TransactionFeeBtc);
                Assert.AreEqual(4, blockchainFile4.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile5 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[5];
                Assert.AreEqual(5, blockchainFile5.BlockchainFileId);
                Assert.AreEqual("blk00005.dat", blockchainFile5.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile5.BlockCount);
                Assert.AreEqual(1, blockchainFile5.TransactionCount);
                Assert.AreEqual(1, blockchainFile5.TransactionInputCount);
                Assert.AreEqual(6, blockchainFile5.TotalInputBtc);
                Assert.AreEqual(1, blockchainFile5.TransactionOutputCount);
                Assert.AreEqual(6, blockchainFile5.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile5.TransactionFeeBtc);
                Assert.AreEqual(6, blockchainFile5.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile6 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[6];
                Assert.AreEqual(6, blockchainFile6.BlockchainFileId);
                Assert.AreEqual("blk00006.dat", blockchainFile6.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile6.BlockCount);
                Assert.AreEqual(1, blockchainFile6.TransactionCount);
                Assert.AreEqual(1, blockchainFile6.TransactionInputCount);
                Assert.AreEqual(0, blockchainFile6.TotalInputBtc);
                Assert.AreEqual(1, blockchainFile6.TransactionOutputCount);
                Assert.AreEqual(10, blockchainFile6.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile6.TransactionFeeBtc);
                Assert.AreEqual(0, blockchainFile6.TotalUnspentOutputBtc);

                ValidationBlockchainFilesDataSet.ValidationBlockchainFilesRow blockchainFile7 = validationBlockchainFilesDataSet.ValidationBlockchainFiles[7];
                Assert.AreEqual(7, blockchainFile7.BlockchainFileId);
                Assert.AreEqual("blk00007.dat", blockchainFile7.BlockchainFileName);
                Assert.AreEqual(1, blockchainFile7.BlockCount);
                Assert.AreEqual(1, blockchainFile7.TransactionCount);
                Assert.AreEqual(1, blockchainFile7.TransactionInputCount);
                Assert.AreEqual(10, blockchainFile7.TotalInputBtc);
                Assert.AreEqual(2, blockchainFile7.TransactionOutputCount);
                Assert.AreEqual(10, blockchainFile7.TotalOutputBtc);
                Assert.AreEqual(0, blockchainFile7.TransactionFeeBtc);
                Assert.AreEqual(10, blockchainFile7.TotalUnspentOutputBtc);

                ValidationTransactionInputDataSet validationTransactionInputDataSet = bitcoinDataLayer.GetValidationTransactionInputSampleDataSet(100, 1).DataSet;
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
