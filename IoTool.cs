using System;
using System.Collections.Generic;
using System.IO;

namespace Xanotech.Tools {
    public static class IoTool {

        public static int CompareByContent(string file1, string file2) {
            // if file1 comes before file2, return < 0
            // if file1 comes after file2, return > 0
            if (file1 == file2) return 0;
            if (file1 != null && file2 == null) return -1;
            if (file1 == null && file2 != null) return 1;

            var file1Exists = File.Exists(file1);
            var file2Exists = File.Exists(file2);
            if (file1Exists && !file2Exists) return -1;
            if (!file1Exists && file2Exists) return 1;

            var fs1 = new FileStream(file1, FileMode.Open);
            var fs2 = new FileStream(file2, FileMode.Open);

            var result = 1;
            if (fs1.Length == fs2.Length) {
                // Read and compare a byte from each file until either a
                // non-matching set of bytes is found or until the end of
                // file1 is reached.
                var byte1 = 0;
                var byte2 = 0;
                while ((byte1 == byte2) && (byte1 != -1)) {
                    byte1 = fs1.ReadByte();
                    byte2 = fs2.ReadByte();
                } // End if
                if (byte1 == -1)
                    result = 0;
                else if (byte1 < byte2)
                    result *= -1;
            } else
                if (fs1.Length < fs2.Length)
                    result *= -1;
            fs1.Close();
            fs2.Close();

            return result;
        } // end method



        public static bool Contains(string filename, IEnumerable<string> searchStrings) {
            if (!File.Exists(filename))
                return false;

            var maxLength = 0;
            foreach (var searchString in searchStrings)
                maxLength = Math.Max(maxLength, searchString.Length);
            maxLength = Math.Max(maxLength * 2, 2048);

            var buffer = new char[maxLength];
            for (var b = 0; b < buffer.Length; b++)
                buffer[b] = ' ';

            var found = false;
            var reader = File.OpenText(filename);
            while (!found && !reader.EndOfStream) {
                var offset = buffer.Length / 2;
                for (var b = 0; b < offset; b++)
                    buffer[b] = buffer[b + offset];
                var lastIndex = reader.Read(buffer, offset, offset) + offset;
                for (var b = lastIndex; b < buffer.Length; b++)
                    buffer[b] = ' ';

                var bufferString = new string(buffer);
                foreach (var searchString in searchStrings)
                    if (bufferString.Contains(searchString))
                        found = true;
            } // end while
            reader.Close();

            return found;
        } // end method

    } // end class
} // nd namespace