//-----------------------------------------------------------------------
// <copyright file="IDatabaseGeneratorParameters.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;

    public interface IDatabaseGeneratorParameters
    {
        string SqlServerName { get; }

        string SqlDbName { get; }

        string SqlUserName { get; }

        string SqlPassword { get; }

        bool IsDropDbSpecified { get; }

        int Threads { get; }

        string BlockchainPath { get; }

        UInt32? BlockId { get; }
    }
}
