using System;
using System.Collections.Generic;
using System.Linq;

namespace Xanotech.Tools {
    public static class SystemTool {

        private static string FormatDouble(double dbl) {
            string result = dbl.ToString();
            int decimalPlaces = 16;
            if (result.Contains("E-"))
                decimalPlaces = int.Parse(result.Split(new[] {"E-"}, StringSplitOptions.None)[1]);
            string decimalFormat = new string('#', decimalPlaces);
            result = string.Format("{0:0." + decimalFormat + "}", dbl);
            return result;
        } // end method



        public static bool Is(this string str, string value) {
            return str.Equals(value, StringComparison.OrdinalIgnoreCase);
        } // end method



        public static bool IsBasic(this Type type) {
            if (type.IsPrimitive)
                return true;

            if (typeof(string).IsAssignableFrom(type) ||
                typeof(DateTime).IsAssignableFrom(type) ||
                typeof(Decimal).IsAssignableFrom(type))
                return true;

            if (type.Name == "Nullable`1") {
                Type underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType.IsPrimitive ||
                    typeof(DateTime).IsAssignableFrom(underlyingType) ||
                    typeof(Decimal).IsAssignableFrom(underlyingType))
                    return true;
            } // end if

            return false;
        } // end method



        public static bool IsIEnumerable(this Type type) {
            return type.GetInterfaces().Any(i => i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        } // end method



        public static long NextLong(this Random rand) {
            var int1 = BitConverter.GetBytes(rand.Next());
            var int2 = BitConverter.GetBytes(rand.Next());
            var longBytes = new byte[int1.Length + int2.Length];
            int1.CopyTo(longBytes, 0);
            int2.CopyTo(longBytes, int1.Length);
            var value = BitConverter.ToInt64(longBytes, 0);
            if (value < 0) {
                value += 1;
                value *= -1;
            } // end if
            return value;
        } // end method



        public static double? RoundToSignificantDigits(this double? d, int digits) {
            if (d == null || d == 0)
                return d;

            bool wasNegative = d < 0;
            d = Math.Abs(d.Value);
            double scale = Math.Pow(10, Math.Floor(Math.Log10(d.Value)) + 1);
            d = scale * Math.Round(d.Value / scale, digits);
            if (wasNegative)
                d *= -1;
            return d;
        } // end method



        public static string ToBasicString(this object obj) {
            if (obj == null)
                return null;

            string result = null;
            if (obj is DateTime?)
                result = string.Format("{0:G}", obj);
            else if (obj is decimal)
                result = string.Format("{0:0.############################}", obj);
            else if (obj is double || obj is float)
                result = FormatDouble((double)obj);
            else
                result = obj.ToString();

            return result;
        } // end method



        public static string ToSqlString(this object obj) {
            if (obj == null)
                return "NULL";

            // String Processing: strings, dates, and characters are all formatted as 'value'.
            // First, try to cast value as a string.  If it doesn't cast, attempt to cast
            // value as a DateTime and then a char in each case then defining s as the
            // string equivalent.  In the end, if the value is a string, DateTime, or char,
            // format it as the appropriate string literal for SQL.
            string s = obj as string;
            if (s == null) {
                var dt = obj as DateTime?;
                if (dt != null)
                    s = dt.Value.ToString("yyyy-MM-dd HH:mm:ss");
            } // end if
            if (s == null) {
                var c = obj as char?;
                if (c != null)
                    s = new string(new[] {c.Value});
            } // end if
            if (s != null)
                return "'" + s.Replace("'", "''") + "'";

            bool? b = obj as bool?;
            if (b != null)
                return b.Value ? "1" : "0";

            return obj.ToString();
        } // end method

    } // end class
} // end namespace