//-----------------------------------------------------------------------
// <copyright file="DatabaseConnection.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Globalization;
    using Microsoft.SqlServer.Management.Common;

    public class DatabaseConnection
    {
        /// <summary>
        /// Used as the name of the SQL Server to indicate the Local DB.
        /// Note that the Local DB has a hard limit of 10 GB for the size of the database so it is not an option for a real scenario.
        /// This is only used for test automation scenarios for integration tests.
        /// </summary>
        public const string LocalDbSqlServerName = "(localDb)";

        private readonly string sqlUserName;
        private readonly string sqlPassword;
        private readonly Lazy<string> connectionString;

        private DatabaseConnection(string sqlServerName, string databaseName, string sqlUserName, string sqlPassword)
        {
            this.SqlServerName = sqlServerName;
            this.DatabaseName = databaseName;
            this.sqlUserName = sqlUserName;
            this.sqlPassword = sqlPassword;

            this.connectionString = new Lazy<string>(this.GetConnectionString);
        }

        public string SqlServerName { get; private set; }

        public string DatabaseName { get; private set; }

        public string ConnectionString
        {
            get { return this.connectionString.Value; }
        }

        public static DatabaseConnection CreateSqlServerConnection(string sqlServerName, string databaseName, string sqlUserName = null, string sqlPassword = null)
        {
            return new DatabaseConnection(sqlServerName, databaseName, sqlUserName, sqlPassword);
        }

        public static DatabaseConnection CreateLocalDbConnection(string databaseName)
        {
            return new DatabaseConnection(LocalDbSqlServerName, databaseName, null, null);
        }

        public ServerConnection GetServerConnection()
        {
            if (this.sqlUserName == null && this.sqlPassword == null)
            {
                return new ServerConnection(this.SqlServerName);
            }
            else
            {
                return new ServerConnection(this.SqlServerName, this.sqlUserName, this.sqlPassword);
            }
        }

        private string GetConnectionString()
        {
            if (this.SqlServerName == LocalDbSqlServerName)
            {
                return string.Format(
                   CultureInfo.InvariantCulture,
                   @"Data Source=(localdb)\mssqllocaldb;Initial Catalog={0};Integrated Security=True",
                   this.DatabaseName);
            }
            else
            {
                if (this.sqlUserName == null && this.sqlPassword == null)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        @"Server={0};database={1};Integrated Security=True",
                        this.SqlServerName,
                        this.DatabaseName);
                }
                else
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        @"Server={0};database={1};uid={2};pwd={3}",
                        this.SqlServerName,
                        this.DatabaseName,
                        this.sqlUserName,
                        this.sqlPassword);
                }
            }
        }
    }
}
