//-----------------------------------------------------------------------
// <copyright file="ParameterRules.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers.ParameterParser
{
    /// <summary>
    /// A class that describes the rules associated with a parameter in a list of parameters.
    /// A typical use is when the list of parameters for a command line tool must be 
    /// parsed.In that case each parameter can be described by an instance of <see cref="ParameterRules"/> class.
    /// </summary>
    public class ParameterRules
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterRules"/> class.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter name.
        /// </param>
        /// <param name="minArguments">
        /// The minimum number of arguments fallowing the parameter in the parameter list.
        /// </param>
        /// <param name="maxArguments">
        /// The maximum number of arguments fallowing the parameter in the parameter list.
        /// </param>
        public ParameterRules(string parameterName, int minArguments, int maxArguments)
        {
            this.ParameterName = parameterName;
            this.MinArguments = minArguments;
            this.MaxArguments = maxArguments;
        }

        /// <summary>
        /// Gets the name of the property for which this rules are defined.
        /// </summary>
        public string ParameterName { get; private set; }

        /// <summary>
        /// Gets the minimum number of arguments fallowing the parameter in the parameter list.
        /// </summary>
        public int MinArguments { get; private set; }

        /// <summary>
        /// Gets the maximum number of arguments fallowing the parameter in the parameter list.
        /// </summary>
        public int MaxArguments { get; private set; }

        /// <summary>
        /// Creates an instance of class <see cref="ParameterRules"/> that corresponds to a required parameter.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter name.
        /// </param>
        /// <param name="argumentsCount">
        /// The number of arguments fallowing the parameter in the parameter list.
        /// </param>
        /// <returns>
        /// An instance of class <see cref="ParameterRules"/>.
        /// </returns>
        public static ParameterRules CreateParameter(string parameterName, int argumentsCount = 0)
        {
            return new ParameterRules(parameterName, argumentsCount, argumentsCount);
        }

        /// <summary>
        /// The create parameter.
        /// </summary>
        /// <param name="parameterName">
        /// The parameter name.
        /// </param>
        /// <param name="minArguments">
        /// The minimum number of arguments fallowing the parameter in the parameter list.
        /// </param>
        /// <param name="maxArguments">
        /// The maximum number of arguments fallowing the parameter in the parameter list.
        /// </param>
        /// <returns>
        /// An instance of class <see cref="ParameterRules"/>.
        /// </returns>
        public static ParameterRules CreateParameter(string parameterName, int minArguments, int maxArguments)
        {
            return new ParameterRules(parameterName, minArguments, maxArguments);
        }
    }
}
