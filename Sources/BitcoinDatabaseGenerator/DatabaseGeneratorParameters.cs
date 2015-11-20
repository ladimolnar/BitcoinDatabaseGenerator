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
        public const string ParameterNameBlockchainPath = "BlockchainPath";
        public const string ParameterNameSqlServerName = "SqlServerName";
        public const string ParameterNameSqlDbName = "SqlDbName";
        public const string ParameterNameSqlUserName = "SqlUserName";
        public const string ParameterNameSqlPassword = "SqlPassword";
        public const string ParameterNameThreads = "Threads";
        public const string ParameterNameDropDb = "DropDb";
        public const string ParameterNameShowDbSchema = "ShowDbSchema";
        public const string ParameterNameRunValidation = "RunValidation";
        public const string ParameterNameBlockId = "BlockId";

        private const int MinThreads = 1;
        private const int MaxThreads = 100;

        public static ParametersListRules ParameterListRules
        {
            get
            {
                ParametersListRules parametersListRules = new ParametersListRules(false, true);

                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameBlockchainPath, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameSqlServerName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameSqlDbName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameSqlUserName, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameSqlPassword, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameThreads, 1));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameDropDb));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameShowDbSchema));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameRunValidation));
                parametersListRules.AddParameterRules(ParameterRules.CreateParameter(ParameterNameBlockId, 1));

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

        public bool IsShowDbSchemaSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameShowDbSchema); }
        }

        public bool IsThreadsSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameThreads); }
        }

        public int Threads { get; private set; }

        public bool IsRunValidationSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameRunValidation); }
        }

        public bool IsBlockIdSpecified
        {
            get { return this.ParameterWasSpecified(ParameterNameBlockId); }
        }

        public UInt32? BlockId { get; private set; }

        public override void Validate()
        {
            if (this.Parameters.Count == 0)
            {
                throw new InvalidParameterException("Not enough parameters are specified. Use \"/?\" for help.");
            }

            this.ValidateBlockchainPathParameter();
            this.ValidateSqlServerNameParameter();
            this.ValidateSqlDbNameParameter();
            this.ValidateSqlUserNameParameter();
            this.ValidateSqlPasswordParameter();
            this.ValidateThreadsParameter();
            this.ValidateDropDbParameter();
            this.ValidateShowDbSchemaParameter();
            this.ValidateRunValidationParameter();
            this.ValidateBlockIdParameter();
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
            }
            else
            {
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

                int threads;
                if (int.TryParse(threadsParameterInfo.Argument, out threads))
                {
                    if (threads < MinThreads || threads > MaxThreads)
                    {
                        throw new InvalidParameterException(string.Format(
                            CultureInfo.InvariantCulture,
                            "The value specified for parameter /{0} is out of its valid range [{1} - {2}].",
                            ParameterNameThreads,
                            MinThreads,
                            MaxThreads));
                    }

                    this.Threads = threads;
                }
                else
                {
                    throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "The value specified for parameter /{0} is invalid.", ParameterNameThreads));
                }
            }
            else
            {
                this.Threads = Environment.ProcessorCount;
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
        private void ValidateShowDbSchemaParameter()
        {
            if (this.IsShowDbSchemaSpecified)
            {
                if (this.IsBlockchainPathSpecified || this.IsRunValidationSpecified)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified if either of /{1} or /{2} is specified.",
                        ParameterNameShowDbSchema,
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

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Format strings are analyzed after inlining the names of the command line parameters. Those are valid strings.")]
        private void ValidateBlockIdParameter()
        {
            if (this.IsBlockIdSpecified)
            {
                if (this.IsBlockchainPathSpecified == false)
                {
                    throw new InvalidParameterException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Parameter /{0} cannot be specified in the absence of /{1}.",
                        ParameterNameBlockId,
                        ParameterNameBlockchainPath));
                }

                ParameterInfo blockIdParameterInfo = this.GetParameterInfo(ParameterNameBlockId);

                UInt32 blockIdValue;
                string numericArgument = blockIdParameterInfo.Argument;

                if (UInt32.TryParse(numericArgument, out blockIdValue))
                {
                    this.BlockId = blockIdValue;
                }
                else
                {
                    if (numericArgument.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        numericArgument = numericArgument.Substring(2);
                        if (UInt32.TryParse(numericArgument, NumberStyles.HexNumber, null, out blockIdValue))
                        {
                            this.BlockId = blockIdValue;
                        }
                    }
                }

                if (this.BlockId == null)
                {
                    throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "The value specified for parameter /{0} is invalid.", ParameterNameBlockId));
                }
            }
        }
    }
}
