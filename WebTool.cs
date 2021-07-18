using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XTools {

    /// <summary>
    ///   This class contains methods useful for simplifying web processing.
    /// </summary>
    public static class WebTool {

        // Used for locking in GetReferences and SaveReferences
        private static object referencesLock = new Object();



        public static string AutoScript(string path) {
            var references = GetReferences();
            var sources = GetHrefs(path, "*.js");
            var response = new StringBuilder();
            foreach (var source in sources) {
                response.AppendLine("<script src=\"" + source +
                    "\"></script>");

                var reference = source;
                var index = reference.LastIndexOf("?");
                if (index >= 0)
                    reference = reference.Substring(0, index);
                if (references != null && !references.Contains(reference))
                    references.Add(reference);
            } // end foreach
            SaveReferences(root, references);
            return response.ToString();
        } // end method



        public static string AutoStyle(string path) {
            var sources = GetHrefs(path, "*.css");
            var response = new StringBuilder();
            foreach (var source in sources)
                response.AppendLine("<link href=\"" + source +
                    "\" rel=\"stylesheet\">");
            return response.ToString();
        } // end method



        public static IEnumerable<Control> GetAllControls(this Control parent) {
            var allControls = new List<Control>();
            foreach (Control c in parent.Controls) {
                allControls.Add(c);
                allControls.AddRange(c.GetAllControls());
            } // end foreach
            return allControls;
        } // end method



        /*
        private static IEnumerable<Assembly> GetBinAssemblies(this IHttpController controller) {
            var assemblies = new List<Assembly>();
            string path = Assembly.GetExecutingAssembly().Location;
            if (File.Exists(path))
                path = Path.GetDirectoryName(path);

            foreach (string dllFilename in Directory.GetFiles(path, "*.dll"))
                assemblies.Add(Assembly.LoadFile(dllFilename));
            return assemblies;
        } // end method
        */
        
        
        
        public static int GetColumnIndex(this GridView gridView, string columnName) {
            DataControlFieldCollection columns = gridView.Columns;
            int index = -1;
            for (int c = 0; c < columns.Count && index == -1; c++)
                if (columns[c].HeaderText == columnName)
                    index = c;
            return index;
        } // end method



        public static int GetColumnIndex(this Table table, string columnName) {
            TableRowCollection rows = table.Rows;
            int index = -1;
            if (rows.Count > 0) {
                TableCellCollection cells = rows[0].Cells;
                for (int c = 0; c < cells.Count && index == -1; c++)
                    if (cells[c].Text == columnName)
                        index = c;
            } // end if
            return index;
        } // end method



        public static string GetColumnName(this GridView gridView, int index) {
            string columnName = null;
            if (index > -1 && index < gridView.Columns.Count)
                columnName = gridView.Columns[index].HeaderText;
            return columnName;
        } // end method



        public static string GetColumnName(this Table table, int index) {
            TableRowCollection rows = table.Rows;
            string columnName = null;
            if (rows.Count > 0) {
                TableCellCollection cells = rows[0].Cells;
                if (index > -1 && index < cells.Count)
                    columnName = cells[index].Text;                    
            } // end if
            return columnName;
        } // end method



        public static string GetCookie(this HttpRequest req, string name) {
            return req.GetCookie(name, "");    
        } // end method



        public static string GetCookie(this HttpRequest req, string name, string defaultValue) {
            string cookieValue = defaultValue;
            try {
                HttpCookie cookie = req.Cookies[name];
                cookieValue = cookie.Value;
            } catch {
                // Do nothying since cookieValue was already set to default value.
            } // end try-catch
            return cookieValue;
        } // end method



        public static IEnumerable<string> GetHrefs(string path, string searchPattern = "*.*", bool addTimestamp = true) {
            var localPath = HttpContext.Current.Server.MapPath(path);
            var files = Directory.EnumerateFiles(localPath, searchPattern, SearchOption.AllDirectories);

            var hrefs = new List<string>();
            var ignored = ReadAllLinesSafely(localPath, "_ignore.txt");
            var ordered = ReadAllLinesSafely(localPath, "_order.txt");
            var root = GetRoot();
            addTimestamp = addTimestamp && !(HttpContext.Current.Request.IsLocal ||
                HttpContext.Current.Request.QueryString.ToString().ToLower().Contains("notimestamp"));

            // The following translates files to their href value replacing blackslashes with front slashes,
            // striping off leading slashes, adding timestamp (if addTimestamp is true), ignoring if IsIgnored,
            // and then ordering them by GetOrder, directory depth (lower first), and then name by name.
            return files.Select(f => {
                var href = f.Substring(root.Length).Replace('\\', '/');
                while (href.StartsWith("/"))
                    href = href.Substring(1);
                if (addTimestamp)
                    href += "?" + File.GetLastWriteTime(f).Ticks / 10000000;
                return href;
            }).Where(x => !IsIgnored(ignored, x))
            .OrderBy(x => GetOrder(ordered, x))
            .ThenBy(x => -x.Split('/').Length)
            .ThenBy(x => x);
        } // end method



        private static int GetOrder(IEnumerable<string> ordered, string file) {
            int order = 0;
            foreach (var ord in ordered) {
                if (ord == file ||
                    (ord.StartsWith("*") && file.EndsWith(ord.Substring(1))) ||
                    (ord.EndsWith("*") && file.StartsWith(ord.Substring(0, ord.Length - 1))))
                    return order;
                order++;
            } // end foreach
            return order;
        } // end method



        public static string GetPageName(this HttpRequest req) {
            string[] splitPath = req.Path.Split(new char[] {'/'});
            return splitPath[splitPath.Length - 1];
        } // end method



        /// <summary>
        ///   Finds the first parent control that is the type specified.
        /// </summary>
        public static Control GetParent(this Control control, Type parentType) {
            Control parent = control.Parent;
            while (parent != null && !parent.GetType().Equals(parentType))
                parent = parent.Parent;
            return parent;
        } // end method



        private static IList<string> GetReferences() {
            var refFile = Path.Combine(GetRoot(), "_references.js");
            string[] lines = null;
            if (!File.Exists(refFile))
                return null;
            
            lock (referencesLock)
                lines = File.ReadAllLines(refFile);

            var references = new List<string>();
            foreach (var line in lines) {
                var reference = line.Trim();
                if (!reference.StartsWith("/// <reference path=\""))
                    continue;

                reference = reference.Substring(21);
                while (reference.EndsWith(" />"))
                    reference = reference.Substring(0, reference.Length - 3) + "/>";
                reference = reference.Substring(0, reference.Length - 3);
                references.Add(reference);
            } // end foreach
            return references;
        } // end method



        private static string root;
        public static string GetRoot() {
            return (root = root ?? HttpContext.Current.Server.MapPath("~"));
        } // end if



        public static TableCell GetTableCell(this TableRow row, string columnName) {
            Control parent = row.Parent;
            bool loop = true;
            while (loop) {
                if (parent == null ||
                    parent is Table)
                    loop = false;
                else
                    parent = parent.Parent;
            } // end while

            int index = -1;
            if (parent is Table)
                index = GetColumnIndex((Table)parent, columnName);

            TableCell cell = null;
            if (index > -1)
                cell = row.Cells[index];
            return cell;
        } // end method



        private static bool IsIgnored(IEnumerable<string> ignore, string src) {
            foreach (var ig in ignore)
                if (ig == src ||
                    (ig.StartsWith("*") && src.EndsWith(ig.Substring(1))) ||
                    (ig.EndsWith("*") && src.StartsWith(ig.Substring(0, ig.Length - 1))))
                    return true;
            return false;
        } // end method



        private static IEnumerable<string> ReadAllLinesSafely(string localPath, string filename) {
            var lines = new string[0];
            filename = Path.Combine(localPath, filename);
            if (File.Exists(filename))
                lines = File.ReadAllLines(filename);
            return lines;
        } // end method



        private static void SaveReferences(string rootPath, IList<string> references) {
            if (references == null || !Debugger.IsAttached)
                return;

            for (var r = 0; r < references.Count; r++)
                references[r] = "/// <reference path=\"" + references[r] + "\"/>";
            references.Sort();
            var refFile = Path.Combine(rootPath, "_references.js");
            references.Insert(0, "// This file is auto-generated by XTools.WebTool.AutoScript.");
            
            lock (referencesLock)
                File.WriteAllLines(refFile, references);
        } // end method

    } // end class
} // end namespace