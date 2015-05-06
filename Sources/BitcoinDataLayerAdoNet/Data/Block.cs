//-----------------------------------------------------------------------
// <copyright file="Block.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet.Data
{
    using System;
    using BitcoinBlockchain.Data;

    /// <summary>
    /// Contains information about a Bitcoin block as saved in the Bitcoin SQL database.
    /// For more information see: https://en.bitcoin.it/wiki/Block
    /// </summary>
    public class Block
    {
        public Block(long blockId, int blockchainFileId, int blockVersion, ByteArray blockHash, ByteArray previousBlockHash, DateTime blockTimestamp)
        {
            this.BlockId = blockId;
            this.BlockchainFileId = blockchainFileId;
            this.BlockVersion = blockVersion;
            this.BlockHash = blockHash;
            this.PreviousBlockHash = previousBlockHash;
            this.BlockTimestamp = blockTimestamp;
        }

        public long BlockId { get; private set; }

        public int BlockchainFileId { get; private set; }

        public int BlockVersion { get; private set; }

        public DateTime BlockTimestamp { get; private set; }

        public ByteArray BlockHash { get; private set; }

        public ByteArray PreviousBlockHash { get; private set; }
    }
}
