//-----------------------------------------------------------------------
// <copyright file="DatabaseManager.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.Properties;
    using Microsoft.SqlServer.Management.Smo;

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
            foreach (string sqlCommand in GetSqlSections(true, true))
            {
                sb.AppendLine(sqlCommand);
                sb.AppendLine("GO");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Deletes the given database if it exists.
        /// </summary>
        /// <returns>
        /// true - the database was found and deleted
        /// false - no database was found.
        /// </returns>
        public bool DeleteDatabaseIfExists()
        {
            Server server = this.GetServer();

            Database database = server.Databases[this.databaseConnection.DatabaseName];
            if (database != null)
            {
                // We use server.KillDatabase instead of database.Drop() because after opening a SqlConnection even if disposing it, 
                // database.Drop() will fail as the DB will be in use. Could that be because the framework manages a pool of connections? 
                server.KillDatabase(this.databaseConnection.DatabaseName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the given database already exists.
        /// </summary>
        /// <returns>
        /// True if the database exists, false otherwise.
        /// </returns>
        public bool DatabaseExists()
        {
            Server server = this.GetServer();
            return server.Databases[this.databaseConnection.DatabaseName] != null;
        }

        public bool EnsureDatabaseExists()
        {
            Server server = this.GetServer();
            Database database = server.Databases[this.databaseConnection.DatabaseName];

            if (database == null)
            {
                this.CreateNewDatabase();
                return true;
            }

            return false;
        }

        public void CreateNewDatabase()
        {
            Server server = this.GetServer();
            Database database = new Database(server, this.databaseConnection.DatabaseName);

            database.Create();

            this.ExecuteDatabaseSetupStatements(true, false);
        }

        public void CreateDatabaseIndexes(Action onSectionExecuted)
        {
            this.ExecuteDatabaseSetupStatements(false, true, onSectionExecuted);
        }

        public void ExecuteDatabaseSetupStatements(bool setupInitialSchema, bool setupIndexes, Action onSectionExecuted = null)
        {
            int timeoutInSeconds = 180;
            if (setupIndexes)
            {
                timeoutInSeconds = 3600;
            }

            string connectionString = this.databaseConnection.ConnectionString;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                AdoNetLayer adoNetLayer = new AdoNetLayer(sqlConnection, timeoutInSeconds);

                foreach (string sqlCommand in GetSqlSections(setupInitialSchema, setupIndexes))
                {
                    adoNetLayer.ExecuteStatementNoResult(sqlCommand);
                    if (onSectionExecuted != null)
                    {
                        onSectionExecuted();
                    }
                }
            }
        }

        private static IEnumerable<string> GetSqlSections(bool includeInitialSchema, bool includeIndexes)
        {
            List<string> sqlCommandsList = new List<string>();

            if (includeInitialSchema)
            {
                sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.Tables));
                sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.SeedData));
                sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.Views));
            }

            if (includeIndexes)
            {
                sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.Indexes));
            }

            return sqlCommandsList;
        }

        private static IEnumerable<string> GenerateSqlCommandsFromResourceText(string sqlCommandsText)
        {
            string[] sqlCommandsArray = sqlCommandsText.Split(new string[] { "-- START SECTION" }, StringSplitOptions.None);

            // The first section always contains the file level comments.
            return sqlCommandsArray.Skip(1);
        }

        private Server GetServer()
        {
            if (this.databaseConnection.SqlServerName == DatabaseConnection.LocalDbSqlServerName)
            {
                return new Server("(localdb)\\mssqllocaldb");
            }
            else
            {
                return new Server(this.databaseConnection.GetServerConnection());
            }
        }
    }
}
