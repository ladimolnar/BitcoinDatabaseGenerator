//-----------------------------------------------------------------------
// <copyright file="InvalidParameterException.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers.ParameterParser
{
    using System;

    /// <summary>
    /// The exception that is thrown when a parameter is found to be invalid while parsing a list of parameters.
    /// </summary>
    public class InvalidParameterException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidParameterException"/> class.
        /// </summary>
        public InvalidParameterException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidParameterException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public InvalidParameterException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidParameterException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception,
        /// or a null reference if no inner exception is specified.
        /// </param>
        public InvalidParameterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
