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
        private readonly string sqlUserName;
        private readonly string sqlPassword;
        private readonly Lazy<string> connectionString;

        public DatabaseConnection(string sqlServerName, string databaseName, string sqlUserName = null, string sqlPassword = null)
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
            //// This is the connection string used for localdb
            //// return string.Format(
            ////    CultureInfo.InvariantCulture,
            ////    @"Data Source=(localdb)\mssqllocaldb;Initial Catalog={0};Integrated Security=True",
            ////    this.databaseConnection.DatabaseName);

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
