//-----------------------------------------------------------------------
// <copyright file="SampleByteArray.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGeneratorIntegrationTest.Helpers
{
    using BitcoinBlockchain.Data;

    public static class SampleByteArray
    {
        public static ByteArray GetSampleByteArray(byte value)
        {
            return GetSampleByteArray(32, value);
        }

        public static ByteArray GetSampleByteArray(int size, byte value)
        {
            byte[] byteArray = new byte[size];
            byteArray[size - 1] = value;

            return new ByteArray(byteArray);
        }
    }
}
