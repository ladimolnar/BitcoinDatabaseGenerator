//-----------------------------------------------------------------------
// <copyright file="ParametersListRules.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers.ParameterParser
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A class that describes the rules associated with a list of parameters.
    /// A typical use is when the list of parameters for a command line tool must be 
    /// parsed.In that case the rules associated with all parameter can be described 
    /// by an instance of <see cref="ParametersListRules"/> class.
    /// </summary>
    public class ParametersListRules
    {
        /// <summary>
        /// The name of the help parameter as specified in the parameter list.
        /// </summary>
        internal const string HelpParameterName = "?";

        /// <summary>
        /// Indicates if the parameters are case insensitive or not.
        /// </summary>
        private readonly bool isCaseSensitive;

        /// <summary>
        /// The rules associated with a list of parameters.
        ///   Key: the parameter name.
        /// Value: the rules for that parameter.
        /// </summary>
        private readonly Dictionary<string, ParameterRules> rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParametersListRules"/> class.
        /// </summary>
        /// <param name="isCaseSensitive">
        /// Indicates if the parameters are case sensitive or not.
        /// </param>
        /// <param name="isHelpParameterAllowed">
        /// A value indicating whether the optional "/?" parameter is allowed.
        /// </param>
        public ParametersListRules(bool isCaseSensitive = false, bool isHelpParameterAllowed = false)
        {
            this.isCaseSensitive = isCaseSensitive;

            this.rules = new Dictionary<string, ParameterRules>();

            if (isHelpParameterAllowed)
            {
                this.AddParameterRules(ParameterRules.CreateParameter(HelpParameterName));
            }
        }

        /// <summary>
        /// Gets an enumerable for all the parameter rules.
        /// </summary>
        public IEnumerable<ParameterRules> Rules
        {
            get { return this.rules.Values; }
        }

        /// <summary>
        /// Adds the rules associated with a parameter.
        /// </summary>
        /// <param name="parameterRules">
        /// The rules associated with a parameter.
        /// </param>
        public void AddParameterRules(ParameterRules parameterRules)
        {
            if (parameterRules == null)
            {
                throw new ArgumentNullException("parameterRules");
            }

            string parameterName = this.isCaseSensitive ? parameterRules.ParameterName : parameterRules.ParameterName.ToUpperInvariant();
            this.rules.Add(parameterName, parameterRules);
        }

        /// <summary>
        /// Attempts to get the parameter rules associated with the specified parameter.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter name.
        /// </param>
        /// <param name="parameterRules">
        /// The parameter rules associated with the specified parameter.
        /// </param>
        /// <returns>
        /// true: if the rules for the specified parameter were found, otherwise false.
        /// </returns>
        public bool TryGetParameterRules(string parameterName, out ParameterRules parameterRules)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException("parameterName");
            }

            parameterName = this.isCaseSensitive ? parameterName : parameterName.ToUpperInvariant();
            return this.rules.TryGetValue(parameterName, out parameterRules);
        }
    }
}
