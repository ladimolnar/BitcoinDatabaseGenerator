//-----------------------------------------------------------------------
// <copyright file="DatabaseIdManager.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    internal class DatabaseIdManager
    {
        private long currentBlockId;
        private long currentTransactionId;
        private long currentTransactionInputId;
        private long currentTransactionOutputId;

        internal DatabaseIdManager(int blockFileId, long blockId, long bitcoinTransactionId, long transactionInputId, long transactionOutputId)
        {
            this.CurrentBlockFileId = blockFileId;
            this.currentBlockId = blockId;
            this.currentTransactionId = bitcoinTransactionId;
            this.currentTransactionInputId = transactionInputId;
            this.currentTransactionOutputId = transactionOutputId;
        }

        internal int CurrentBlockFileId { get; private set; }

        internal int GetNextBlockFileId()
        {
            return ++this.CurrentBlockFileId;
        }

        internal long GetNextBlockId()
        {
            return ++this.currentBlockId;
        }

        internal long GetNextTransactionId()
        {
            return ++this.currentTransactionId;
        }

        internal long GetNextTransactionInputId()
        {
            return ++this.currentTransactionInputId;
        }

        internal long GetNextTransactionOutputId()
        {
            return ++this.currentTransactionOutputId;
        }
    }
}
