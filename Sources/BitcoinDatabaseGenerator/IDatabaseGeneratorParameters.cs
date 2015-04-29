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

        string DatabaseName { get; }

        string SqlUserName { get; }

        string SqlPassword { get; }

        bool SkipDbManagement { get; }

        bool DropDb { get; }

        int Threads { get; }

        string BlockchainPath { get; }
    }
}
