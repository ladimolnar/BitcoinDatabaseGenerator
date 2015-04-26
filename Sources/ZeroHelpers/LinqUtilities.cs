//-----------------------------------------------------------------------
// <copyright file="LinqUtilities.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class that contains utility methods that extend the capabilities of Linq.
    /// </summary>
    public static class LinqUtilities
    {
        /// <summary>
        /// Provides access to the elements of an enumerator in batches.
        /// </summary>
        /// <typeparam name="T">The type of the elements that are enumerated.</typeparam>
        /// <param name="source">The source enumerator that will be split in batches.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <returns>
        /// A enumerable of enumerable. 
        /// The inner enumerable provides access to the elements in a batch. 
        /// The outer enumerable provides access to batches.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This method will be deleted")]
        public static IEnumerable<IEnumerable<T>> GetBatches<T>(this IEnumerable<T> source, int batchSize)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException("batchSize");
            }

            T[] batch = new T[batchSize];
            int indexInBatch = 0;

            foreach (T sourceElement in source)
            {
                batch[indexInBatch++] = sourceElement;

                if (indexInBatch == batchSize)
                {
                    yield return batch;
                    indexInBatch = 0;
                }
            }

            if (indexInBatch > 0)
            {
                yield return batch.Take(indexInBatch);
            }
        }
    }
}
