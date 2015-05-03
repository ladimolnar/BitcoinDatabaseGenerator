//-----------------------------------------------------------------------
// <copyright file="DatabaseGeneratorParameters.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using ZeroHelpers.ParameterParser;

    public class DatabaseGeneratorParameters : ParametersListInfo, IDatabaseGeneratorParameters
    {
        public const string ParameterNameInfo = "Info";
        public const string ParameterNameBlockchainPath = "BlockchainPath";
        public const string ParameterNameSqlServerName = "SqlServerName";
        public const string ParameterNameSqlDbName = "SqlDbName";
        public const string ParameterNameSqlUserName = "SqlUserName";
        public const string ParameterNameSqlPassword = "SqlPassword";
        public const string ParameterNameThreads = "Threads";
        public const string ParameterNameDropDb = "DropDb";
        public const string ParameterNameSkipDbCreate = "SkipDbCreate";
        public const string ParameterNameTypeDbSchema = "TypeDbSchema";
        public const string ParameterNameRunValidation = "RunValidation";

        private const int MinThreads = 1;
        private const int MaxThreads = 100;

        private int threads;

        public static ParametersListRules ParameterListRules
        {
            get
            {
                ParametersListRules parametersListRules = new ParametersListRules(false, true);

                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameInfo));
                parametersListRules.AddParameterRules(ParameterRules.CreateRequiredParameter(ParameterNameBlockchainPath, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlServerName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateRequiredParameter(ParameterNameSqlDbName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlUserName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSqlPassword, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameThreads, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameDropDb));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameSkipDbCreate));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameTypeDbSchema));
                parametersListRules.AddParameterRules(ParameterRules.CreateOptionalParameter(ParameterNameRunValidation));

                return parametersListRules;
            }
        }

        public string BlockchainPath
        {
            get { return base[ParameterNameBlockchainPath].Argument; }
        }

        public bool IsBlockchainPathSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameBlockchainPath); }
        }

        public string SqlDbName
        {
            get { return base[ParameterNameSqlDbName].Argument; }
        }

        public bool IsSqlDbNameSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameSqlDbName); }
        }

        public bool IsSqlServerNameSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameSqlServerName); }
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

        public bool IsSqlUserNameSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameSqlUserName); }
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

        public bool IsSqlPasswordSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameSqlPassword); }
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

        public bool IsDropDbSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameDropDb); }
        }

        public bool IsSkipDbCreateSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameSkipDbCreate); }
        }

        public bool IsTypeDbSchemaSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameTypeDbSchema); }
        }

        public bool IsThreadsSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameThreads); }
        }

        public int Threads
        {
            get { return this.threads; }
        }

        public bool IsRunValidationSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameRunValidation); }
        }

        public bool IsInfoSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameInfo); }
        }

        public override void Validate()
        {
            if (this.Parameters.Count == 0)
            {
                throw new InvalidParameterException("Not enough parameters are specified.");
            }

            this.ValidateInfoParameter();
            this.ValidateBlockchainPathParameter();
            this.ValidateSqlServerNameParameter();
            this.ValidateSqlDbNameParameter();
            this.ValidateSqlUserNameParameter();
            this.ValidateSqlPasswordParameter();
            this.ValidateThreadsParameter();
            this.ValidateDropDbParameter();
            this.ValidateSkipDbCreateParameter();
            this.ValidateTypeDbSchemaParameter();
            this.ValidateRunValidationParameter();
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateInfoParameter()
        {
            if (this.IsInfoSpecified)
            {
                if (this.IsBlockchainPathSpecified || this.IsTypeDbSchemaSpecified || this.IsRunValidationSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified if either of /{1}, /{2} or /{3} is specified.",
                        ParameterNameInfo,
                        ParameterNameBlockchainPath,
                        ParameterNameTypeDbSchema,
                        ParameterNameRunValidation));
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Kept instance method for consistency.")]
        private void ValidateBlockchainPathParameter()
        {
            // Nothing to do here. This is the "main" parameter. 
            // Any validation written here would be redundant with other existing validations.
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateSqlServerNameParameter()
        {
            if (this.IsSqlServerNameSpecified)
            {
                if (this.IsBlockchainPathSpecified == false && this.IsRunValidationSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of either /{1} or /{2}.",
                        ParameterNameSqlServerName,
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateSqlDbNameParameter()
        {
            if (this.IsSqlDbNameSpecified)
            {
                if (this.IsBlockchainPathSpecified == false && this.IsRunValidationSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of either /{1} or /{2}.",
                        ParameterNameSqlDbName,
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }
            }
            else
            {
                if (this.IsBlockchainPathSpecified || this.IsRunValidationSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} is required when parameter /{1} or /{2} is specified.",
                        ParameterNameSqlDbName,
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateSqlUserNameParameter()
        {
            if (this.IsSqlUserNameSpecified)
            {
                if (this.IsBlockchainPathSpecified == false && this.IsRunValidationSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of either /{1} or /{2}.",
                        ParameterNameSqlUserName,
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }
            }
            else
            {
                if (this.IsSqlPasswordSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameters /{0} and /{1} must be specified together.",
                        ParameterNameSqlUserName,
                        ParameterNameSqlPassword));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateSqlPasswordParameter()
        {
            if (this.IsSqlPasswordSpecified)
            {
                if (this.IsBlockchainPathSpecified == false && this.IsRunValidationSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of either /{1} or /{2}.",
                        ParameterNameSqlPassword,
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }

                if (this.IsSqlUserNameSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameters /{0} and /{1} must be specified together.",
                        ParameterNameSqlUserName,
                        ParameterNameSqlPassword));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateThreadsParameter()
        {
            if (this.IsThreadsSpecified)
            {
                if (this.IsBlockchainPathSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of /{1}.",
                        ParameterNameThreads,
                        ParameterNameBlockchainPath));
                }

                ParameterInfo threadsParameterInfo = this.GetParameterInfo(ParameterNameThreads);

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
            else
            {
                this.threads = Environment.ProcessorCount;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateDropDbParameter()
        {
            if (this.IsDropDbSpecified)
            {
                if (this.IsBlockchainPathSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of /{1}.",
                        ParameterNameDropDb,
                        ParameterNameBlockchainPath));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateSkipDbCreateParameter()
        {
            if (this.IsSkipDbCreateSpecified)
            {
                if (this.IsBlockchainPathSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of /{1}.",
                        ParameterNameSkipDbCreate,
                        ParameterNameBlockchainPath));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateTypeDbSchemaParameter()
        {
            if (this.IsTypeDbSchemaSpecified)
            {
                if (this.IsBlockchainPathSpecified || this.IsRunValidationSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified if either of /{1} or /{2} is specified.",
                        ParameterNameTypeDbSchema,
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateRunValidationParameter()
        {
            if (this.IsRunValidationSpecified)
            {
                if (this.IsBlockchainPathSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameters /{0} and /{1} cannot be specified together.",
                        ParameterNameBlockchainPath,
                        ParameterNameRunValidation));
                }
            }
        }
    }
}
