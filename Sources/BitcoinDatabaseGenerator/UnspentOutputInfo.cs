//-----------------------------------------------------------------------
// <copyright file="UnspentOutputInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;

    /// <summary>
    /// Contains information about an unspent transaction output. 
    /// </summary>
    public class UnspentOutputInfo : IEquatable<UnspentOutputInfo>
    {
        public UnspentOutputInfo(long transactionOutputId, int outputIndex)
        {
            this.TransactionOutputId = transactionOutputId;
            this.OutputIndex = outputIndex;
        }

        public long TransactionOutputId { get; private set; }

        public int OutputIndex { get; private set; }

        public bool Equals(UnspentOutputInfo other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return this.TransactionOutputId == other.TransactionOutputId && this.OutputIndex == other.OutputIndex;
        }
    }
}
