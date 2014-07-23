using System;
using System.Collections.Generic;
using System.Linq;

namespace Xanotech.Tools {
    public static class SystemTool {

        public enum Comparison {
            Default, Numeric, String
        } // end enum



        public static int CompareTo(this object obj, object someObj) {
            var comp = Comparison.Default;
            return obj.CompareTo(someObj, ref comp);
        } // end method



        public static int CompareTo(this object obj, object someObj, ref Comparison comparison) {
            // null comparison (handles comparison where one or both objects are null)
            if (obj == null && someObj == null)
                return 0;
            if (someObj == null)
                return 1;
            if (obj == null)
                return -1;

            // Both objects are non-null from this point onward.

            // string comparison
            if (comparison == Comparison.String)
                return obj.ToString().CompareTo(someObj.ToString());

            // comparison is either Default or Numeric from this point onward.

            // built-in IComparable.CompareTo comparison (only for Comparison.Default)
            // Only returns a result if obj and someObj are of the same type and are IComparable.
            if (comparison == Comparison.Default) {
                var objType = obj.GetType();
                var someObjType = someObj.GetType();
                var comp = obj as IComparable;
                if (comp != null && someObjType.IsAssignableFrom(objType))
                    return comp.CompareTo(someObj);
            } // end if

            // If both objects are IConvertable, then attempt a numeric comparison.
            // First, try converting and comparing using decimal.  If that fails,
            // try doubles forcing success if comparison is Numeric.  (Forcing
            // success essentially treats non-numeric object values as 0.  "ABC" is
            // the same as "DEF" for the purposes of numeric comparisons.)
            // If either attempt succeeds, change comparison to Numeric (which only
            // has an effect if comparison is Default) and return the result.
            int result;
            if (TryNumericCompareTo(obj, someObj, false, out result, o => Convert.ToDecimal(o)) ||
                TryNumericCompareTo(obj, someObj, comparison == Comparison.Numeric, out result, o => Convert.ToDouble(o))) {
                comparison = Comparison.Numeric;
                return result;
            } // end if

            // ToString comparison
            // comparison can only be Default at this point.  Convert both objects
            // to string and return comparison (setting comparison to String).
            if (comparison == Comparison.Default)
                comparison = Comparison.String;
            return obj.ToString().CompareTo(someObj.ToString());
        } // end method



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

            if (type.IsNullable()) {
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



        public static bool IsNullable(this Type type) {
            return type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>);
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



        public static string Remove(this String str, string substring) {
            return str.Replace(substring, "");
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
            if (obj == null || obj == DBNull.Value)
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



        private static bool TryNumericCompareTo<T>(object a, object b,
            bool forceComparison, out int result, Func<object, T> convert) where T : IComparable {
            result = 0;
            if (a as IConvertible == null || b as IConvertible == null)
                return false;

            T aT, bT;
            try {
                // If either a or b are DateTimes, switch to the value of
                // Ticks for the purposes of numeric comparison
                var aDT = a as DateTime?;
                if (aDT != null)
                    a = aDT.Value.Ticks;
                var bDT = b as DateTime?;
                if (bDT != null)
                    b = bDT.Value.Ticks;

                aT = convert(a);
                bT = convert(b);
                result = aT.CompareTo(bT);
                return true;
            } catch {
                if (forceComparison) {
                    try {
                        aT = convert(a);
                    } catch {
                        aT = convert(0);
                    } // end try-catch

                    try {
                        bT = convert(b);
                    } catch {
                        bT = convert(0);
                    } // end try-catch

                    result = aT.CompareTo(bT);
                    return true;
                } // end if
                return false;
            } // end try-catch
        } // end method

    } // end class
} // end namespace