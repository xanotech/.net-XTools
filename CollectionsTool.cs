using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Xanotech.Tools {
    public static class CollectionsTool {

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