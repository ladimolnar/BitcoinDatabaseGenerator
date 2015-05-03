//-----------------------------------------------------------------------
// <copyright file="Program.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using BitcoinBlockchain.Parser;
    using BitcoinDataLayerAdoNet;
    using ZeroHelpers.Exceptions;
    using ZeroHelpers.ParameterParser;

    public static class Program
    {
        /// <summary>
        /// Represents the main entry point in the command line tool.
        /// </summary>
        /// <param name="args">
        /// Contains the command line arguments.
        /// For a complete description of the command line arguments see method TypeHelpPage.
        /// </param>
        /// <returns>
        /// 0 - Success.
        /// 1 - Error.
        /// </returns>
        private static int Main(string[] args)
        {
            int result = 1; // Assume error.

            try
            {
                ParameterParser<DatabaseGeneratorParameters> parameterParser = new ParameterParser<DatabaseGeneratorParameters>(DatabaseGeneratorParameters.ParameterListRules);
                DatabaseGeneratorParameters parameters = parameterParser.ParseParameters(args);

                if (parameters.IsHelpSpecified)
                {
                    TypeHelpPage();
                    result = 0; // Success.
                }
                else if (parameters.IsInfoSpecified)
                {
                    TypeInfoPage();
                    result = 0; // Success.
                }
                else
                {
                    Console.WriteLine(GetApplicationNameAndVersion());

                    if (parameters.IsTypeDbSchemaSpecified)
                    {
                        TypeDbSchema();
                        result = 0; // Success.
                    }
                    else if (parameters.IsRunValidationSpecified)
                    {
                        AutoValidator autoValidator = new AutoValidator(
                            parameters.SqlServerName,
                            parameters.SqlDbName,
                            parameters.SqlUserName,
                            parameters.SqlPassword);

                        result = autoValidator.Validate() ? 0 : 1;
                    }
                    else
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Active threads: {0}", parameters.Threads));
                        Console.WriteLine();

                        DatabaseConnection databaseConnection = DatabaseConnection.CreateSqlServerConnection(parameters.SqlServerName, parameters.SqlDbName, parameters.SqlUserName, parameters.SqlPassword);
                        DatabaseGenerator databaseGenerator = new DatabaseGenerator(parameters, databaseConnection);
                        databaseGenerator.GenerateAndPopulateDatabase().Wait();

                        result = 0; // Success.
                    }
                }
            }
            catch (InvalidParameterException ex)
            {
                Console.Error.WriteLine("Invalid command line: {0}", ex.Message);
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine();
                HandleException(ex);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("AN ERROR OCCURRED:{0}{1}", Environment.NewLine, ex);
            }

            Console.WriteLine();

            //// TypePeakMemoryUsage();

            return result;
        }

        private static void HandleException(Exception ex)
        {
            if (ex.InnerException is AggregateException)
            {
                HandleException(ex.InnerException);
            }
            else if (ex.InnerException is InternalErrorException)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("INTERNAL ERROR: {0}", ex.InnerException.Message);
            }
            else if (ex.InnerException is InvalidBlockchainFilesException)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("ERROR: {0}", ex.InnerException.Message);
            }
            else if (ex.InnerException is InvalidBlockchainContentException)
            {
                Console.Error.WriteLine("{0}{0}", Environment.NewLine);
                Console.Error.WriteLine("ERROR: {0}", ex.InnerException.Message);
                Console.Error.WriteLine("It appears that the blockchain imported in the database is invalid.\nPlease make sure that files imported are valid and then run the \nBitcoinDatabaseGenerator.exe again specifying the /DropDb\nparameter in order to rebuild the entire database.");
            }
            else if (ex.InnerException is UnknownBlockVersionException)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("ERROR: The blockchain contains blocks with an unknown version. {0}", ex.InnerException.Message);
            }
            else
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("AN ERROR OCCURRED:{0}{1}", Environment.NewLine, ex);
            }
        }

        ////private static void TypePeakMemoryUsage()
        ////{
        ////    using (Process process = Process.GetCurrentProcess())
        ////    {
        ////        Console.WriteLine();
        ////        Console.WriteLine("Peak memory usage: {0:n0} bytes.", process.PeakWorkingSet64);
        ////    }
        ////}

        private static void TypeDbSchema()
        {
            // @@@ need to get rid of this. Another reason to use ALTER INDEX [*INDEX_NAME*] ON *TABLE_NAME* DISABLE
            Console.WriteLine("Consider creating the database without any indexes. Add the indexes only after the database is populated with its initial data.");
            Console.Write("Hit any key");
            Console.ReadKey();

            Console.WriteLine();
            Console.WriteLine("Database schema:");
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine(DatabaseManager.GetDatabaseSchema());
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Various strings in the help message need to be ignored by the spell checker.")]
        private static void TypeHelpPage()
        {
            Console.WriteLine();
            Console.WriteLine(GetApplicationNameAndVersion());

            Console.Write(
@"
Transfers data from Bitcoin blockchain files into a SQL Server database.

Usage:  {0} 
        [/?] | [/info] | [<transfer-options>] |
        [/TypeDbSchema] | [<auto-validation-options>]

<transfer-options>: 
        /BlockchainPath path 
        [/SqlServerName sql_server_name] /SqlDbName db_name
        [/SqlUserName user_name /SqlPassword pwd]
        [/Threads number_of_threads]
        [/DropDb] [/SkipDbCreate]

<auto-validation-options>:
        /RunValidation   
        [/SqlServerName sql_server_name] /SqlDbName db_name
        [/SqlUserName user_name /SqlPassword pwd]

/?               Displays this help page.
/info            Displays general information about the transfer process
                 and provides links to documentation and sources.
/BlockchainPath  Specifies the path to the folder where the blockchain files
                 are stored.
/SqlServerName   Specifies the name of the SQL Server.
                 Default value: localhost
/SqlDbName       Specifies the name of a SQL Server database.
/SqlUserName     Specifies the SQL server user name.
/SqlPassword     Specifies the SQl server user password.
                 When the SQl server user name and password are not specified,
                 Windows Authentication is used.
/Threads         The number of background threads.
                 If not specified, the number of logical processors on your
                 system is assumed.
                 The valid range is [1-100].
/DropDb          When specified the database will be dropped and recreated 
                 before the blockchain transfer is executed.
/SkipDbCreate    When specified the database will not be created 
                 automatically. Useful if the database is hosted on a system
                 that does not allow programmatic access to DB create 
                 commands. In a case like that you will need to create the 
                 database manually. You may want to consider using 
                 /TypeDbSchema to obtain the database schema.
                 /SkipDbCreate and /DropDb cannot be specified together.
/TypeDbSchema    When specified the database schema will be displayed. 
                 You may want to use the command line redirect syntax in 
                 order to redirect the output to a file:
                 BitcoinDatabaseGenerator /TypeDbSchema > schema.txt
                 or the pipe syntax to copy the output to the clipboard:
                 BitcoinDatabaseGenerator /TypeDbSchema | clip
/RunValidation   Runs in auto-validation mode.
                 Reserved for development.
                 If you are a developer and make changes to the sources, 
                 in addition to the available test automation, you can run
                 the application in auto-validation mode. 
                 The application will run certain queries over an existing 
                 database, save the results to temporary data files and 
                 compare their content with baselines. A large category of 
                 bugs introduced during development can be caught this way.
                 This test is based on the fact that data once in the 
                 blockchain should never change. The baseline data may be 
                 updated for future versions as the blockchain grows.",
                GetApplicationName());

            Console.WriteLine();
        }

        private static void TypeInfoPage()
        {
            Console.WriteLine();
            Console.WriteLine(GetApplicationNameAndVersion());

            Console.Write(
@"
Transfers data from Bitcoin blockchain files into a SQL Server database.

~ PLACEHOLDER ~
            ");

            Console.WriteLine();
        }

        private static string GetApplicationName()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        }

        private static string GetApplicationNameAndVersion()
        {
            AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();

#if DEBUG
            const string configuration = " [DEBUG]";
#else
            const string configuration = null;
#endif

            return string.Format(CultureInfo.InvariantCulture, "{0} {1}.{2}{3}", assemblyName.Name, assemblyName.Version.Major, assemblyName.Version.Minor, configuration);
        }
    }
}
