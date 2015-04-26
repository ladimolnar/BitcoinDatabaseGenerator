//-----------------------------------------------------------------------
// <copyright file="DatabaseGeneratorParameters.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Globalization;
    using ZeroHelpers.ParameterParser;

    public class DatabaseGeneratorParameters : ParametersListInfo
    {
        private const string ParameterNameBlockchainPath = "BlockchainPath";
        private const string ParameterNameSqlServerName = "SqlServerName";
        private const string ParameterNameDatabaseName = "DatabaseName";
        private const string ParameterNameSqlUserName = "SqlUserName";
        private const string ParameterNameSqlPassword = "SqlPassword";
        private const string ParameterNameDropDb = "DropDb";
        private const string ParameterNameSkipDbManagement = "SkipDbManagement";
        private const string ParameterNameTypeDbSchema = "TypeDbSchema";
        private const string ParameterNameThreads = "Threads";
        private const string ParameterNameValidation = "Validation";

        private const int MinThreads = 1;
        private const int MaxThreads = 100;

        private int threads;

        public static ParametersListRules ParameterListRules
        {
            get
            {
                ParametersListRules parametersListRules = new ParametersListRules(false, true);

                parametersListRules.AddParameterRules(ParameterRules.CreateRequiredParameter(ParameterNameBlockchainPath, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlServerName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateRequiredParameter(ParameterNameDatabaseName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlUserName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlPassword, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameThreads, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameDropDb));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSkipDbManagement));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameTypeDbSchema));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameValidation, 1));

                return parametersListRules;
            }
        }

        public string BlockchainPath
        {
            get { return base[ParameterNameBlockchainPath].Argument; }
        }

        public string DatabaseName
        {
            get { return base[ParameterNameDatabaseName].Argument; }
        }

        public string SqlServerName
        {
            get
            {
                string sqlServerName;
                ParameterInfo parameterInfo;

                if (this.TryGetParameterInfo(ParameterNameSqlServerName, out parameterInfo))
                {
                    sqlServerName = parameterInfo.Argument;
                }
                else
                {
                    sqlServerName = "localhost";
                }

                return sqlServerName;
            }
        }

        public string SqlUserName
        {
            get
            {
                string sqlUserName = null;
                ParameterInfo parameterInfo;

                if (this.TryGetParameterInfo(ParameterNameSqlUserName, out parameterInfo))
                {
                    sqlUserName = parameterInfo.Argument;
                }

                return sqlUserName;
            }
        }

        public string SqlPassword
        {
            get
            {
                string sqlPassword = null;
                ParameterInfo parameterInfo;

                if (this.TryGetParameterInfo(ParameterNameSqlPassword, out parameterInfo))
                {
                    sqlPassword = parameterInfo.Argument;
                }

                return sqlPassword;
            }
        }

        public bool DropDb
        {
            get { return this.ParameterWasSpecified(ParameterNameDropDb); }
        }

        public bool SkipDbManagement
        {
            get { return this.ParameterWasSpecified(ParameterNameSkipDbManagement); }
        }

        public bool TypeDbSchema
        {
            get { return this.ParameterWasSpecified(ParameterNameTypeDbSchema); }
        }

        public int Threads
        {
            get { return this.threads; }
        }

        public bool Validation
        {
            get { return this.ParameterWasSpecified(ParameterNameValidation); }
        }

        public string ValidationDatabaseName
        {
            get { return base[ParameterNameValidation].Argument; }
        }

        public override void Validate()
        {
            this.ValidateThreadsParameter();
        }

        private void ValidateThreadsParameter()
        {
            ParameterInfo threadsParameterInfo;

            if (this.TryGetParameterInfo(ParameterNameThreads, out threadsParameterInfo))
            {
                if (threadsParameterInfo.Argument.ToUpperInvariant() == "AUTO")
                {
                    this.threads = Environment.ProcessorCount;
                }
                else
                {
                    if (int.TryParse(threadsParameterInfo.Argument, out this.threads))
                    {
                        if (this.threads < MinThreads || this.threads > MaxThreads)
                        {
                            throw new InvalidParameterException(string.Format(
                                CultureInfo.InvariantCulture,
                                "The value specified for parameter /{0} is out of its valid range [{1} - {2}].",
                                ParameterNameThreads,
                                MinThreads,
                                MaxThreads));
                        }
                    }
                    else
                    {
                        throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "The value specified for parameter /{0} is invalid.", ParameterNameThreads));
                    }
                }
            }
            else
            {
                this.threads = Environment.ProcessorCount;
            }
        }
    }
}
