using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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



        public static IDictionary<string, string> Read(this IDictionary<string, string> dict, StreamReader reader) {
            if (reader == null)
                throw new ArgumentNullException("reader", "The reader parameter is null");

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
                throw new ArgumentNullException("filename", "The filename parameter is null");

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
                throw new ArgumentNullException("filename", "The filename parameter is null");

            using (var writer = new StreamWriter(filename))
                dict.Write(writer);
        } // end method

    } // end class
} // end namespace