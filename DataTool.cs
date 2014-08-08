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

        private static Cache<Type, Mirror> mirrorCache = new Cache<Type, Mirror>(t => new Mirror(t));

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



        public static DbParameter AddParameter(this IDbCommand cmd, string name, object value, DataRow schemaRow = null) {
            var mirror = mirrorCache[cmd.Parameters.GetType()];
            var addMethod = mirror.GetMethod("AddWithValue", new[] {typeof(string), typeof(object)});
            var parameter = addMethod.Invoke(cmd.Parameters, new[] {name, value ?? DBNull.Value}) as DbParameter;

            if (schemaRow != null) {
                mirror = mirrorCache[parameter.GetType()];
                var prop = mirror.GetProperty("SqlDbType");
                var dataTypeName = schemaRow.GetValue<string>("DataTypeName");
                if (prop != null && dataTypeName != null) {
                    var sqlDbType = Enum.Parse(typeof(SqlDbType), dataTypeName, true);
                    prop.SetValue(parameter, sqlDbType, null);
                } // end if

                var dataType = schemaRow.GetValue<Type>("DataType");
                if (dataType.Name == "Decimal") {
                    SetParameterProperty(parameter, "Precision", schemaRow["NumericPrecision"]);
                    SetParameterProperty(parameter, "Scale", schemaRow["NumericScale"]);
                } // end if

                object size = 0;
                if (dataType.Name == "String")
                    size = schemaRow["ColumnSize"];
                SetParameterProperty(parameter, "Size", size);
            } // end if
            return parameter;
        } // end method



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



        public static T GetValue<T>(this DataRow row, string columnName) {
            T value = default(T);
            if (row.Table == null)
                try {
                    return (T)row[columnName];
                } catch (ArgumentException) {
                    // Do nothing: the exception occurred because the columnName
                    // doesn't exist.  Just leave value as default(T).
                } // end try-catch
            else if (row.Table.Columns.Contains(columnName))
                value = (T)row[columnName];
            return value;
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
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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



        private static void SetParameterProperty(object parameter, string propertyName, object value) {
            var mirror = mirrorCache[parameter.GetType()];
            var prop = mirror.GetProperty(propertyName);
            if (prop == null)
                return;

            mirror = mirrorCache[typeof(Convert)];
            var convert = mirror.GetMethod("To" + prop.PropertyType.Name, new[] {typeof(object)});
            value = convert.Invoke(null, new[] {value});
            prop.SetValue(parameter, value, null);
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