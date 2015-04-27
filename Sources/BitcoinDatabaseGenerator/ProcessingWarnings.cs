//-----------------------------------------------------------------------
// <copyright file="ProcessingWarnings.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;
    using System.Collections.Generic;

    public class ProcessingWarnings
    {
        private readonly List<string> warningsList;

        public ProcessingWarnings()
        {
            this.warningsList = new List<string>();
        }

        public int Count
        {
            get { return this.warningsList.Count; }
        }

        public void AddWarning(string warning)
        {
            this.warningsList.Add(warning);
        }

        public void DisplayWarnings()
        {
            if (this.warningsList.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("{0} warnings were detected:", this.warningsList.Count);

                Console.WriteLine();
                foreach (string warningText in this.warningsList)
                {
                    Console.WriteLine(warningText);
                }
            }
        }
    }
}
