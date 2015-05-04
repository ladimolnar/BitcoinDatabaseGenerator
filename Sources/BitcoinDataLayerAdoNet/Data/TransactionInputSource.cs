//-----------------------------------------------------------------------
// <copyright file="TransactionInputSource.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet.Data
{
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains information about a Bitcoin transaction input source as saved in the Bitcoin SQL database.
    /// In the database, in table TransactionInputSource we store "row" data indicating the source of a transaction input.
    /// At the end of the processing, after we delete the stale blocks, the information in table TransactionInputSource 
    /// is processed in order to calculate the values for column TransactionInput.SourceTransactionOutputId.
    /// </summary>
    public class TransactionInputSource
    {
        public TransactionInputSource(long transactionInputId, ByteArray sourceTransactionHash, int sourceTransactionOutputIndex)
        {
            this.TransactionInputId = transactionInputId;
            this.SourceTransactionHash = sourceTransactionHash;
            this.SourceTransactionOutputIndex = sourceTransactionOutputIndex;
        }

        public long TransactionInputId { get; private set; }

        public ByteArray SourceTransactionHash { get; private set; }

        public int SourceTransactionOutputIndex { get; private set; }
    }
}
