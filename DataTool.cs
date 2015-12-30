using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Reflection;

namespace XTools {
    using IRecord = IDictionary<string, object>;
    using Record = Dictionary<string, object>;

    /// <summary>
    ///   This class contains methods useful for simplifying database interaction.
    /// </summary>
    public static class DataTool {

        private static IDictionary<Type, DbType> dbTypeMap = CreateDbTypeMap();
        private static IDictionary<int, OleDbType> oleDbTypeMap = CreateOleDbTypeMap();
        private static IDictionary<DbType, Type> typeMap = CreateTypeMap();
        private static Cache<string, string> parameterFormatMap = new Cache<string, string>();



        public static IDbDataParameter AddParameter(this IDbCommand cmd, string name, object value, DataRow schemaRow = null) {
            var parameter = AttemptAddWithValue(cmd.Parameters, name, value ?? DBNull.Value);
            if (parameter == null) {
                parameter = cmd.CreateParameter();
                parameter.ParameterName = name;
                parameter.Set(value);
                cmd.Parameters.Add(parameter);
            } // end if

            if (schemaRow != null) {
                var mirror = Mirror.mirrorCache[parameter.GetType()];
                var dataType = schemaRow.GetValue<Type>("DataType");
                if (dataType != null) {
                    // The following is a hack of sorts.  SQLite (unlike every
                    // other database) does not have explicit data types for
                    // its columns.  Thus, the "DataType" in the schema table
                    // is always "string".  Therefore, instead of setting the
                    // parameter's type to match that of the schema and
                    // converting, we just let the type be whatever it was
                    // set to initially in the first parameter.Set call.
                    // Unfortunately, the only way apply this behavior is to
                    // look at the parameter's type name to see if it's Sqlite.
                    // Its hackish, but because this logic always applies,
                    // it belongs here.
                    if (!parameter.GetType().Name.Is("SQLiteParameter")) {
                        parameter.DbType = dbTypeMap[dataType];
                        parameter.Set(value, true);
                    } // end if

                    if (dataType.Name == "Decimal") {
                        SetParameterProperty(parameter, "Precision", schemaRow["NumericPrecision"]);
                        SetParameterProperty(parameter, "Scale", schemaRow["NumericScale"]);
                    } // end if

                    object size = 0;
                    if (dataType.Name == "String")
                        size = schemaRow["ColumnSize"];
                    SetParameterProperty(parameter, "Size", size);
                } // end if

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
            var mirror = Mirror.mirrorCache[parameters.GetType()];
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
            map[typeof(char)] = DbType.String;
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
            map[typeof(char?)] = DbType.String;
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



        private static IDictionary<DbType, Type> CreateTypeMap() {
            var map = new Dictionary<DbType, Type>();
            map[DbType.AnsiString] = typeof(string);
            map[DbType.AnsiStringFixedLength] = typeof(string);
            map[DbType.Currency] = typeof(decimal);
            map[DbType.String] = typeof(string);
            map[DbType.Byte] = typeof(byte);
            map[DbType.SByte] = typeof(sbyte);
            map[DbType.Int16] = typeof(short);
            map[DbType.UInt16] = typeof(ushort);
            map[DbType.Int32] = typeof(int);
            map[DbType.UInt32] = typeof(uint);
            map[DbType.Int64] = typeof(long);
            map[DbType.UInt64] = typeof(ulong);
            map[DbType.Single] = typeof(float);
            map[DbType.Double] = typeof(double);
            map[DbType.Decimal] = typeof(decimal);
            map[DbType.Boolean] = typeof(bool);
            map[DbType.StringFixedLength] = typeof(string);
            map[DbType.Guid] = typeof(Guid);
            map[DbType.Date] = typeof(DateTime);
            map[DbType.DateTime] = typeof(DateTime);
            map[DbType.DateTime2] = typeof(DateTime);
            map[DbType.DateTimeOffset] = typeof(DateTimeOffset);
            map[DbType.Binary] = typeof(byte[]);
            map[DbType.VarNumeric] = typeof(double);
            map[DbType.Xml] = typeof(string);
            return map;
        } // end method



        public static IEnumerable<IRecord> ExecuteReader(this IDbConnection con,
            string commandText) {
            using (IDbCommand cmd = con.CreateCommand()) {
                cmd.CommandText = commandText;
                using (IDataReader reader = cmd.ExecuteReader())
                    return reader.ReadData();
            } // end using
        } // end method



        private static string FindParameterFormat(IDbConnection con) {
            var defaultFormat = "@{0}";

            // I hate this kludge, but Informix does not support GetSchema and
            // it doesn't use "@ParameterName" like every other database, so...
            if (con.GetType().FullName == "IBM.Data.Informix.IfxConnection")
                defaultFormat = "?";
            
            var mirror = Mirror.mirrorCache[con.GetType()];
            var getSchema = mirror.GetMethod("GetSchema", new[] {typeof(string)});
            if (getSchema == null)
                return defaultFormat;

            DataTable dataSourceInfo = null;
            try {
                dataSourceInfo = getSchema.Invoke(con, new[] {"DataSourceInformation"}) as DataTable;
            } catch {
                // Do nothing: the null check after the try-catch will deal with it.
            } // end try catch
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



        public static IEnumerable<IRecord> ReadData(this IDataReader reader) {
            var data = new List<IRecord>();
            while (reader.Read()) {
                var row = new Record(StringComparer.OrdinalIgnoreCase);
                for (int fc = 0; fc < reader.FieldCount; fc++) {
                    string name = reader.GetName(fc);
                    object value = reader.GetValue(fc);
                    if (value is DBNull)
                        value = null;
                    row[name] = value;
                } // end for
                data.Add(row);
            } // end while
            return data;
        } // end method



        public static DataTable ReadDataTable(this IDbConnection con,
            string commandText) {
            var assembly = con.GetType().Assembly;
            var adapterName = con.GetType().FullName;
            adapterName = adapterName.Substring(0, adapterName.Length - 10) + "DataAdapter";
            var adapterType = assembly.GetType(adapterName);

            var dataTable = new DataTable();
            using (IDbCommand cmd = con.CreateCommand())
            using (var adapter = Activator.CreateInstance(adapterType, new[] {cmd}) as DbDataAdapter) {
                cmd.CommandText = commandText;
                adapter.Fill(dataTable);
            } // end using
            return dataTable;
        } // end method



        public static object Set(this IDbDataParameter parameter, object value, bool convert = false) {
            value = value ?? DBNull.Value;
            if (convert) {
                if (value != DBNull.Value)
                    value = SystemTool.SmartConvert(value, typeMap[parameter.DbType]);
                parameter.Value = value;
            } else {
                parameter.Value = value;
                parameter.DbType = dbTypeMap[value.GetType()];
            } // end if-else
            return value;
        } // end method



        private static void SetParameterProperty(object parameter, string propertyName, object value) {
            var mirror = Mirror.mirrorCache[parameter.GetType()];
            var prop = mirror.GetProperty(propertyName);
            if (prop == null)
                return;

            value = SystemTool.SmartConvert(value, prop.PropertyType);
            prop.SetValue(parameter, value, null);
        } // end method

    } // end class
} // end namespace