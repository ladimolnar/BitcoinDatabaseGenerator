//-----------------------------------------------------------------------
// <copyright file="ParameterInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers.ParameterParser
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains information about one parameter that resulted after parsing a list of parameters.
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterInfo"/> class.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter name.
        /// </param>
        internal ParameterInfo(string parameterName)
        {
            this.ParameterName = parameterName;
            this.Arguments = new List<string>();
        }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string ParameterName { get; private set; }

        /// <summary>
        /// Gets the one argument associated with this parameter.
        /// If this parameter is not associated with rules that enforce the existence of one and only one argument then 
        /// this method may throw at run time. See <see cref="ParametersListRules"/> and <see cref="ParameterRules"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The parameter has no arguments associated with it or has more than one argument associated with it.
        /// </exception>
        public string Argument
        {
            get
            {
                if (this.Arguments.Count != 1)
                {
                    throw new InvalidOperationException("Argument can only be called for a parameter that has exactly one argument.");
                }

                return this.Arguments[0];
            }
        }

        /// <summary>
        /// Gets the arguments associated with this parameter.
        /// </summary>
        public List<string> Arguments { get; private set; }

        /// <summary>
        /// Add an argument associated with this parameter.
        /// </summary>
        /// <param name="argument">The argument that will be added.</param>
        internal void AddArgument(string argument)
        {
            this.Arguments.Add(argument);
        }
    }
}
