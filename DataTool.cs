using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Xanotech.Tools {

    /// <summary>
    /// This class contains methods useful for simplifying database
    /// interaction.
    /// </summary>
    public static class DataTool {

        /*
        public static DbType GetDbType(object value) {
            DbType type = DbType.String;
            if (value is bool) type = DbType.SByte;
            else if (value is byte) type = DbType.Byte;
            else if (value is char) type = DbType.String;
            else if (value is DateTime) type = DbType.DateTime;
            else if (value is decimal) type = DbType.Decimal;
            else if (value is double) type = DbType.Double;
            else if (value is sbyte) type = DbType.SByte;
            else if (value is string) type = DbType.String;
            else type = DbType.VarNumeric;
            return type;
        } // end method
        */



        public static IEnumerable<IDictionary<string, object>> ExecuteReader(this IDbConnection con,
            string commandText) {
            using (IDbCommand cmd = con.CreateCommand()) {
                cmd.CommandText = commandText;
                //if (parameters != null) SetParameters(cmd, parameters);
                cmd.Prepare();
                using (IDataReader reader = cmd.ExecuteReader())
                    return reader.ReadData();
            } // end using
        } // end method



        public static T OpenConnection<T>(string connectionString) where T : IDbConnection, new() {
            var con = new T();
            con.ConnectionString = connectionString;
            con.Open();
            return con;
        } // end method



        public static IDbConnection OpenConnection(string connectionStringName) {
            var cs = ConfigurationManager.ConnectionStrings[connectionStringName];

            // If no connection string is found, throw an exception.
            if (cs == null)
                throw new SettingsPropertyNotFoundException("The connection string \"" +
                    connectionStringName + "\" does not exist.");

            var pf = DbProviderFactories.GetFactory(cs.ProviderName);
            var con = pf.CreateConnection();
            con.ConnectionString = cs.ConnectionString;
            con.Open();
            return con;
        } // end method



        public static IEnumerable<IDictionary<string, object>> ReadData(this IDataReader reader) {
            var data = new List<IDictionary<string, object>>();
            while (reader.Read()) {
                var row = new Dictionary<string, object>();
                for (int fc = 0; fc < reader.FieldCount; fc++) {
                    string name = reader.GetName(fc);
                    object value = reader.GetValue(fc);
                    if (value is DBNull) value = null;
                    row[name] = value;
                } // end for
                data.Add(row);
            } // end while
            return data;
        } // end method



        /*
        private static void SetParameters(IDbCommand cmd,
            ICollection parameters) {
            // For IDictionary objects, add named parameters with the
            // assumption that they keys of the parameters collection
            // are the names of the parameters.
            // For other types of collections, assume each element
            // in the collection is a parameter value and just add them
            // without a name in the order they appear in the collection.
            if (parameters is IDictionary) {
                IDictionary parameterDictionary = (IDictionary)parameters;
                foreach (string k in parameterDictionary.Keys) {
                    IDbDataParameter parameter = cmd.CreateParameter();
                    parameter.ParameterName = k;
                    parameter.Value = parameterDictionary[k];
                    parameter.DbType = GetDbType(parameter.Value);
                    cmd.Parameters.Add(parameter);
                } // end foreach
            } else {
                foreach (object p in parameters) {
                    IDbDataParameter parameter = cmd.CreateParameter();
                    parameter.Value = p;
                    parameter.DbType = GetDbType(p);
                    cmd.Parameters.Add(parameter);
                } // end foreach
            } // end if-else
        } // end method
        */

    } // end class
} // end namespace