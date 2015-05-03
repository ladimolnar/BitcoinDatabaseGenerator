//-----------------------------------------------------------------------
// <copyright file="ParametersListInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers.ParameterParser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Contains information resulted from parsing a list of parameters.
    /// A typical use is to parse the list of parameters for a command line tool.
    /// </summary>
    public class ParametersListInfo
    {
        /// <summary>
        /// Indicates if the parameters are case insensitive or not.
        /// </summary>
        private readonly bool isCaseSensitive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParametersListInfo"/> class.
        /// </summary>
        /// <param name="isCaseSensitive">
        /// Indicates if the parameters are case sensitive or not.
        /// </param>
        public ParametersListInfo(bool isCaseSensitive = false)
        {
            this.isCaseSensitive = isCaseSensitive;
            this.Parameters = new Dictionary<string, ParameterInfo>();
        }

        /// <summary>
        /// Gets a dictionary containing information about all parameters that were parsed.
        ///   Key: the parameter name.
        /// Value: An instance of <see cref="ParameterInfo"/> class.
        /// </summary>
        public Dictionary<string, ParameterInfo> Parameters { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the "/?" parameter was specified .
        /// </summary>
        public bool IsHelpSpecified
        {
            get
            {
                return this.ParameterWasSpecified(ParametersListRules.HelpParameterName);
            }
        }

        /// <summary>
        /// Gets the value associated with the specified parameter.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter for witch the parameter information is retrieved.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ParameterInfo"/> class.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The given parameter was not found.
        /// </exception>
        public ParameterInfo this[string parameterName]
        {
            get
            {
                ParameterInfo parameterInfo;
                if (this.TryGetParameterInfo(parameterName, out parameterInfo) == false)
                { 
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Parameter: {0} was not specified.", parameterName));
                }

                return parameterInfo;
            }
        }

        /// <summary>
        /// Determines if the given parameter was specified.
        /// </summary>
        /// <param name="parameterName">The name of the target parameter.</param>
        /// <returns>
        /// True if the given parameter was specified, otherwise false.
        /// </returns>
        public bool ParameterWasSpecified(string parameterName)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException("parameterName");
            }

            parameterName = this.isCaseSensitive ? parameterName : parameterName.ToUpperInvariant();
            return this.Parameters.ContainsKey(parameterName);
        }

        /// <summary>
        /// Attempt to get the parameter information for the given parameter.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter for witch the parameter information is retrieved.
        /// </param>
        /// <param name="parameterInfo">
        /// At return will contain parameter information if the parameter is found; 
        /// otherwise, null.
        /// </param>
        /// <returns>
        /// true if the parameter information was found, otherwise, false.
        /// </returns>
        public bool TryGetParameterInfo(string parameterName, out ParameterInfo parameterInfo)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException("parameterName");
            }

            parameterName = this.isCaseSensitive ? parameterName : parameterName.ToUpperInvariant();
            return this.Parameters.TryGetValue(parameterName, out parameterInfo);
        }

        /// <summary>
        /// Attempt to get the parameter information for the given parameter.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter for witch the parameter information is retrieved.
        /// </param>
        /// <returns>
        /// The parameter information for the given parameter.
        /// </returns>
        public ParameterInfo GetParameterInfo(string parameterName)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException("parameterName");
            }

            parameterName = this.isCaseSensitive ? parameterName : parameterName.ToUpperInvariant();
            return this.Parameters[parameterName];
        }

        /// <summary>
        /// Validates the parameter values.
        /// </summary>
        /// <exception cref="InvalidParameterException">
        /// Thrown when a parameter is found to be invalid.
        /// </exception>
        public virtual void Validate()
        {
        }

        /// <summary>
        /// information resulted from parsing one parameter in a list of parameters.
        /// </summary>
        /// <param name="parameterInfo">
        /// An instance of <see cref="ParameterInfo"/> class.
        /// </param>
        /// <exception cref="InvalidParameterException">
        /// Thrown when an attempt to add same parameter is a second time is made.
        /// </exception>
        internal void Add(ParameterInfo parameterInfo)
        {
            string parameterName = this.isCaseSensitive ? parameterInfo.ParameterName : parameterInfo.ParameterName.ToUpperInvariant();
            if (this.ParameterWasSpecified(parameterName))
            {
                throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "Parameter {0} is specified more than once", parameterInfo.ParameterName));
            }

            this.Parameters[parameterName] = parameterInfo;
        }
    }
}
