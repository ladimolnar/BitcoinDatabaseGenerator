//-----------------------------------------------------------------------
// <copyright file="UnspentTransactionLookup.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains a dictionary with information of all unspent transactions. 
    /// </summary>
    public class UnspentTransactionLookup : IEquatable<UnspentTransactionLookup>
    {
        private readonly Dictionary<ByteArray, UnspentTransactionInfo> unspentTransactionsDictionary;
        private readonly ProcessingWarnings processingWarnings;

        public UnspentTransactionLookup(ProcessingWarnings processingWarnings)
        {
            this.processingWarnings = processingWarnings;
            this.unspentTransactionsDictionary = new Dictionary<ByteArray, UnspentTransactionInfo>();
        }

        public int UnspentTransactionsCount
        {
            get { return this.unspentTransactionsDictionary.Count; }
        }

        public int UnspentTransactionOutputsCount
        {
            get { return this.unspentTransactionsDictionary.Sum(t => t.Value.UnspentTransactionOutputsCount); }
        }

        /// <summary>
        /// Implements <c>IEquatable&lt;UnspentTransactionLookup&gt;.Equals</c>
        /// </summary>
        /// <param name="other">
        /// The other instance of <see cref="UnspentTransactionLookup "/> to compare to.
        /// </param>
        /// <returns>
        /// True if the current object is equal to the other parameter; otherwise, false.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "UnspentTransactionLookup", Justification = "UnspentTransactionLookup refers to the name of a method.")]
        public bool Equals(UnspentTransactionLookup other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            List<UnspentTransactionInfo> unspentTransactionInfoList1 = this.unspentTransactionsDictionary.Values.OrderBy(t => t.BitcoinTransactionId).ToList();
            List<UnspentTransactionInfo> unspentTransactionInfoList2 = other.unspentTransactionsDictionary.Values.OrderBy(t => t.BitcoinTransactionId).ToList();

            if (unspentTransactionInfoList1.Count != unspentTransactionInfoList2.Count)
            {
                // Console.WriteLine("UnspentTransactionLookup.Equals found a difference: L1.Count: {0}. L2.Count: {1}.", unspentTransactionInfoList1.Count, unspentTransactionInfoList2.Count);
                return false;
            }

            for (int i = 0; i < unspentTransactionInfoList1.Count; i++)
            {
                if (unspentTransactionInfoList1[i].Equals(unspentTransactionInfoList2[i]) == false)
                {
                    // Console.WriteLine("UnspentTransactionLookup.Equals found a difference: i: {0}. U1: {1}. U2: {2}.", i, unspentTransactionInfoList1[i], unspentTransactionInfoList2[i]);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called when a transaction output is being spent.
        /// If the transaction output being spent was the last unspent output of a transaction, 
        /// that transaction is removed from the list maintained by this instance of type <see cref="UnspentTransactionLookup"/>.
        /// </summary>
        /// <param name="spenderTransactionHash">
        /// The hash that identifies the "spender" transaction.
        /// </param>
        /// <param name="sourceTransactionHash">
        /// The hash that identifies the transaction that has the output that is being spent.
        /// </param>
        /// <param name="outputIndex">
        /// Identifies the transaction output that is being spent.
        /// </param>
        /// <returns>
        /// The ID that identifies in the database the transaction output that is being spent.
        /// </returns>
        public long SpendTransactionOutput(ByteArray spenderTransactionHash, ByteArray sourceTransactionHash, int outputIndex)
        {
            UnspentTransactionInfo unspentTransactionInfo;
            if (this.unspentTransactionsDictionary.TryGetValue(sourceTransactionHash, out unspentTransactionInfo) == false)
            {
                this.processingWarnings.AddWarning(string.Format(
                    CultureInfo.InvariantCulture,
                    "An attempt was detected to spend an output on a transaction that was not found or was already entirely spent.\n This may be a case where the spender is a duplicate transaction. Spender transaction hash: {0}. Source Transaction Hash: {1}. Output Index: {2}",
                    spenderTransactionHash,
                    sourceTransactionHash,
                    outputIndex));

                return -1;
            }

            UnspentOutputInfo unspentOutputInfo;
            if (unspentTransactionInfo.TrySpendOutput(outputIndex, out unspentOutputInfo) == false)
            {
                this.processingWarnings.AddWarning(string.Format(
                    CultureInfo.InvariantCulture,
                    "An attempt was detected to spend an output that was not found or was already spent.\n This may be a case where the spender is a duplicate transaction. Spender transaction hash: {0}. Source Transaction Hash: {1}. Output Index: {2}",
                    spenderTransactionHash,
                    sourceTransactionHash,
                    outputIndex));

                return -1;
            }

            if (unspentTransactionInfo.UnspentTransactionOutputsCount == 0)
            {
                this.unspentTransactionsDictionary.Remove(sourceTransactionHash);
            }

            return unspentOutputInfo.TransactionOutputId;
        }

        /// <summary>
        /// Adds information about a transaction that has unspent outputs.
        /// </summary>
        /// <param name="unspentTransactionInfo">
        /// Contains information about the transaction with unspent outputs that will be added.
        /// </param>
        /// <param name="dataOrigin">
        /// Specifies the origin of the data that is being processed.
        /// </param>
        public void AddUnspentTransactionInfo(UnspentTransactionInfo unspentTransactionInfo, DataOrigin dataOrigin)
        {
            if (dataOrigin == DataOrigin.Blockchain)
            {
                UnspentTransactionInfo existingUnspentTransactionInfo;
                if (this.unspentTransactionsDictionary.TryGetValue(unspentTransactionInfo.TransactionHash, out existingUnspentTransactionInfo))
                {
                    // We already have an unspent transaction stored in unspentTransactionsDictionary that has the same hash as the new unspent transaction. 
                    // The existing unspent transaction will have to be overwritten. It also means that it is unspendable. 
                    // This is a known issue for some early Bitcoin transactions. 
                    // These two transaction hashes are expected to be in this situation: 
                    //  - D5D27987D2A3DFC724E359870C6644B40E497BDC0589A033220FE15429D88599
                    //  - E3BF3D07D4B0375638D5F1DB5255FE07BA2C4CB067CD81B84EE974B6585FB468
                    this.processingWarnings.AddWarning(string.Format(
                        CultureInfo.InvariantCulture,
                        "An unspendable transaction was detected. Transaction database id: {0}. Transaction hash: {1}.",
                        existingUnspentTransactionInfo.BitcoinTransactionId,
                        existingUnspentTransactionInfo.TransactionHash));
                }
            }

            this.unspentTransactionsDictionary[unspentTransactionInfo.TransactionHash] = unspentTransactionInfo;
        }
    }
}
