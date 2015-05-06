//-----------------------------------------------------------------------
// <copyright file="ProcessingStatistics.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Diagnostics;

    public class ProcessingStatistics
    {
        private readonly object lockObject;

        private readonly Stopwatch preprocessingWatch;
        private readonly Stopwatch processingBlockchainWatch;
        private readonly Stopwatch postProcessingWatch;
        private readonly Stopwatch totalProcessingWatch;

        public ProcessingStatistics()
        {
            this.lockObject = new object();

            this.preprocessingWatch = new Stopwatch();
            this.processingBlockchainWatch = new Stopwatch();
            this.postProcessingWatch = new Stopwatch();
            this.totalProcessingWatch = new Stopwatch();
        }

        public int BlockchainFilesCount { get; private set; }

        public int BlocksCount { get; private set; }

        public long TransactionsCount { get; private set; }

        public long TransactionInputsCount { get; private set; }

        public long TransactionOutputsCount { get; private set; }

        public TimeSpan PreprocessingDuration
        {
            get { return this.preprocessingWatch.Elapsed; }
        }

        public TimeSpan ProcessingBlockchainDuration
        {
            get { return this.processingBlockchainWatch.Elapsed; }
        }

        public TimeSpan PostProcessingDuration
        {
            get { return this.postProcessingWatch.Elapsed; }
        }

        public TimeSpan TotalProcessingDuration
        {
            get { return this.totalProcessingWatch.Elapsed; }
        }

        public void AddBlockchainFilesCount(int count)
        {
            lock (this.lockObject)
            {
                this.BlockchainFilesCount += count;
            }
        }

        public void AddBlocksCount(int count)
        {
            lock (this.lockObject)
            {
                this.BlocksCount += count;
            }
        }

        public void AddTransactionsCount(int count)
        {
            lock (this.lockObject)
            {
                this.TransactionsCount += count;
            }
        }

        public void AddTransactionInputsCount(int count)
        {
            lock (this.lockObject)
            {
                this.TransactionInputsCount += count;
            }
        }

        public void AddTransactionOutputsCount(int count)
        {
            lock (this.lockObject)
            {
                this.TransactionOutputsCount += count;
            }
        }

        public void PreprocessingStarting()
        {
            this.totalProcessingWatch.Start();
            this.preprocessingWatch.Start();
        }

        public void ProcessingBlockchainStarting()
        {
            this.preprocessingWatch.Stop();
            this.processingBlockchainWatch.Start();
        }

        public void PostProcessingStarting()
        {
            this.processingBlockchainWatch.Stop();
            this.postProcessingWatch.Start();
        }

        public void ProcessingCompleted()
        {
            this.postProcessingWatch.Stop();
            this.totalProcessingWatch.Stop();
        }

        public void DisplayStatistics()
        {
            Console.WriteLine();
            Console.WriteLine("Processing summary:");
            Console.WriteLine();
            Console.WriteLine("                 Block files: {0,14:n0}", this.BlockchainFilesCount);
            Console.WriteLine("                      Blocks: {0,14:n0}", this.BlocksCount);
            Console.WriteLine("                Transactions: {0,14:n0}", this.TransactionsCount);
            Console.WriteLine("          Transaction Inputs: {0,14:n0}", this.TransactionInputsCount);
            Console.WriteLine("         Transaction Outputs: {0,14:n0}", this.TransactionOutputsCount);
            Console.WriteLine();

            TimeSpan preprocessingDuration = this.PreprocessingDuration;
            Console.WriteLine("           Pre transfer time: {0,10:0.000} seconds", preprocessingDuration.TotalSeconds);

            TimeSpan processingBlockchainDuration = this.ProcessingBlockchainDuration;
            Console.WriteLine("    Blockchain transfer time: {0,10:0.000} seconds", processingBlockchainDuration.TotalSeconds);

            TimeSpan postProcessingDuration = this.PostProcessingDuration;
            Console.WriteLine("          Post transfer time: {0,10:0.000} seconds", postProcessingDuration.TotalSeconds);

            TimeSpan totalDuration = this.TotalProcessingDuration;
            Console.WriteLine("                  Total time: {0,10:0.000} seconds", totalDuration.TotalSeconds);

            Console.WriteLine();

            if (this.BlockchainFilesCount > 0)
            {
                TimeSpan averageBlockchainFileDuration = new TimeSpan(processingBlockchainDuration.Ticks / this.BlockchainFilesCount);
                Console.WriteLine("    On average a blockchain file was transferred in {0:0.000} seconds.", averageBlockchainFileDuration.TotalSeconds);
            }
            else
            {
                Console.WriteLine("    No blocks were processed.");
            }
        }
    }
}
