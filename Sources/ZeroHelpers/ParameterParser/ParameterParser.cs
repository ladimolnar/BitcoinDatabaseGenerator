//-----------------------------------------------------------------------
// <copyright file="ParameterParser.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers.ParameterParser
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A class that implements functionality related to parsing a list of parameters.
    /// A typical use is to parse the list of parameters for a command line tool.
    /// </summary>
    /// <typeparam name="T">
    /// A type inherited from ParametersListInfo. 
    /// The result of processing the list of arguments will be an instance of this type.
    /// </typeparam>
    public class ParameterParser<T> where T : ParametersListInfo, new()
    {
        /// <summary>
        /// Specifies the rules that guide the parsing of the given list of parameters.
        /// </summary>
        private readonly ParametersListRules parametersListRules;

        /// <summary>
        /// The list of arguments that must be parsed.
        /// </summary>
        private string[] parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterParser{T}"/> class.
        /// </summary>
        /// <param name="parametersListRules">
        /// Specifies the rules that guide the parsing of the given list of parameters.
        /// </param>
        public ParameterParser(ParametersListRules parametersListRules)
        {
            this.parametersListRules = parametersListRules;
        }

        /// <summary>
        /// Parses the given parameters according to the parsing rules that were provided in the constructor.
        /// </summary>
        /// <param name="args">
        /// The arguments that must be parsed.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="ParametersListInfo"/> containing information that resulted from parsing the given list of parameters.
        /// </returns>
        public T ParseParameters(string[] args)
        {
            try
            {
                return this.InternalParseParameters(args);
            }
            catch (InvalidParameterException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidParameterException("An error occurred while parsing parameters.", ex);
            }
        }

        /// <summary>
        /// Parses the given parameters according to the parsing rules that were provided in the constructor.
        /// </summary>
        /// <param name="args">
        /// The arguments that must be parsed.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="ParametersListInfo"/> containing information that resulted from parsing the given list of parameters.
        /// </returns>
        public T InternalParseParameters(string[] args)
        {
            ParametersListInfo parametersListInfo = new T();

            if (args == null)
            {
                return (T)parametersListInfo;
            }

            this.parameters = args;

            for (int i = 0; i < args.Length; i++)
            {
                string argument = args[0];
                if (argument[0] == '/')
                {
                    ParameterInfo parameterInfo = this.ParseParameter(i);
                    parametersListInfo.Add(parameterInfo);
                    i += parameterInfo.Arguments.Count;
                }
            }

            if (parametersListInfo.IsHelpSpecified)
            {
                if (parametersListInfo.Parameters.Count > 1)
                {
                    throw new InvalidParameterException("The parameter '/?' when present should be the only parameter specified.");
                }
            }
            else
            {
                parametersListInfo.Validate();
            }

            return (T)parametersListInfo;
        }

        /// <summary>
        /// Parses one parameter from the list of parameters.
        /// </summary>
        /// <param name="position">
        /// The position in the list of parameters where the target parameter is placed.
        /// </param>
        /// <returns>
        /// An instance of type ParameterInfo containing information that resulted from parsing the given parameter.
        /// </returns>
        private ParameterInfo ParseParameter(int position)
        {
            string parameterName = this.parameters[position].Substring(1);

            ParameterRules parameterRules;
            if (this.parametersListRules.TryGetParameterRules(parameterName, out parameterRules) == false)
            {
                throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "Invalid parameter: {0}", parameterName));
            }

            ParameterInfo parameterInfo = new ParameterInfo(parameterName);

            for (int i = position + 1; i < this.parameters.Length; i++)
            {
                if (this.parameters[i][0] == '/')
                {
                    break;
                }

                if (parameterInfo.Arguments.Count >= parameterRules.MaxArguments)
                {
                    throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "Too many arguments following parameter {0}", parameterName));
                }

                parameterInfo.AddArgument(this.parameters[i]);
            }

            if (parameterInfo.Arguments.Count < parameterRules.MinArguments)
            {
                throw new InvalidParameterException(string.Format(CultureInfo.InvariantCulture, "Not enough arguments following parameter {0}", parameterName));
            }

            return parameterInfo;
        }
    }
}
