//-----------------------------------------------------------------------
// <copyright file="ValidationDataSetInfo.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System.Data;
    using System.Data.SqlClient;

    public class ValidationDataSetInfo<T> where T : DataSet, new()
    {
        public ValidationDataSetInfo(T dataSet, string sqlStatement, params SqlParameter[] sqlParameters) 
        {
            this.SqlStatement = sqlStatement;
            this.SqlParameters = sqlParameters;
            this.DataSet = dataSet;
        }

        public T DataSet { get; private set; }

        public string SqlStatement { get; private set; }

        public SqlParameter[] SqlParameters { get; private set; }
    }
}
