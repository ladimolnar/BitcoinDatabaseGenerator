//-----------------------------------------------------------------------
// <copyright file="FakeBlockchainParser.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Helpers
{
    using System.Collections.Generic;
    using BitcoinBlockchain.Data;
    using BitcoinBlockchain.Parser;

    public class FakeBlockchainParser : IBlockchainParser
    {
        private readonly IEnumerable<Block> blocks;

        public FakeBlockchainParser(IEnumerable<Block> blocks)
        {
            this.blocks = blocks;
        }

        public IEnumerable<Block> ParseBlockchain()
        {
            return this.blocks;
        }

        public void SetBlockId(uint blockId)
        {
            throw new System.NotSupportedException();
        }
    }
}
