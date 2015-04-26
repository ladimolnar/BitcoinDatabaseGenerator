//-----------------------------------------------------------------------
// <copyright file="TransactionOutput.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet.Data
{
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains information about a Bitcoin transaction output as saved in the Bitcoin SQL database.
    /// For more information see:
    /// <c>https://en.bitcoin.it/wiki/Transaction#general_format_.28inside_a_block.29_of_each_output_of_a_transaction_-_Txout</c>
    /// </summary>
    public class TransactionOutput
    {
        public TransactionOutput(
            long transactionOutputId,
            long bitcoinTransactionId,
            int outputIndex,
            decimal outputValueBtc,
            ByteArray outputScript)
        {
            this.TransactionOutputId = transactionOutputId;
            this.BitcoinTransactionId = bitcoinTransactionId;
            this.OutputIndex = outputIndex;
            this.OutputValueBtc = outputValueBtc;
            this.OutputScript = outputScript;
        }

        public long TransactionOutputId { get; private set; }

        public long BitcoinTransactionId { get; private set; }

        /// <summary>
        /// Gets the output index. 
        /// The output index is an index defined in the scope of the Bitcoin transaction that contains this output.
        /// Useful when we need to lookup the source output for an input in a subsequent transaction.
        /// </summary>
        public int OutputIndex { get; private set; }

        public decimal OutputValueBtc { get; private set; }

        public ByteArray OutputScript { get; private set; }
    }
}
