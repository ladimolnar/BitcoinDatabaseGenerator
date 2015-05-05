//-----------------------------------------------------------------------
// <copyright file="InvalidEnvironmentException.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDatabaseGenerator
{
    using System;

    /// <summary>
    /// The exception that is thrown when some external conditions do not allow the application to proceed.
    /// </summary>
    [Serializable]
    public class InvalidEnvironmentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidEnvironmentException"/> class.
        /// </summary>
        public InvalidEnvironmentException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidEnvironmentException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public InvalidEnvironmentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidEnvironmentException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception,
        /// or a null reference if no inner exception is specified.
        /// </param>
        public InvalidEnvironmentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
