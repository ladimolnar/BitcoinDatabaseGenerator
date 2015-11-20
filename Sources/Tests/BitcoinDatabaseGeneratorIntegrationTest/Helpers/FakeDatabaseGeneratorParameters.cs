//-----------------------------------------------------------------------
// <copyright file="FakeDatabaseGeneratorParameters.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Helpers
{
    using System;
    using BitcoinDatabaseGenerator;

    public class FakeDatabaseGeneratorParameters : IDatabaseGeneratorParameters
    {
        public const int AutoThreads = -1;

        public FakeDatabaseGeneratorParameters(bool isDropDbSpecified, int threads)
        {
            this.IsDropDbSpecified = isDropDbSpecified;

            this.Threads = threads;
            if (this.Threads == AutoThreads)
            {
                this.Threads = Environment.ProcessorCount;
            }
        }

        public string SqlServerName
        {
            get { return "(localDb)"; }
        }

        public string SqlDbName
        {
            get { return "TestAutomationBitcoinDatabase"; }
        }

        public string SqlUserName
        {
            get { return null; }
        }

        public string SqlPassword
        {
            get { return null; }
        }

        public bool IsDropDbSpecified { get; private set; }

        public int Threads { get; private set; }

        public string BlockchainPath
        {
            get { throw new NotSupportedException(); }
        }

        public uint? BlockId
        {
            get { return null; }
        }
    }
}
