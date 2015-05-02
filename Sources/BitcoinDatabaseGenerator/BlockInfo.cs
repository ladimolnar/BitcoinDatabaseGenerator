//-----------------------------------------------------------------------
// <copyright file="BlockInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using BitcoinDataLayerAdoNet.DataSets;

    // @@@ rename once we store data for more than a block.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "No need to dispose a DataSet")]
    internal class BlockInfo
    {
        private readonly BlockchainDataSet blockchainDataSet;

        internal BlockInfo()
        {
            this.blockchainDataSet = new BlockchainDataSet();
        }

        public BlockchainDataSet.BlockDataTable BlockDataTable
        {
            get { return this.blockchainDataSet.Block; }
        }

        public BlockchainDataSet.BitcoinTransactionDataTable BitcoinTransactionDataTable
        {
            get { return this.blockchainDataSet.BitcoinTransaction; }
        }

        public BlockchainDataSet.TransactionInputDataTable TransactionInputDataTable
        {
            get { return this.blockchainDataSet.TransactionInput; }
        }

        public BlockchainDataSet.TransactionInputSourceDataTable TransactionInputSourceDataTable
        {
            get { return this.blockchainDataSet.TransactionInputSource; }
        }

        public BlockchainDataSet.TransactionOutputDataTable TransactionOutputDataTable
        {
            get { return this.blockchainDataSet.TransactionOutput; }
        }

        public bool IsFull
        {
            get
            {
                return
                    this.BlockDataTable.Rows.Count +
                    this.BitcoinTransactionDataTable.Rows.Count +
                    this.TransactionInputDataTable.Rows.Count +
                    this.TransactionInputSourceDataTable.Rows.Count +
                    this.TransactionOutputDataTable.Rows.Count >= 1000000;
            }
        }
    }
}
