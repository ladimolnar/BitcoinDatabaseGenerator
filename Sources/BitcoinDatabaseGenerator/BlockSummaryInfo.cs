//-----------------------------------------------------------------------
// <copyright file="BlockSummaryInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using ParserData = BitcoinBlockchain.Data;

    public class BlockSummaryInfo
    {
        public BlockSummaryInfo(string blockchainFileName, ParserData.ByteArray blockHash, ParserData.ByteArray previousBlockHash)
        {
            this.IsActive = false;
            this.BlockchainFileName = blockchainFileName;
            this.BlockHash = blockHash;
            this.PreviousBlockHash = previousBlockHash;
        }

        public bool IsActive { get; set; }

        public string BlockchainFileName { get; private set; }

        public ParserData.ByteArray BlockHash { get; private set; }

        public ParserData.ByteArray PreviousBlockHash { get; private set; }
    }
}
