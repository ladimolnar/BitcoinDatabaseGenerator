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

    public class DatabaseGeneratorParameters : ParametersListInfo, IDatabaseGeneratorParameters
    {
        public const string ParameterNameBlockchainPath = "BlockchainPath";
        public const string ParameterNameSqlServerName = "SqlServerName";
        public const string ParameterNameSqlDbName = "SqlDbName";
        public const string ParameterNameSqlUserName = "SqlUserName";
        public const string ParameterNameSqlPassword = "SqlPassword";
        public const string ParameterNameDropDb = "DropDb";
        public const string ParameterNameSkipDbCreate = "SkipDbCreate";
        public const string ParameterNameThreads = "Threads";
        public const string ParameterNameTypeDbSchema = "TypeDbSchema";
        public const string ParameterNameRunValidation = "RunValidation";
        public const string ParameterNameInfo= "Info";

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
                parametersListRules.AddParameterRules(ParameterRules.CreateRequiredParameter(ParameterNameSqlDbName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlUserName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlPassword, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameDropDb));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSkipDbCreate));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameThreads, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameTypeDbSchema));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameRunValidation));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameInfo));

                return parametersListRules;
            }
        }

        public string BlockchainPath
        {
            get { return base[ParameterNameBlockchainPath].Argument; }
        }

        public string SqlDbName
        {
            get { return base[ParameterNameSqlDbName].Argument; }
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

        public bool SkipDbCreate
        {
            get { return this.ParameterWasSpecified(ParameterNameSkipDbCreate); }
        }

        public bool TypeDbSchema
        {
            get { return this.ParameterWasSpecified(ParameterNameTypeDbSchema); }
        }

        public int Threads
        {
            get { return this.threads; }
        }

        public bool RunValidation
        {
            get { return this.ParameterWasSpecified(ParameterNameRunValidation); }
        }

        public bool Info
        {
            get { return this.ParameterWasSpecified(ParameterNameInfo); }
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
