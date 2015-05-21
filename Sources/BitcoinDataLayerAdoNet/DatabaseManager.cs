//-----------------------------------------------------------------------
// <copyright file="DatabaseManager.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.Properties;

    public class DatabaseManager
    {
        private readonly DatabaseConnection databaseConnection;

        public DatabaseManager(DatabaseConnection databaseConnection)
        {
            this.databaseConnection = databaseConnection;
        }

        public static string GetDatabaseSchema()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string sqlCommand in GetSqlSections())
            {
                sb.Append(sqlCommand);
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        public void CreateNewDatabase()
        {
            string connectionString = this.databaseConnection.MasterConnectionString;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                AdoNetLayer adoNetLayer = new AdoNetLayer(sqlConnection);

                adoNetLayer.ExecuteStatementNoResult(string.Format(CultureInfo.InvariantCulture, "CREATE DATABASE {0}", this.databaseConnection.DatabaseName));
            }
        }

        public bool DatabaseExists()
        {
            string connectionString = this.databaseConnection.MasterConnectionString;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                AdoNetLayer adoNetLayer = new AdoNetLayer(sqlConnection);

                return AdoNetLayer.ConvertDbValue<int>(adoNetLayer.ExecuteScalar(
                    "SELECT CASE WHEN EXISTS (SELECT * FROM sys.databases WHERE [Name] = @DatabaseName) THEN 1 ELSE 0 END AS DatabaseExists",
                    AdoNetLayer.CreateInputParameter("@DatabaseName", SqlDbType.NVarChar, this.databaseConnection.DatabaseName))) == 1;
            }
        }

        public void DeleteDatabase()
        {
            string connectionString = this.databaseConnection.MasterConnectionString;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                AdoNetLayer adoNetLayer = new AdoNetLayer(sqlConnection);

                string takeDbOffline = string.Format(CultureInfo.InvariantCulture, "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", this.databaseConnection.DatabaseName);
                string deleteDb = string.Format(CultureInfo.InvariantCulture, "DROP DATABASE [{0}]", this.databaseConnection.DatabaseName);

                adoNetLayer.ExecuteStatementNoResult(takeDbOffline);
                adoNetLayer.ExecuteStatementNoResult(deleteDb);
            }
        }

        public void ExecuteDatabaseSetupStatements()
        {
            string connectionString = this.databaseConnection.ConnectionString;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                AdoNetLayer adoNetLayer = new AdoNetLayer(sqlConnection);

                foreach (string sqlCommand in GetSqlSections())
                {
                    adoNetLayer.ExecuteStatementNoResult(sqlCommand);
                }
            }
        }

        private static IEnumerable<string> GetSqlSections()
        {
            List<string> sqlCommandsList = new List<string>();

            sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.Tables));
            sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.SeedData));
            sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.Views));

            return sqlCommandsList;
        }

        private static IEnumerable<string> GenerateSqlCommandsFromResourceText(string sqlCommandsText)
        {
            string[] sqlCommandsArray = sqlCommandsText.Split(new string[] { "-- START SECTION" }, StringSplitOptions.None);

            // The first section always contains the file level comments.
            return sqlCommandsArray.Skip(1);
        }
    }
}
