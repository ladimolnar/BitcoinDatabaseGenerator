//-----------------------------------------------------------------------
// <copyright file="BitcoinTransaction.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet.Data
{
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains information about a Bitcoin transaction as saved in the Bitcoin SQL database.
    /// For more information see: https://en.bitcoin.it/wiki/Transaction
    /// </summary>
    public class BitcoinTransaction
    {
        public BitcoinTransaction(
            long bitcoinTransactionId,
            long blockId,
            ByteArray transactionHash,
            int transactionVersion,
            int transactionLockTime)
        {
            this.BitcoinTransactionId = bitcoinTransactionId;
            this.BlockId = blockId;
            this.TransactionHash = transactionHash;
            this.TransactionVersion = transactionVersion;
            this.TransactionLockTime = transactionLockTime;
        }

        public long BitcoinTransactionId { get; private set; }

        public long BlockId { get; private set; }

        public int TransactionVersion { get; private set; }

        public int TransactionLockTime { get; private set; }

        public ByteArray TransactionHash { get; private set; }
    }
}
