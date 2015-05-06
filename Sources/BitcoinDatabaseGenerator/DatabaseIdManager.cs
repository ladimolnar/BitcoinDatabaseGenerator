//-----------------------------------------------------------------------
// <copyright file="DatabaseIdManager.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    public class DatabaseIdManager
    {
        private int currentBlockchainFileId;
        private long currentBlockId;
        private long currentTransactionId;
        private long currentTransactionInputId;
        private long currentTransactionOutputId;

        public DatabaseIdManager(int blockFileId, long blockId, long bitcoinTransactionId, long transactionInputId, long transactionOutputId)
        {
            this.currentBlockchainFileId = blockFileId;
            this.currentBlockId = blockId;
            this.currentTransactionId = bitcoinTransactionId;
            this.currentTransactionInputId = transactionInputId;
            this.currentTransactionOutputId = transactionOutputId;
        }

        public int GetNextBlockchainFileId(int increment)
        {
            int result = this.currentBlockchainFileId;
            this.currentBlockchainFileId += increment;
            return result;
        }

        public long GetNextBlockId(long increment)
        {
            long result = this.currentBlockId;
            this.currentBlockId += increment;
            return result;
        }

        public long GetNextTransactionId(long increment)
        {
            long result = this.currentTransactionId;
            this.currentTransactionId += increment;
            return result;
        }

        public long GetNextTransactionInputId(long increment)
        {
            long result = this.currentTransactionInputId;
            this.currentTransactionInputId += increment;
            return result;
        }

        public long GetNextTransactionOutputId(long increment)
        {
            long result = this.currentTransactionOutputId;
            this.currentTransactionOutputId += increment;
            return result;
        }
    }
}
