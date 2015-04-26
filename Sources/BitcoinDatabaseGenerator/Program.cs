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

                if (parameters.HelpRequested)
                {
                    TypeHelpPage();
                    result = 0; // Success.
                }
                else
                {
                    Console.WriteLine(GetApplicationNameAndVersion());

                    if (parameters.TypeDbSchema)
                    {
                        TypeDbSchema();
                        result = 0; // Success.
                    }
                    else if (parameters.Validation)
                    {
                        AutoValidator autoValidator = new AutoValidator(parameters.ValidationDatabaseName);
                        bool validationResult = autoValidator.Validate().Result;
                        result = validationResult ? 0 : 1;
                    }
                    else
                    {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Active threads: {0}", parameters.Threads));
                        Console.WriteLine();

                        DatabaseGenerator databaseGenerator = new DatabaseGenerator(parameters);
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
                Console.Error.WriteLine("AN ERROR OCCURRED:{0}{1}", Environment.NewLine, ex.ToString());
            }

            Console.WriteLine();

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
                Console.Error.WriteLine("AN ERROR OCCURRED:{0}{1}", Environment.NewLine, ex.ToString());
            }
        }

        private static void TypeDbSchema()
        {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BitcoinDatabaseGenerator", Justification = "BitcoinDatabaseGenerator is the name of this application")]
        private static void TypeHelpPage()
        {
            Console.WriteLine(GetApplicationNameAndVersion());
            Console.WriteLine();
        }

        private static string GetApplicationNameAndVersion()
        {
            AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}.{2}", assemblyName.Name, assemblyName.Version.Major, assemblyName.Version.Minor);
        }
    }
}
