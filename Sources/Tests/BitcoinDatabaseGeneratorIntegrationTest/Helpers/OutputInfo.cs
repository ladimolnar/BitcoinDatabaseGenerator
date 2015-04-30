//-----------------------------------------------------------------------
// <copyright file="OutputInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Helpers
{
    public class OutputInfo
    {
        public OutputInfo(decimal outputValueSatoshi)
        {
            this.OutputValueSatoshi = outputValueSatoshi;
        }

        public decimal OutputValueSatoshi { get; set; }
    }
}
