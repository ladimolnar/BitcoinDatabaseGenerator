//-----------------------------------------------------------------------
// <copyright file="DataOrigin.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    /// <summary>
    /// Specifies the origin of the data that is being processed.
    /// </summary>
    public enum DataOrigin
    {
        /// <summary>
        /// The data that is processed originates from the database.
        /// </summary>
        Database,

        /// <summary>
        /// The data that is processed originates from the 
        /// blockchain before being transferred in the database.
        /// </summary>
        Blockchain,
    }
}
