//-----------------------------------------------------------------------
// <copyright file="UnspentTransactionInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains information about an unspent transaction. 
    /// </summary>
    public class UnspentTransactionInfo : IEquatable<UnspentTransactionInfo>
    {
        // TODO: Maybe a sorted list or a dictionary would make the lookup faster? 

        /// <summary>
        /// A list with the indexes of all the outputs that are unspent. 
        /// </summary>
        private readonly List<UnspentOutputInfo> unspentOutputInfoList;

        public UnspentTransactionInfo(long bitcoinTransactionId, ByteArray transactionHash, List<UnspentOutputInfo> unspentOutputInfoList)
        {
            this.BitcoinTransactionId = bitcoinTransactionId;
            this.TransactionHash = transactionHash;
            this.unspentOutputInfoList = unspentOutputInfoList;
        }

        public int UnspentTransactionOutputsCount
        {
            get { return this.unspentOutputInfoList.Count; }
        }

        /// <summary>
        /// Gets the ID as stored in the database for this transaction 
        /// </summary>
        public long BitcoinTransactionId { get; private set; }

        /// <summary>
        /// Gets the hash for this transaction.
        /// </summary>
        public ByteArray TransactionHash { get; private set; }

        /// <summary>
        /// Will eliminate the information about the given output.
        /// </summary>
        /// <param name="outputIndex">
        /// Identifies the transaction output that will be eliminated.
        /// </param>
        /// <param name="unspentOutputInfo">
        /// At return, it will be set to an instance of type <see cref="UnspentOutputInfo"/> 
        /// containing information about the unspent output that was spent.
        /// </param>
        /// <returns>
        /// True  - the unspent output was found and spent.
        /// False - the unspent output was not found.
        /// </returns>
        public bool TrySpendOutput(int outputIndex, out UnspentOutputInfo unspentOutputInfo)
        {
            unspentOutputInfo = this.unspentOutputInfoList.FirstOrDefault(o => o.OutputIndex == outputIndex);
            if (unspentOutputInfo != null)
            {
                this.unspentOutputInfoList.Remove(unspentOutputInfo);
            }

            return unspentOutputInfo != null;
        }

        /// <summary>
        /// Implements <c>IEquatable&lt;UnspentTransactionInfo&gt;.Equals</c>
        /// </summary>
        /// <param name="other">
        /// The other instance of <see cref="UnspentTransactionInfo "/> to compare to.
        /// </param>
        /// <returns>
        /// True if the current object is equal to the other parameter; otherwise, false.
        /// </returns>
        public bool Equals(UnspentTransactionInfo other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            if (this.BitcoinTransactionId != other.BitcoinTransactionId)
            {
                return false;
            }

            if (this.TransactionHash != other.TransactionHash)
            {
                return false;
            }

            if (this.unspentOutputInfoList.Count != other.unspentOutputInfoList.Count)
            {
                return false;
            }

            List<UnspentOutputInfo> orderedOutputIndexesList1 = this.unspentOutputInfoList.OrderBy(o => o.OutputIndex).ToList();
            List<UnspentOutputInfo> orderedOutputIndexesList2 = other.unspentOutputInfoList.OrderBy(o => o.OutputIndex).ToList();

            for (int i = 0; i < orderedOutputIndexesList1.Count; i++)
            {
                if (orderedOutputIndexesList1[i].Equals(orderedOutputIndexesList2[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ID: {0}. Hash: {1}. List:", this.BitcoinTransactionId, this.TransactionHash);

            foreach (UnspentOutputInfo unspentOutputInfo in this.unspentOutputInfoList)
            {
                sb.AppendFormat(" [{0}, {1}]", unspentOutputInfo.OutputIndex, unspentOutputInfo.TransactionOutputId);
            }

            return sb.ToString();
        }
    }
}
