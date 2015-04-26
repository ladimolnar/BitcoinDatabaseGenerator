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
        public BlockchainFile(int blockFileId, string fileName)
        {
            this.BlockFileId = blockFileId;
            this.FileName = fileName;
        }

        public int BlockFileId { get; private set; }

        public string FileName { get; private set; }
    }
}
