//-----------------------------------------------------------------------
// <copyright file="IDatabaseGeneratorParameters.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    public interface IDatabaseGeneratorParameters
    {
        string SqlServerName { get; }

        string SqlDbName { get; }

        string SqlUserName { get; }

        string SqlPassword { get; }

        bool IsSkipDbCreateSpecified { get; }

        bool IsDropDbSpecified { get; }

        int Threads { get; }

        string BlockchainPath { get; }
    }
}
