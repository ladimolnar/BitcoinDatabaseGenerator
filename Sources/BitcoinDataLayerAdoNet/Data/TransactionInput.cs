//-----------------------------------------------------------------------
// <copyright file="TransactionInput.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet.Data
{
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains information about a Bitcoin transaction input as saved in the Bitcoin SQL database.
    /// For more information see:
    /// <c>https://en.bitcoin.it/wiki/Transaction#general_format_.28inside_a_block.29_of_each_input_of_a_transaction_-_Txin</c>
    /// </summary>
    public class TransactionInput
    {
        public const int SourceTransactionOutputIdUnknown = -1;

        public TransactionInput(
            long transactionInputId,
            long bitcoinTransactionId,
            long? sourceTransactionOutputId)
        {
            this.TransactionInputId = transactionInputId;
            this.BitcoinTransactionId = bitcoinTransactionId;
            this.SourceTransactionOutputId = sourceTransactionOutputId;
        }

        public long TransactionInputId { get; private set; }

        public long BitcoinTransactionId { get; private set; }

        /// <summary>
        /// Gets the ID of the transaction output that will be consumed by this input.
        /// Will be null if this input does not consume an output.
        /// Note: The blockchain contains the hash of the source transaction and the index of the output in that transaction.
        ///       When the data is transferred to the DB, we convert that information to the database ID of the source output.
        /// </summary>
        public long? SourceTransactionOutputId { get; private set; }
    }
}
