using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace XTools {
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



        private static bool IsIgnored(IEnumerable<string> ignores, string item) {
            if (ignores == null || item == null)
                return false;

            item = item.Trim('\\');
            string[] itemSplit = item.Split('\\');
            foreach (string ignore in ignores)
                foreach (string split in itemSplit) {
                    if (ignore.StartsWith("*") && ignore.EndsWith("*") &&
                        split.Contains(ignore.Substring(1, ignore.Length - 2)))
                        return true;
                    else if (ignore.StartsWith("*") && split.EndsWith(ignore.Substring(1)))
                        return true;
                    else if (ignore.EndsWith("*") && split.StartsWith(ignore.Substring(0, ignore.Length - 1)))
                        return true;
                    else if (ignore.Equals(split))
                        return true;
                } // end foreach
            return false;
        } // end method



        private static IDictionary<string, FileSystemInfo> MapInfosByRelativeName(FileSystemInfo baseInfo,
            FileSystemInfo[] childInfos) {
            IDictionary<string, FileSystemInfo> dict = new Dictionary<string, FileSystemInfo>();
            string baseName = baseInfo.FullName;
            foreach (FileSystemInfo info in childInfos)
                if (info.FullName.StartsWith(baseInfo.FullName)) {
                    string key = info.FullName.Substring(baseName.Length);
                    dict[key] = info;
                } // end if

            List<string> sortedKeys = new List<string>(dict.Keys);
            sortedKeys.Sort();

            IDictionary<string, FileSystemInfo> sortedDict = new Dictionary<string, FileSystemInfo>();
            foreach (string key in sortedKeys)
                sortedDict[key] = dict[key];
            return sortedDict;
        } // end method



        public static void Synchronize(this DirectoryInfo source, DirectoryInfo target,
            IEnumerable<string> ignores = null, Action<string> log = null) {
            if (log != null)
                log("Synchronizing " + source.FullName + " to " + target.FullName);

            if (!source.Exists)
                throw new ArgumentException("The source directory does not exist", "source");

            if (!target.Exists)
                target.Create();

            IDictionary<string, FileSystemInfo> sourceItems =
                MapInfosByRelativeName(source, source.GetFileSystemInfos("*", SearchOption.AllDirectories));
            IDictionary<string, FileSystemInfo> targetItems =
                MapInfosByRelativeName(target, target.GetFileSystemInfos("*", SearchOption.AllDirectories));

            IDictionary<FileInfo, FileInfo> copyQueue = new Dictionary<FileInfo, FileInfo>();

            // Delete any targetItems that do not have a matching sourceItem.
            // NOTE: Delete should take place first because of case changes
            // on target items (as in target contains "\bin" and source
            // contains "\Bin").  In this scenario, if the delete is performed
            // after the copy, the new directory would get cleaned up
            // simply because the names do not exactly match (because
            // the casing is different).  By deleting first, we ensure
            // that case changes do not cause inadvertent deletes and
            // that changes in case flow thru to the target.
            foreach (string relativeName in targetItems.Keys)
                if (!sourceItems.ContainsKey(relativeName) &&
                    !IsIgnored(ignores, relativeName)) {
                    string fullName = targetItems[relativeName].FullName;
                    if (Directory.Exists(fullName)) {
                        Directory.Delete(fullName, true);
                        if (log != null)
                            log("deleted " + fullName);
                    } else if (File.Exists(fullName)) {
                        File.Delete(fullName);
                        if (log != null)
                            log("deleted " + fullName);
                    } // end if-else
                } // end if

            // Process all the items in each sourceItems key as compared to targetItems keys.
            // If the source is a directory, see if a corresponding target exists.  If so,
            // and it's a file, delete it and replace it with a new directory.  If not,
            // create a new directory. If the source is a file, see if a corresponding target
            // exists.  If so, and it's a directory, delete the directory.  If not or if the
            // corresponding file differs in size or date, copy the source to the target.
            foreach (string relativeName in sourceItems.Keys) {
                // Check the name of the source item and ignore it if necessary
                if (IsIgnored(ignores, relativeName))
                    continue;

                // Source is a directory
                if (sourceItems[relativeName] is DirectoryInfo) {
                    // Target is a file
                    if (targetItems.ContainsKey(relativeName) &&
                        targetItems[relativeName] is FileInfo) {
                        targetItems[relativeName].Delete();
                        if (log != null)
                            log("deleted " + targetItems[relativeName].FullName);
                        targetItems.Remove(relativeName);
                    } // end if

                    // Target does not exist (note: if it was a file and did exist,
                    // the previous if-block removed it from targetItems so at this
                    // point, it would no longer exist.
                    if (!targetItems.ContainsKey(relativeName)) {
                        Directory.CreateDirectory(target.FullName + relativeName);
                        if (log != null)
                            log("created " + target.FullName + relativeName);
                    } // end if
                } else {
                    // Target is a directory
                    if (targetItems.ContainsKey(relativeName) &&
                        targetItems[relativeName] is DirectoryInfo) {
                        targetItems[relativeName].Delete();
                        if (log != null)
                            log("deleted " + targetItems[relativeName].FullName);
                        targetItems.Remove(relativeName);
                    } // end if

                    // Check to see if the target either does not exist or does not match.
                    // If so, add it to the queue to be copied.
                    FileInfo sourceFile = sourceItems[relativeName] as FileInfo;
                    FileInfo targetFile = targetItems.ContainsKey(relativeName) ?
                        targetItems[relativeName] as FileInfo :
                        new FileInfo(target.FullName + relativeName);
                    if (!targetFile.Exists ||
                        sourceFile.Length != targetFile.Length ||
                        sourceFile.LastWriteTimeUtc != targetFile.LastWriteTimeUtc)
                        copyQueue[sourceFile] = targetFile;
                } // end if-else
            } // end foreach

            // Run through all copy operations in a parallel block
            Parallel.ForEach(copyQueue.Keys, sourceFile => {
                FileInfo targetFile = copyQueue[sourceFile];
                if (targetFile.Exists && targetFile.IsReadOnly)
                    targetFile.IsReadOnly = false;
                sourceFile.CopyTo(targetFile.FullName, true);
                targetFile.Attributes = FileAttributes.Normal;
                //targetFile.LastWriteTimeUtc = sourceFile.LastWriteTimeUtc;
                            
                if (log != null)
                    lock (log)
                        log("copied " + sourceFile.Name + " to " + targetFile.FullName);
            });

            if (log != null)
                log("Synchronization complete");
        } // end method

    } // end class
} // end namespace