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

        private IEnumerable<Block> GetBlocksForSimpleScenario()
        {
            BlockHeader blockHeader1 = new BlockHeader()
            {
                BlockHash = SampleByteArray.GetSampleByteArray(1),
                BlockNonce = 0,
                BlockTargetDifficulty = 0,
                BlockTimestamp = new DateTime(2010, 1, 1),
                BlockTimestampUnix = 0,
                BlockVersion = 1,
                MerkleRootHash = ByteArray.Empty,
                PreviousBlockHash = ByteArray.Empty,
            };

            Block block1 = new Block("blk00000.dat", blockHeader1);

            Transaction transaction1 = new Transaction()
            {
                TransactionHash = SampleByteArray.GetSampleByteArray(1),
                TransactionLockTime = 0,
                TransactionVersion = 1,
            };

            TransactionInput transactionInput1 = new TransactionInput()
            {
                InputScript = ByteArray.Empty,
                SourceTransactionHash = ByteArray.Empty,
                SourceTransactionOutputIndex = TransactionInput.OutputIndexNotUsed,
            };

            TransactionOutput transactionOutput1 = new TransactionOutput()
            {
                OutputScript = ByteArray.Empty,
                OutputValueSatoshi = 50 * DatabaseGenerator.BtcToSatoshi,
            };

            transaction1.AddInput(transactionInput1);
            transaction1.AddOutput(transactionOutput1);

            block1.AddTransaction(transaction1);

            BlockHeader blockHeader2 = new BlockHeader()
            {
                BlockHash = SampleByteArray.GetSampleByteArray(2),
                BlockNonce = 0,
                BlockTargetDifficulty = 0,
                BlockTimestamp = new DateTime(2010, 1, 2),
                BlockTimestampUnix = 0,
                BlockVersion = 1,
                MerkleRootHash = ByteArray.Empty,
                PreviousBlockHash = blockHeader1.BlockHash,
            };

            Block block2 = new Block("blk00001.dat", blockHeader2);

            Transaction transaction2 = new Transaction()
            {
                TransactionHash = SampleByteArray.GetSampleByteArray(2),
                TransactionLockTime = 0,
                TransactionVersion = 1,
            };

            TransactionInput transactionInput2 = new TransactionInput()
            {
                InputScript = ByteArray.Empty,
                SourceTransactionHash = block1.Transactions[0].TransactionHash,
                SourceTransactionOutputIndex = 0,
            };

            TransactionOutput transactionOutput2 = new TransactionOutput()
            {
                OutputScript = ByteArray.Empty,
                OutputValueSatoshi = 49 * DatabaseGenerator.BtcToSatoshi,
            };

            transaction2.AddInput(transactionInput2);
            transaction2.AddOutput(transactionOutput2);

            block2.AddTransaction(transaction2);

            return new List<Block>()
            {
                block1,
                block2,
            };
        }
    }
}
