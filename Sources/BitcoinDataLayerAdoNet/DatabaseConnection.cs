//-----------------------------------------------------------------------
// <copyright file="DatabaseConnection.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Globalization;

    public class DatabaseConnection
    {
        /// <summary>
        /// Used as the name of the SQL Server to indicate the Local DB.
        /// Note that the Local DB has a hard limit of 10 GB for the size of the database so it is not an option for a real scenario.
        /// This is only used for test automation scenarios for integration tests.
        /// </summary>
        private const string LocalDbSqlServerName = "(localDb)";

        private readonly string sqlUserName;
        private readonly string sqlPassword;

        private DatabaseConnection(string sqlServerName, string databaseName, string sqlUserName, string sqlPassword)
        {
            this.SqlServerName = sqlServerName;
            this.DatabaseName = databaseName;
            this.sqlUserName = sqlUserName;
            this.sqlPassword = sqlPassword;

            this.SetConnectionStrings();
        }

        public string SqlServerName { get; private set; }

        public string DatabaseName { get; private set; }

        public string ConnectionString { get; private set; }

        public string MasterConnectionString { get; private set; }

        public static DatabaseConnection CreateSqlServerConnection(string sqlServerName, string databaseName, string sqlUserName = null, string sqlPassword = null)
        {
            return new DatabaseConnection(sqlServerName, databaseName, sqlUserName, sqlPassword);
        }

        public static DatabaseConnection CreateLocalDbConnection(string databaseName)
        {
            return new DatabaseConnection(LocalDbSqlServerName, databaseName, null, null);
        }

        private void SetConnectionStrings()
        {
            if (this.SqlServerName == LocalDbSqlServerName)
            {
                this.ConnectionString = string.Format(
                       CultureInfo.InvariantCulture,
                       @"Data Source=(localdb)\mssqllocaldb;Initial Catalog={0};Integrated Security=True",
                       this.DatabaseName);

                this.MasterConnectionString = string.Format(
                       CultureInfo.InvariantCulture,
                       @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=master;Integrated Security=True");
            }
            else
            {
                if (this.sqlUserName == null && this.sqlPassword == null)
                {
                    this.ConnectionString = string.Format(
                            CultureInfo.InvariantCulture,
                            @"Server={0};database={1};Integrated Security=True",
                            this.SqlServerName,
                            this.DatabaseName);

                    this.MasterConnectionString = string.Format(
                            CultureInfo.InvariantCulture,
                            @"Server={0};database=master;Integrated Security=True",
                            this.SqlServerName);
                }
                else
                {
                    this.ConnectionString = string.Format(
                            CultureInfo.InvariantCulture,
                            @"Server={0};database={1};uid={2};pwd={3}",
                            this.SqlServerName,
                            this.DatabaseName,
                            this.sqlUserName,
                            this.sqlPassword);

                    this.MasterConnectionString = string.Format(
                            CultureInfo.InvariantCulture,
                            @"Server={0};database=master;uid={1};pwd={2}",
                            this.SqlServerName,
                            this.sqlUserName,
                            this.sqlPassword);
                }
            }
        }
    }
}
