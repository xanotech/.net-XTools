using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Reflection;

namespace Xanotech.Tools {

    /// <summary>
    /// This class contains methods useful for simplifying database
    /// interaction.
    /// </summary>
    public static class DataTool {

        private static IDictionary<Type, DbType> dbTypeMap = CreateDbTypeMap();
        private static Cache<Type, Mirror> mirrorCache = new Cache<Type, Mirror>(t => new Mirror(t));
        private static IDictionary<int, OleDbType> oleDbTypeMap = CreateOleDbTypeMap();
        private static Cache<string, string> parameterFormatMap = new Cache<string, string>();



        public static IDbDataParameter AddParameter(this IDbCommand cmd, string name, object value, DataRow schemaRow = null) {
            var parameter = AttemptAddWithValue(cmd.Parameters, name, value ?? DBNull.Value);
            if (parameter == null) {
                parameter = cmd.CreateParameter();
                parameter.ParameterName = name;
                value = parameter.Set(value);
                parameter.DbType = dbTypeMap[value.GetType()];
                cmd.Parameters.Add(parameter);
            } // end if

            if (schemaRow != null) {
                var mirror = mirrorCache[parameter.GetType()];
                var prop = mirror.GetProperty("SqlDbType");
                if (prop != null) {
                    var dataTypeName = schemaRow.GetValue<string>("DataTypeName");
                    if (dataTypeName != null) {
                        var sqlDbType = Enum.Parse(typeof(SqlDbType), dataTypeName, true);
                        prop.SetValue(parameter, sqlDbType, null);
                    } // end if
                } // end if

                prop = mirror.GetProperty("OleDbType");
                if (prop != null) {
                    var providerType = schemaRow.GetValue<int?>("ProviderType");
                    if (providerType != null) {
                        var oleDbType = oleDbTypeMap[providerType.Value];
                        prop.SetValue(parameter, oleDbType, null);
                    } // end if
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



        public static long? AsLong(object obj) {
            if (obj == null || obj == DBNull.Value)
                return null;
            return Convert.ToInt64(obj);
        } // end method



        private static IDbDataParameter AttemptAddWithValue(IDataParameterCollection parameters,
            string name, object value) {
            var mirror = mirrorCache[parameters.GetType()];
            var addWithValue = mirror.GetMethod("AddWithValue", new[] {typeof(string), typeof(object)});
            if (addWithValue == null)
                return null;

            var parameter = addWithValue.Invoke(parameters, new[] {name, value});
            return parameter as IDbDataParameter;
        } // end method



        private static IDictionary<Type, DbType> CreateDbTypeMap() {
            var map = new Dictionary<Type, DbType>();
            map[typeof(DBNull)] = DbType.String;
            map[typeof(string)] = DbType.String;
            map[typeof(byte)] = DbType.Byte;
            map[typeof(sbyte)] = DbType.SByte;
            map[typeof(short)] = DbType.Int16;
            map[typeof(ushort)] = DbType.UInt16;
            map[typeof(int)] = DbType.Int32;
            map[typeof(uint)] = DbType.UInt32;
            map[typeof(long)] = DbType.Int64;
            map[typeof(ulong)] = DbType.UInt64;
            map[typeof(float)] = DbType.Single;
            map[typeof(double)] = DbType.Double;
            map[typeof(decimal)] = DbType.Decimal;
            map[typeof(bool)] = DbType.Boolean;
            map[typeof(char)] = DbType.StringFixedLength;
            map[typeof(Guid)] = DbType.Guid;
            map[typeof(DateTime)] = DbType.DateTime;
            map[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            map[typeof(byte[])] = DbType.Binary;
            map[typeof(byte?)] = DbType.Byte;
            map[typeof(sbyte?)] = DbType.SByte;
            map[typeof(short?)] = DbType.Int16;
            map[typeof(ushort?)] = DbType.UInt16;
            map[typeof(int?)] = DbType.Int32;
            map[typeof(uint?)] = DbType.UInt32;
            map[typeof(long?)] = DbType.Int64;
            map[typeof(ulong?)] = DbType.UInt64;
            map[typeof(float?)] = DbType.Single;
            map[typeof(double?)] = DbType.Double;
            map[typeof(decimal?)] = DbType.Decimal;
            map[typeof(bool?)] = DbType.Boolean;
            map[typeof(char?)] = DbType.StringFixedLength;
            map[typeof(Guid?)] = DbType.Guid;
            map[typeof(DateTime?)] = DbType.DateTime;
            map[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
            //map[typeof(System.Data.Linq.Binary)] = DbType.Binary;
            return map;
        } // end method



        private static IDictionary<int, OleDbType> CreateOleDbTypeMap() {
            var map = new Dictionary<int, OleDbType>();
            var fields = typeof(OleDbType).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields) {
                var oleDbType = (OleDbType)field.GetValue(null);
                map[oleDbType.GetHashCode()] = oleDbType;
            } // end foreach
            return map;
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



        private static string FindParameterFormat(IDbConnection con) {
            var defaultFormat = "@{0}";
            var mirror = mirrorCache[con.GetType()];
            var getSchema = mirror.GetMethod("GetSchema", new[] {typeof(string)});
            if (getSchema == null)
                return defaultFormat;
            var dataSourceInfo = getSchema.Invoke(con, new[] {"DataSourceInformation"}) as DataTable;
            if (dataSourceInfo == null)
                return defaultFormat;

            var format = dataSourceInfo.Rows[0].GetValue<string>("ParameterMarkerFormat") ?? defaultFormat;
            // Some clients (*cough* SqlClient *cough*) return "{0}",
            // which is invalid.  If this happens, just use defaultFormat;
            if (format == "{0}")
                format = defaultFormat;
            return format;
        } // end method



        public static string FormatParameter(this IDbCommand cmd, string name) {
            if (cmd == null || cmd.Connection == null || cmd.Connection.ConnectionString == null)
                return "@" + name;

            var format = parameterFormatMap.GetValue(cmd.Connection.ConnectionString,
                () => { return FindParameterFormat(cmd.Connection); });
            return string.Format(format, name);
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



        public static object Set(this IDbDataParameter parameter, object value) {
            try {
                value = value ?? DBNull.Value;
                parameter.Value = value;
            } catch (ArgumentException) {
                // Oracle does not support bools (the jerks) so,
                // if the value is a bool, just 1 or 0 accordingly
                // (if not a bool, throw the original exception).
                var boolVal = value as bool?;
                if (boolVal != null) {
                    value = boolVal.Value ? 1 : 0;
                    parameter.Value = value;
                } else
                    throw;
            } // end try-catch
            return value;
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

    } // end class
} // end namespace