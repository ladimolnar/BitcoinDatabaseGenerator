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
            foreach (string sqlCommand in GetSqlSections())
            {
                sb.AppendLine(sqlCommand);
                sb.AppendLine("GO");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Deletes the given database.
        /// </summary>
        public void DeleteDatabase()
        {
            Server server = this.GetServer();

            // We use server.KillDatabase instead of database.Drop() because after opening a SqlConnection even if disposing it, 
            // database.Drop() will fail as the DB will be in use. Could that be because the framework manages a pool of connections? 
            server.KillDatabase(this.databaseConnection.DatabaseName);
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

            this.ExecuteDatabaseSetupStatements();
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
            sqlCommandsList.AddRange(GenerateSqlCommandsFromResourceText(Resources.Indexes));

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
