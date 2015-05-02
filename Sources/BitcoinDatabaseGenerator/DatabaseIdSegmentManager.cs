//-----------------------------------------------------------------------
// <copyright file="DatabaseIdSegmentManager.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;

    public class DatabaseIdSegmentManager
    {
        private readonly long initialTransactionId;

        private readonly long lastBlockId;
        private readonly long lastTransactionId;
        private readonly long lastTransactionInputId;
        private readonly long lastTransactionOutputId;

        private long currentBlockId;
        private long currentTransactionId;
        private long currentTransactionInputId;
        private long currentTransactionOutputId;

        public DatabaseIdSegmentManager(
            DatabaseIdManager databaseIdManager,
            long blockCount,
            long bitcoinTransactionCount,
            long transactionInputCount,
            long transactionOutputCount)
        {
            this.currentBlockId = databaseIdManager.GetNextBlockId(blockCount);
            this.currentTransactionId = databaseIdManager.GetNextTransactionId(bitcoinTransactionCount);
            this.currentTransactionInputId = databaseIdManager.GetNextTransactionInputId(transactionInputCount);
            this.currentTransactionOutputId = databaseIdManager.GetNextTransactionOutputId(transactionOutputCount);

            this.lastBlockId = this.currentBlockId + blockCount - 1;
            this.lastTransactionId = this.currentTransactionId + bitcoinTransactionCount - 1;
            this.lastTransactionInputId = this.currentTransactionInputId + transactionInputCount - 1;
            this.lastTransactionOutputId = this.currentTransactionOutputId + transactionOutputCount - 1;

            this.initialTransactionId = this.currentTransactionId;
        }

        public long GetNextBlockId()
        {
            if (this.currentBlockId > this.lastBlockId)
            {
                throw new InvalidOperationException("This method was invoked too many times");
            }

            return this.currentBlockId++;
        }

        public long GetNextTransactionId()
        {
            if (this.currentTransactionId > this.lastTransactionId)
            {
                throw new InvalidOperationException("This method was invoked too many times");
            }

            return this.currentTransactionId++;
        }

        public long GetNextTransactionInputId()
        {
            if (this.currentTransactionInputId > this.lastTransactionInputId)
            {
                throw new InvalidOperationException("This method was invoked too many times");
            }

            return this.currentTransactionInputId++;
        }

        public long GetNextTransactionOutputId()
        {
            if (this.currentTransactionOutputId > this.lastTransactionOutputId)
            {
                throw new InvalidOperationException("This method was invoked too many times");
            }

            return this.currentTransactionOutputId++;
        }

        public void ResetNextTransactionId()
        {
            this.currentTransactionId = this.initialTransactionId;
        }
    }
}
