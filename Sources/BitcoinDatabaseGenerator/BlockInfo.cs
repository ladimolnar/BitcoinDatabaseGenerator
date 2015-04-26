//-----------------------------------------------------------------------
// <copyright file="BlockInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System.Collections.Generic;
    using DBData = BitcoinDataLayerAdoNet.Data;
    
    internal class BlockInfo
    {
        internal BlockInfo()
        {
            this.BitcoinTransactions = new List<DBData.BitcoinTransaction>();
            this.TransactionInputs = new List<DBData.TransactionInput>();
            this.TransactionInputSources = new List<DBData.TransactionInputSource>();
            this.TransactionOutputs = new List<DBData.TransactionOutput>();
        }

        public DBData.Block Block { get; set; }

        public List<DBData.BitcoinTransaction> BitcoinTransactions { get; private set; }

        public List<DBData.TransactionInput> TransactionInputs { get; private set; }

        public List<DBData.TransactionInputSource> TransactionInputSources { get; private set; }

        public List<DBData.TransactionOutput> TransactionOutputs { get; private set; }
    }
}
