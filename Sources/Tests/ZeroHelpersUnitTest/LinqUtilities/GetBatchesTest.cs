//-----------------------------------------------------------------------
// <copyright file="GetBatchesTest.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpersUnitTest.LinqUtilities
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ZeroHelpers;

    [TestClass]
    public class GetBatchesTest
    {
        [TestMethod]
        public void EmptySource()
        {
            int[] source = new int[0];

            foreach (IEnumerable<int> batch in source.GetBatches(1))
            {
                Assert.Fail("An empty source should not produce any batches.");
            }
        }

        [TestMethod]
        public void SourceWithOneElement()
        {
            int[] source = new int[1] { 123 };

            int batchesCount = 0;
            foreach (IEnumerable<int> batch in source.GetBatches(1))
            {
                batchesCount++;

                int batchSize = 0;
                foreach (int element in batch)
                {
                    Assert.AreEqual(123, element);
                    batchSize++;
                }

                Assert.AreEqual(1, batchSize);
            }

            Assert.AreEqual(1, batchesCount);
        }

        [TestMethod]
        public void LastBatchFull()
        {
            int[] source = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            int expectedValue = 0;
            int batchCount = 0;
            int totalElements = 0;

            foreach (IEnumerable<int> batch in source.GetBatches(5))
            {
                int batchSize = 0;
                foreach (int element in batch)
                {
                    Assert.AreEqual(expectedValue, element);
                    expectedValue++;
                    batchSize++;
                    totalElements++;
                }

                Assert.AreEqual(5, batchSize);

                batchCount++;
            }

            Assert.AreEqual(10, totalElements);
            Assert.AreEqual(2, batchCount);
        }

        [TestMethod]
        public void LastBatchIncomplete()
        {
            int[] source = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            int expectedValue = 0;
            int batchCount = 0;
            int totalElements = 0;

            foreach (IEnumerable<int> batch in source.GetBatches(3))
            {
                int batchSize = 0;
                foreach (int element in batch)
                {
                    Assert.AreEqual(expectedValue, element);
                    expectedValue++;
                    batchSize++;
                    totalElements++;
                }

                if (batchCount < 3)
                {
                    Assert.AreEqual(3, batchSize, "A regular batch should have a size of 3");
                }
                else
                {
                    Assert.AreEqual(1, batchSize, "The last batch should have a size of 1");
                }

                batchCount++;
            }

            Assert.AreEqual(10, totalElements);
            Assert.AreEqual(4, batchCount);
        }

        [TestMethod]
        public void FirstBatchIncomplete()
        {
            int[] source = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            int expectedValue = 0;
            int batchCount = 0;
            int totalElements = 0;

            foreach (IEnumerable<int> batch in source.GetBatches(11))
            {
                int batchSize = 0;
                foreach (int element in batch)
                {
                    Assert.AreEqual(expectedValue, element);
                    expectedValue++;
                    batchSize++;
                    totalElements++;
                }

                Assert.AreEqual(10, batchSize);

                batchCount++;
            }

            Assert.AreEqual(10, totalElements);
            Assert.AreEqual(1, batchCount);
        }
    }
}
