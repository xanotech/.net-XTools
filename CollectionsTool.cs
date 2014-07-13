using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xanotech.Tools {
    public static class CollectionsTool {

        public static void AddRange<T>(this IList<T> ilist, IEnumerable<T> items) {
            List<T> list = ilist as List<T>;
            if (list != null)
                list.AddRange(items);
            else
                foreach (T item in items)
                    ilist.Add(item);
        } // end method



        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> idictionary,
            IDictionary<TKey, TValue> items) {
            foreach (var key in items.Keys)
                idictionary[key] = items[key];
        } // end method



        private static string Decode(string val) {
            if (val == null) return "";

            var splitVal = val.Split(new Char[] {'&'});
            var result = new StringBuilder(splitVal[0]);
            for (int sv = 1; sv < splitVal.Length; sv++) {
                if (splitVal[sv].Length == 0) {
                    result.Append('&');
                    sv++;
                    result.Append(splitVal[sv]);
                } else if (splitVal[sv][0] == 'n')
                    result.Append('\n' + splitVal[sv].Substring(1));
                else if (splitVal[sv][0] == 'r')
                    result.Append('\r' + splitVal[sv].Substring(1));
                else if (splitVal[sv][0] == 't')
                    result.Append('\t' + splitVal[sv].Substring(1));
                else if (splitVal[sv][0] == 'e')
                    result.Append('=' + splitVal[sv].Substring(1));
                else result.Append(splitVal[sv]);
            } // end for

            return result.ToString();
        } // end method



        private static string Encode(string str) {
            if (str == null) return "";

            str = str.Replace("&", "&&");
            str = str.Replace("\n", "&n");
            str = str.Replace("\r", "&r");
            str = str.Replace("\t", "&t");
            str = str.Replace("=", "&e");

            return str;
        } // end method



        private static object FindExtreme(IEnumerable enumerable, ref SystemTool.Comparison comparison, bool max) {
            object extreme = null;

            var isDefaultComparison = comparison == SystemTool.Comparison.Default; // indicates if the original comparsion value is Default
            var isEverDefault = false; // indicates if any comparison is ever Default
            var isEverNumeric = false; // indicates if any comparison is ever Numeric
            var isFirst = true; // flag for processing the first item in enumerable
            var isStringCheckDone = false; // flag for indicating if all the items in enumerable were checked for String comparison

            // Loop through all the items in enumerable.  If comparison is either String
            // or Numeric, just perform the comparison, look at the result and set extreme
            // if necessary.  For Default comparisons, special logic is necessary (see
            // comment for isDefaultComparison if statement below).
            foreach (var item in enumerable) {
                if (isFirst) {
                    isFirst = false;
                    extreme = item;
                    continue;
                } // end if

                // (Non-Default comparisons are easy.  Just perform the comparison.
                // For Default, perform the check and then examine the comparison
                // value afterwards.  If its String, just return FindExtreme passing
                // comparison of String.  If its Default, make note that a Default
                // comparison was used by setting isEverDefault to true;
                // If its numeric, make note that a Numeric comparison was used
                // by setting isEverNumeric to true.  Then, if it hasn't already
                // been done (via isStingCheckDone), compare all the values
                // to the number 0 with a Default comparison to see if any rely
                // on a String comparison.  If they do, just return FindExtreme
                // passing comparison of String.  If no String comparisons occur,
                // then either Default only or Numeric only comparisons are safe.
                int compResult;
                if (isDefaultComparison) {
                    comparison = SystemTool.Comparison.Default;
                    compResult = item.CompareTo(extreme, ref comparison);

                    if (comparison == SystemTool.Comparison.String)
                        return FindExtreme(enumerable, ref comparison, max);

                    if (comparison == SystemTool.Comparison.Default)
                        isEverDefault = true;

                    if (comparison == SystemTool.Comparison.Numeric) {
                        isEverNumeric = true;
                        if (!isStringCheckDone) {
                            foreach (var e in enumerable) {
                                comparison = SystemTool.Comparison.Default;
                                e.CompareTo(0, ref comparison);
                                if (comparison == SystemTool.Comparison.String)
                                    return FindExtreme(enumerable, ref comparison, max);
                            } // end foreach
                            isStringCheckDone = true;
                        } // end if
                    } // end if
                } else
                    compResult = item.CompareTo(extreme, ref comparison);

                if (max && compResult > 0 || !max && compResult < 0)
                    extreme = item;
            } // end foreach

            // At this point, there are no String comparisons.  At this point,
            // if there was ever a Default comparison and also a Numeric comparison,
            // it is necessary to repeat FindExtreme call but this time with
            // an explicit Numeric comparison in case mixed objects that are Comparable
            // (Default comparison) do not compare the same way numerically.
            if (isEverNumeric && isEverDefault) {
                comparison = SystemTool.Comparison.Numeric;
                return FindExtreme(enumerable, ref comparison, max);
            } // end if

            return extreme;
        } // end method



        public static object FindMax(this IEnumerable enumerable) {
            var comp = SystemTool.Comparison.Default;
            return enumerable.FindMax(ref comp);
        } // end method



        public static object FindMax(this IEnumerable enumerable, ref SystemTool.Comparison comparison) {
            return FindExtreme(enumerable, ref comparison, true);
        } // end method



        public static object FindMin(this IEnumerable enumerable) {
            var comp = SystemTool.Comparison.Default;
            return enumerable.FindMin(ref comp);
        } // end method



        public static object FindMin(this IEnumerable enumerable, ref SystemTool.Comparison comparison) {
            return FindExtreme(enumerable, ref comparison, false);
        } // end method



        public static IDictionary<string, string> Read(this IDictionary<string, string> dict, StreamReader reader) {
            if (reader == null)
                throw new ArgumentNullException("reader", "The reader parameter is null.");

            while (reader.Peek() >= 0) {
                var line = reader.ReadLine();
                if (line != null) {
                    int index = line.IndexOf('=');
                    if (index == -1)
                        index = line.Length;

                    var newKey = Decode(line.Substring(0, index));
                    var newValue = "";
                    if (index < line.Length)
                        newValue = Decode(line.Substring(index + 1));
                    dict[newKey] = newValue;
                } // end if
            } // end while

            return dict;
        } // end method



        public static IDictionary<string, string> Read(this IDictionary<string, string> dict, string filename) {
            if (filename == null)
                throw new ArgumentNullException("filename", "The filename parameter is null.");

            using (var reader = new StreamReader(filename))
                dict.Read(reader);

            return dict;
        } // end method



        public static void Sort<T>(this IList<T> ilist) {
            SortIList(ilist, new Action<List<T>>(l => l.Sort()));
        } // end method



        public static void Sort<T>(this IList<T> ilist, Comparison<T> comparison) {
            SortIList(ilist, new Action<List<T>>(l => l.Sort(comparison)));
        } // end method



        public static void Sort<T>(this IList<T> ilist, IComparer<T> comparer) {
            SortIList(ilist, new Action<List<T>>(l => l.Sort(comparer)));
        } // end method



        public static void Sort<T>(this IList<T> ilist,
            int index, int count, IComparer<T> comparer) {
            SortIList(ilist, new Action<List<T>>(l => l.Sort(index, count, comparer)));
        } // end method



        private static void SortIList<T>(IList<T> ilist, Action<List<T>> sortAction) {
            List<T> list;
            if (ilist is List<T>) {
                list = ilist as List<T>;
                sortAction(list);
            } else {
                list = new List<T>();
                list.AddRange(ilist);
                sortAction(list);
                for (var l = 0; l < list.Count; l++)
                    ilist[l] = list[l];
            } // end if-else
        } // end method



        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) {
            TValue value;
            return dictionary.TryRemove(key, out value);
        } // end method



        public static void Write(this IDictionary<string, string> dict, StreamWriter writer) {
            foreach (var entry in dict) {
                string line = Encode(entry.Key) + "=" +
                    Encode(entry.Value) + "\n";
                writer.Write(line);
            } // end foreach
            writer.Flush();
        } // end method



        public static void Write(this IDictionary<string, string> dict, string filename) {
            if (filename == null)
                throw new ArgumentNullException("filename", "The filename parameter is null.");

            using (var writer = new StreamWriter(filename))
                dict.Write(writer);
        } // end method

    } // end class
} // end namespace