//-----------------------------------------------------------------------
// <copyright file="InputInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Helpers
{
    using BitcoinBlockchain.Data;

    public class InputInfo
    {
        public InputInfo(ByteArray sourceTransactionHash, uint sourceTransactionOutputIndex)
        {
            this.SourceTransactionHash = sourceTransactionHash;
            this.SourceTransactionOutputIndex = sourceTransactionOutputIndex;
        }

        public ByteArray SourceTransactionHash { get; set; }

        public uint SourceTransactionOutputIndex { get; set; }
    }
}
