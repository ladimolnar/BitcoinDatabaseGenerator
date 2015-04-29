//-----------------------------------------------------------------------
// <copyright file="AutoValidator.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using BitcoinDataLayerAdoNet;
    using ResharperAnnotations;

    public class AutoValidator
    {
        private const int ValidationSqlCommandTimeout = 1200;

        private readonly DatabaseConnection databaseConnection;

        public AutoValidator(string validationDatabaseName)
        {
            this.databaseConnection = DatabaseConnection.CreateSqlServerConnection("localhost", validationDatabaseName);
        }

        public async Task<bool> Validate()
        {
            await this.PrepareDumpFolder();
            return this.ValidateDataAgainstBaseline();
        }

        private static bool CompareFiles(string pathToFile1, string pathToFile2)
        {
            string contentOfFirstFile = File.ReadAllText(pathToFile1);
            string[] linesInFirstFile = contentOfFirstFile.Split(new char[] { '\n' });

            string contentOfSecondFile = File.ReadAllText(pathToFile2);
            string[] linesInSecondFile = contentOfSecondFile.Split(new char[] { '\n' });

            if (linesInFirstFile.Length != linesInSecondFile.Length)
            {
                Console.Error.WriteLine("The dataset files have different sizes. See files:\n{0}\n{1}", pathToFile1, pathToFile2);
                return false;
            }

            for (int lineIndex = 0; lineIndex < linesInFirstFile.Length; lineIndex++)
            {
                if (string.CompareOrdinal(linesInFirstFile[lineIndex], linesInSecondFile[lineIndex]) != 0)
                {
                    Console.Error.WriteLine("The dataset files have different content in line {0}. See files:\n{1}\n{2}", lineIndex, pathToFile1, pathToFile2);
                    return false;
                }
            }

            Console.WriteLine("Dataset verified.");
            return true;
        }

        private static string GetPathToDumpFolder()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", System.IO.Path.GetTempPath(), "BitcoinDatabaseGenerator");
        }

        private static void DumpResultsToFile(StreamWriter dumpFile, DataSet dataSet)
        {
            DataTable dataTable = dataSet.Tables[0];

            dumpFile.WriteLine("Columns:");
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                dumpFile.WriteLine(dataTable.Columns[i].ColumnName);
            }

            dumpFile.WriteLine();

            for (int r = 0; r < dataTable.Rows.Count; r++)
            {
                DataRow row = dataTable.Rows[r];
                dumpFile.WriteLine("Row {0}", r);

                for (int c = 0; c < dataTable.Columns.Count; c++)
                {
                    dumpFile.Write(DbValueTostring(row[c]));
                    dumpFile.WriteLine();
                }

                dumpFile.WriteLine();
            }
        }

        private static string DbValueTostring(object value)
        {
            byte[] byteArray = value as byte[];
            if (byteArray != null)
            {
                return BitConverter.ToString(byteArray);
            }

            if (value is DateTime)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} [Ticks: {1}]", value.ToString(), ((DateTime)value).Ticks.ToString(CultureInfo.InvariantCulture));
            }

            return value.ToString();
        }

        private static string CopyBaselineFile(string validationDatasetFileName)
        {
            string pathToOriginalBaselineFile = string.Format(
                CultureInfo.InvariantCulture,
                "{0}\\ValidationBaseline\\{1}.txt",
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                validationDatasetFileName);

            string pathToDumpBaselineFile = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_Baseline.txt", GetPathToDumpFolder(), validationDatasetFileName);

            File.Copy(pathToOriginalBaselineFile, pathToDumpBaselineFile);

            return pathToDumpBaselineFile;
        }

        private static string GetPathToDumpFile(string validationDatasetFileName)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_Actual.txt", GetPathToDumpFolder(), validationDatasetFileName);
        }

        private static bool ValidateDataSet(string validationDatasetFileName, [InstantHandle] Func<DataSet> retrieveDataSet)
        {
            Console.WriteLine();
            Console.WriteLine("Validating dataset: {0}. Please wait...", validationDatasetFileName);

            string pathToBaselineFile = CopyBaselineFile(validationDatasetFileName);

            DataSet dataSet = retrieveDataSet();
            Console.WriteLine("{0} rows were retrieved.", dataSet.Tables[0].Rows.Count);

            string pathToDumpFile = GetPathToDumpFile(validationDatasetFileName);
            using (StreamWriter dumpFile = new StreamWriter(pathToDumpFile))
            {
                dumpFile.WriteLine("Validation dataset: {0}\r\n", validationDatasetFileName);
                DumpResultsToFile(dumpFile, dataSet);
            }

            return CompareFiles(pathToDumpFile, pathToBaselineFile);
        }

        private bool ValidateDataAgainstBaseline()
        {
            //// These are values we can use to produce validation baselines for a samller sample.
            //// const int maxBlockFileId = 3;
            //// const int blockSampleRatio = 1000;
            //// const int transactionSampleRatio = 1000;
            //// const int transactionInputSampleRatio = 10000;
            //// const int transactionOutputSampleRatio = 10000;

            const int maxBlockFileId = 250;
            const int blockSampleRatio = 350;
            const int transactionSampleRatio = 6500;
            const int transactionInputSampleRatio = 200000;
            const int transactionOutputSampleRatio = 200000;

            bool validationResult;

            using (BitcoinDataLayer bitcoinDataLayer = new BitcoinDataLayer(this.databaseConnection.ConnectionString, ValidationSqlCommandTimeout))
            {
                validationResult = ValidateDataSet("01_BlockchainData", () => bitcoinDataLayer.GetValidationBlockchainDataSet(maxBlockFileId));
                validationResult = ValidateDataSet("02_BlockFilesData", () => bitcoinDataLayer.GetValidationBlockFilesDataSet(maxBlockFileId)) && validationResult;
                validationResult = ValidateDataSet("03_BlockSampleData", () => bitcoinDataLayer.GetValidationBlockSampleDataSet(maxBlockFileId, blockSampleRatio)) && validationResult;
                validationResult = ValidateDataSet("04_TransactionSampleData", () => bitcoinDataLayer.GetValidationTransactionSampleDataSet(maxBlockFileId, transactionSampleRatio)) && validationResult;
                validationResult = ValidateDataSet("05_TransactionInputSampleData", () => bitcoinDataLayer.GetValidationTransactionInputSampleDataSet(maxBlockFileId, transactionInputSampleRatio)) && validationResult;
                validationResult = ValidateDataSet("06_TransactionOutputSampleData", () => bitcoinDataLayer.GetValidationTransactionOutputSampleDataSet(maxBlockFileId, transactionOutputSampleRatio)) && validationResult;
            }

            return validationResult;
        }

        private async Task PrepareDumpFolder()
        {
            string pathToDumpFolder = GetPathToDumpFolder();
            if (Directory.Exists(pathToDumpFolder))
            {
                Directory.Delete(pathToDumpFolder, true);
            }

            // Wait for the folder to actually be deleted. Otherwise we'll create the new folder before the delete method completes 
            // and when the delete method eventually completes, the new folder will be deleted as well.
            while (Directory.Exists(pathToDumpFolder))
            {
                await Task.Delay(100);
            }

            Directory.CreateDirectory(pathToDumpFolder);
        }
    }
}
