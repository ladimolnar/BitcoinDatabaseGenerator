//-----------------------------------------------------------------------
// <copyright file="BlockchainFile.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet.Data
{
    /// <summary>
    /// Contains information about a Bitcoin blockchain file as saved in the Bitcoin SQL database.
    /// </summary>
    public class BlockchainFile
    {
        public BlockchainFile(int blockchainFileId, string blockchainFileName)
        {
            this.BlockchainFileId = blockchainFileId;
            this.BlockchainFileName = blockchainFileName;
        }

        public int BlockchainFileId { get; private set; }

        public string BlockchainFileName { get; private set; }
    }
}
