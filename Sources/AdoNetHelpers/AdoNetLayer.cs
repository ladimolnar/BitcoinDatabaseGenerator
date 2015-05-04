//-----------------------------------------------------------------------
// <copyright file="ADONetLayer.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace AdoNetHelpers
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Threading.Tasks;

    /// <summary>
    /// Class providing utility methods needed when using ADO.NET.
    /// </summary>
    public class AdoNetLayer
    {
        /// <summary>
        /// The default timeout in seconds that is used for each SQL command created internally 
        /// by this instance of <see cref="AdoNetLayer"/>.
        /// </summary>
        public const int DefaultCommandTimeout = 180;

        /// <summary>
        /// The ASO.NET SQL connection associated with this instance of <see cref="AdoNetLayer"/>.
        /// </summary>
        private readonly SqlConnection sqlConnection;

        /// <summary>
        /// The timeout in seconds that is used for each SQL command created internally 
        /// by this instance of <see cref="AdoNetLayer"/>. The default is defaultCommandTimeout.
        /// </summary>
        private readonly int commandTimeout;

        /// <summary>
        /// The ASO.NET SQL transaction associated with this instance of <see cref="AdoNetLayer"/>.
        /// </summary>
        private SqlTransaction sqlTransaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdoNetLayer" /> class.
        /// </summary>
        /// <param name="sqlConnection">
        /// The ADO.NET SQL connection that will be used by this instance of <see cref="AdoNetLayer"/>.
        /// </param>
        /// <param name="commandTimeout">
        /// The timeout in seconds that is used for each SQL command created internally 
        /// by this instance of <see cref="AdoNetLayer"/>.
        /// </param>
        public AdoNetLayer(SqlConnection sqlConnection, int commandTimeout)
        {
            this.sqlConnection = sqlConnection;
            this.commandTimeout = commandTimeout;
            this.sqlTransaction = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdoNetLayer"/> class.
        /// </summary>
        /// <param name="sqlConnection">
        /// The ADO.NET SQL connection that will be used by this instance of <see cref="AdoNetLayer"/>.
        /// </param>
        public AdoNetLayer(SqlConnection sqlConnection)
            : this(sqlConnection, DefaultCommandTimeout)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlParameter" /> class for an input parameter based on the given parameter name, type and value
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <returns>An instance of <see cref="SqlParameter" /> class.</returns>
        public static SqlParameter CreateInputParameter(
           string parameterName,
           SqlDbType parameterType,
           object parameterValue)
        {
            return CreateParameter(parameterName, parameterType, null, ParameterDirection.Input, parameterValue);
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlParameter" /> class based on the given parameter name, type, size and value.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterSize">The maximum size, in bytes, of the data associated with this parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <returns>An instance of <see cref="SqlParameter" /> class.</returns>
        public static SqlParameter CreateStoredParameter(
           string parameterName,
           SqlDbType parameterType,
           int parameterSize,
           object parameterValue)
        {
            return CreateParameter(parameterName, parameterType, parameterSize, ParameterDirection.Input, parameterValue);
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlParameter" /> class based on the given parameter name, type, size, direction and value.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterSize">The maximum size, in bytes, of the data associated with this parameter.</param>
        /// <param name="parameterDirection">The direction of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <returns>An instance of <see cref="SqlParameter" /> class.</returns>
        public static SqlParameter CreateParameter(
           string parameterName,
           SqlDbType parameterType,
           int? parameterSize,
           ParameterDirection parameterDirection,
           object parameterValue)
        {
            SqlParameter sqlParameter = new SqlParameter(parameterName, parameterType);
            
            if (parameterSize.HasValue)
            {
                sqlParameter.Size = parameterSize.Value;
            }
            
            sqlParameter.Direction = parameterDirection;
            
            sqlParameter.Value = parameterValue ?? DBNull.Value;

            return sqlParameter;
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlParameter" /> class for an output parameter based on the given parameter name and type.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <returns>An instance of <see cref="SqlParameter" /> class.</returns>
        public static SqlParameter CreateOutputSqlParameter(string parameterName, SqlDbType parameterType)
        {
            SqlParameter sqlParameter = new SqlParameter(parameterName, parameterType);
            sqlParameter.Direction = ParameterDirection.Output;

            return sqlParameter;
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlParameter" /> class for an output parameter based on the given parameter name, type and size.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterSize">The maximum size, in bytes, of the data associated with this parameter.</param>
        /// <returns>An instance of <see cref="SqlParameter" /> class.</returns>
        public static SqlParameter CreateOutputSqlParameter(string parameterName, SqlDbType parameterType, int parameterSize)
        {
            SqlParameter sqlParameter = new SqlParameter(parameterName, parameterType);
            sqlParameter.Direction = ParameterDirection.Output;
            sqlParameter.Size = parameterSize;

            return sqlParameter;
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlParameter" /> class for a return parameter based on the given parameter name and type.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <returns>An instance of <see cref="SqlParameter" /> class.</returns>
        public static SqlParameter CreateReturnParameter(string parameterName, SqlDbType parameterType)
        {
            SqlParameter sqlParameter = new SqlParameter(parameterName, parameterType);
            sqlParameter.Direction = ParameterDirection.ReturnValue;

            return sqlParameter;
        }

        /// <summary>
        /// Casts <param name="databaseValue" /> to a value of type T. 
        /// If the database value is null then returns the value <c>default(T)</c>
        /// </summary>
        /// <typeparam name="T">The type to which the database value will be cast to.</typeparam>
        /// <param name="databaseValue">A value obtained from a SQL command.</param>
        /// <returns>
        /// The database value casted to the type T. Null will be converted to <c>default(T)</c>.
        /// </returns>
        public static T ConvertDbValue<T>(object databaseValue)
        {
            return ConvertDbValue(databaseValue, default(T));
        }

        /// <summary>
        /// Casts <param name="databaseValue" /> to a value of type T. 
        /// If the database value is null then returns the value <param name="defaultValue"></param>
        /// </summary>
        /// <typeparam name="T">The type to which the database value will be cast to.</typeparam>
        /// <param name="databaseValue">A value obtained from a SQL command.</param>
        /// <param name="defaultValue">The value returned in case the database value is null.</param>
        /// <returns>
        /// The database value casted to the type T. Null will be converted to <param name="defaultValue" />
        /// </returns>
        public static T ConvertDbValue<T>(object databaseValue, T defaultValue)
        {
            if (databaseValue == DBNull.Value)
            {
                return defaultValue;
            }

            return (T)databaseValue;
        }

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginTransaction", Justification = "BeginTransaction is referencing the name a method")]
        public void BeginTransaction()
        {
            if (this.sqlTransaction != null)
            {
                throw new InvalidOperationException("Nested BeginTransaction is not supported.");
            }

            this.sqlTransaction = this.sqlConnection.BeginTransaction();
        }

        /// <summary>
        /// Commits the database transaction that was previously opened.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CommitTransaction", Justification = "CommitTransaction is referencing the name a method")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginTransaction", Justification = "CommitTransaction is referencing the name a method")]
        public void CommitTransaction()
        {
            if (this.sqlTransaction == null)
            {
                throw new InvalidOperationException("CommitTransaction called without a corresponding BeginTransaction");
            }

            this.sqlTransaction.Commit();
            this.sqlTransaction = null;
        }

        /// <summary>
        /// Rolls back the database transaction that was previously opened.
        /// Any exceptions related to SQL errors thrown during the rollback will be propagated to the caller.
        /// </summary>
        public void RollbackTransaction()
        {
            this.RollbackTransaction(false);
        }

        /// <summary>
        /// Rolls back the database transaction that was previously opened.
        /// </summary>
        /// <param name="suppressSqlErrors">
        /// True  - Exceptions related to SQL errors will be ignored and not be propagated to the caller.
        /// False - All exceptions will be propagated to the caller. 
        /// </param>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "RollbackTransaction", Justification = "RollbackTransaction is referencing the name a method")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginTransaction", Justification = "RollbackTransaction is referencing the name of a method")]
        public void RollbackTransaction(bool suppressSqlErrors)
        {
            if (this.sqlTransaction == null)
            {
                throw new InvalidOperationException("RollbackTransaction called without a corresponding BeginTransaction");
            }

            try
            {
                this.sqlTransaction.Rollback();
            }
            catch (SqlException)
            {
                if (suppressSqlErrors)
                {
                    return;
                }

                throw;
            }
            finally
            {
                this.sqlTransaction = null;
            }
        }

        /// <summary>
        /// Adds or refreshes rows in the System.Data.DataSet based on the given SQL command.
        /// </summary>
        /// <param name="dataSet">
        /// A <see cref="System.Data.DataSet" /> to fill with records.
        /// </param>
        /// <param name="sqlCommandText">
        /// The SQL command used to retrieve results from the database.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        public void FillDataSetFromStatement(DataSet dataSet, string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            if (sqlCommandText == null)
            {
                throw new ArgumentNullException("sqlCommandText");
            }

            using (SqlCommand sqlCommand = this.CreateStatementCommand(sqlCommandText, sqlParameters))
            {
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        if (i == 0)
                        {
                            sqlDataAdapter.TableMappings.Add("Table", dataSet.Tables[i].TableName);
                        }
                        else
                        {
                            sqlDataAdapter.TableMappings.Add(string.Format(CultureInfo.InvariantCulture, "Table{0}", i), dataSet.Tables[i].TableName);
                        }
                    }

                    sqlDataAdapter.Fill(dataSet);
                }
            }
        }

        /// <summary>
        /// Adds or refreshes rows in the System.Data.DataSet based on the given SQL stored procedure and parameters.
        /// </summary>
        /// <param name="dataSet">
        /// A <see cref="System.Data.DataSet" /> to fill with records.
        /// </param>
        /// <param name="storedProcedureName">
        /// The name of a SQL stored procedure that will be invoked.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the stored procedure.
        /// </param>
        public void FillDataSetFromStoredProcedure(
           DataSet dataSet,
           string storedProcedureName,
           params SqlParameter[] sqlParameters)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            if (storedProcedureName == null)
            {
                throw new ArgumentNullException("storedProcedureName");
            }

            using (SqlCommand sqlCommand = this.CreateStoredProcedureCommand(storedProcedureName, sqlParameters))
            {
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        if (i == 0)
                        {
                            sqlDataAdapter.TableMappings.Add("Table", dataSet.Tables[i].TableName);
                        }
                        else
                        {
                            sqlDataAdapter.TableMappings.Add(string.Format(CultureInfo.InvariantCulture, "Table{0}", i), dataSet.Tables[i].TableName);
                        }
                    }

                    sqlDataAdapter.Fill(dataSet);
                }
            }
        }

        /// <summary>
        /// Executes a T-SQL statement and returns the number of rows affected.
        /// </summary>
        /// <param name="sqlCommandText">
        /// The text of the SQL command.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        /// <returns>
        /// The number of rows affected.
        /// </returns>
        public int ExecuteStatementNoResult(string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStatementCommand(sqlCommandText, sqlParameters);
            return sqlCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes asynchronously a T-SQL statement and returns the number of rows affected.
        /// </summary>
        /// <param name="sqlCommandText">
        /// The text of the SQL command.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The task's result is the number of rows affected.
        /// </returns>
        public async Task<int> ExecuteStatementNoResultAsync(string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStatementCommand(sqlCommandText, sqlParameters);
            return await sqlCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Invokes a stored procedure and returns the number of rows affected.
        /// </summary>
        /// <param name="storedProcedureName">
        /// The name of the stored procedure that will be invoked.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        /// <returns>
        /// The number of rows affected.
        /// </returns>
        public int ExecuteStoredProcedureNoResult(string storedProcedureName, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStoredProcedureCommand(storedProcedureName, sqlParameters);
            return sqlCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Invokes a scalar function and returns the result.
        /// </summary>
        /// <param name="sqlCommandText">
        /// The text of the SQL command.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        /// <returns>
        /// The scalar result of the SQL command execution.
        /// </returns>
        public object ExecuteScalar(string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStatementCommand(sqlCommandText, sqlParameters);
            return sqlCommand.ExecuteScalar();
        }

        /// <summary>
        /// Returns a <see cref="System.Data.SqlClient.SqlDataReader" /> instance obtained by invoking 
        /// the stored procedure or function against the connection associated with this instance of 
        /// <see cref="AdoNetLayer"/> 
        /// </summary>
        /// <param name="sqlCommandText">
        /// The text of the SQL command.
        /// Security Note: To avoid security vulnerabilities you should ensure that this parameter 
        ///                does not contain sections provided by the user.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        /// <returns>
        /// A <see cref="System.Data.SqlClient.SqlDataReader" /> object.
        /// </returns>
        public SqlDataReader ExecuteStatementReader(string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStatementCommand(sqlCommandText, sqlParameters);
            return sqlCommand.ExecuteReader();
        }

        /// <summary>
        /// Returns a <see cref="System.Data.SqlClient.SqlDataReader" /> instance obtained by invoking 
        /// the stored procedure or function against the connection associated with this instance of 
        /// <see cref="AdoNetLayer"/> 
        /// </summary>
        /// <param name="storedProcedureOrFunctionName">
        /// The name of the stored procedure or function that will be invoked.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL command.
        /// </param>
        /// <returns>
        /// A <see cref="System.Data.SqlClient.SqlDataReader" /> object.
        /// </returns>
        public SqlDataReader ExecuteStoredProcedureReader(string storedProcedureOrFunctionName, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStoredProcedureCommand(storedProcedureOrFunctionName, sqlParameters);
            return sqlCommand.ExecuteReader();
        }

        /// <summary>
        /// Invokes a scalar function and returns the result.
        /// </summary>
        /// <param name="functionName">
        /// The name of the function that will be invoked.
        /// </param>
        /// <param name="returnType">
        /// Specifies the return type.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL function.
        /// </param>
        /// <returns>
        /// The value returned by the function.
        /// </returns>
        public object InvokeScalarFunction(
           string functionName,
           SqlDbType returnType,
           params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = this.CreateStoredProcedureCommand(functionName, sqlParameters);
            SqlParameter returnParam = CreateReturnParameter("@ReturnValue", returnType);
            sqlCommand.Parameters.Add((SqlParameter)returnParam);

            sqlCommand.ExecuteNonQuery();
            return returnParam.Value;
        }

        /// <summary>
        /// Transfer the data from a in-memory DataTable into a database table using <see cref="SqlBulkCopy" />.
        /// </summary>
        /// <param name="destinationTableName">
        /// The name of the destination database table.
        /// </param>
        /// <param name="dataTable">
        /// Contains the data that must be transferred in the database table.
        /// </param>
        public void BulkCopyTable(string destinationTableName, DataTable dataTable, int bulkCopyTimeout)
        {
            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(this.sqlConnection, SqlBulkCopyOptions.KeepIdentity, null))
            {
                sqlBulkCopy.BulkCopyTimeout = bulkCopyTimeout;
                sqlBulkCopy.DestinationTableName = destinationTableName;
                sqlBulkCopy.WriteToServer(dataTable);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlCommand"/> class based on the given SQL statement and parameters.
        /// </summary>
        /// <param name="sqlCommandText">
        /// A SQL statement.
        /// Security Note: To avoid security vulnerabilities you should ensure that this parameter 
        ///                does not contain sections provided by the user.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL function.
        /// </param>
        /// <returns>
        /// An instance of <see cref="SqlCommand"/> class .
        /// </returns>
        private SqlCommand CreateStatementCommand(string sqlCommandText, params SqlParameter[] sqlParameters)
        {
            return this.CreateCommand(sqlCommandText, CommandType.Text, sqlParameters);
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlCommand"/> class based on the given stored procedure or function name and parameters.
        /// </summary>
        /// <param name="storedProcedureOrFunctionName">
        /// The name of the stored procedure or function that will be invoked.
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL function.
        /// </param>
        /// <returns>
        /// An instance of <see cref="SqlCommand"/> class .
        /// </returns>
        private SqlCommand CreateStoredProcedureCommand(string storedProcedureOrFunctionName, params SqlParameter[] sqlParameters)
        {
            return this.CreateCommand(storedProcedureOrFunctionName, CommandType.StoredProcedure, sqlParameters);
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlCommand"/> class based on the given SQL statement, command type and parameters.
        /// </summary>
        /// <param name="sqlStatementText">
        /// A SQL statement. Can be the text of a query, the name of a stored procedure or a function. 
        /// The type of this text must be correlated with the <c>commadType</c> parameter.
        /// </param>
        /// <param name="commandType">
        /// Specifies the type of the command. 
        /// </param>
        /// <param name="sqlParameters">
        /// An array of SQL parameters that will be used when invoking the SQL function.
        /// </param>
        /// <returns>
        /// An instance of <see cref="SqlCommand"/> class .
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The SQL statement is provided by the caller.")]
        private SqlCommand CreateCommand(string sqlStatementText, CommandType commandType, params SqlParameter[] sqlParameters)
        {
            SqlCommand sqlCommand = new SqlCommand(sqlStatementText, this.sqlConnection);
            try
            {
                sqlCommand.CommandTimeout = this.commandTimeout;
                sqlCommand.CommandType = commandType;

                if (this.sqlTransaction != null)
                {
                    sqlCommand.Transaction = this.sqlTransaction;
                }

                if (sqlParameters != null)
                {
                    for (int i = 0; i < sqlParameters.Length; i++)
                    {
                        sqlCommand.Parameters.Add((SqlParameter)sqlParameters[i]);
                    }
                }

                return sqlCommand;
            }
            catch (Exception)
            {
                sqlCommand.Dispose();
                throw;
            }
        }
    }
}
