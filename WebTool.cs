using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XTools {

    /// <summary>
    ///   This class contains methods useful for simplifying web processing.
    /// </summary>
    public static class WebTool {

        public static void AutoScript(this Page page, string path) {
            var suffix = "";
            if (!page.Request.QueryString.ToString().ToLower().Contains("nojavascriptsuffix"))
                suffix = "?" + DateTime.Now;

            var localPath = page.Server.MapPath(path);
            var root = page.Server.MapPath("~");
            var files = new List<string>();
            files.AddRange(Directory.EnumerateFiles(localPath, "*.js", SearchOption.AllDirectories));
            files.Sort();

            var ignore = GetIgnoredFiles(localPath);
            var references = GetReferences(root);

            foreach (var file in files) {
                var src = file.Substring(root.Length).Replace('\\', '/');
                if (IsIgnored(ignore, src))
                    continue;

                page.Response.Write("<script src=\"" + src + suffix + "\"></script>" + Environment.NewLine);
                if (references != null && !references.Contains(src))
                    references.Add(src);
            } // end foreach
            SaveReferences(root, references);
        } // end method



        public static IList GetAllControls(this Control parent) {
            ArrayList allControls = new ArrayList();
            foreach (Control c in parent.Controls) {
                allControls.Add(c);
                allControls.AddRange(c.GetAllControls());
            } // end foreach
            return allControls;
        } // end method



        private static IEnumerable<Assembly> GetBinAssemblies(this IHttpController controller) {
            var assemblies = new List<Assembly>();
            string path = Assembly.GetExecutingAssembly().Location;
            if (File.Exists(path))
                path = Path.GetDirectoryName(path);

            foreach (string dllFilename in Directory.GetFiles(path, "*.dll"))
                assemblies.Add(Assembly.LoadFile(dllFilename));
            return assemblies;
        } // end method
        
        
        
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
            }
            return cookieValue;
        } // end method



        private static IEnumerable<string> GetIgnoredFiles(string localPath) {
            var ignore = new string[0];
            var ignoreFile = Path.Combine(localPath, "_ignore.txt");
            if (File.Exists(ignoreFile))
                ignore = File.ReadAllLines(ignoreFile);
            return ignore;
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



        private static IList<string> GetReferences(string rootPath) {
            var refFile = Path.Combine(rootPath, "_references.js");
            if (!File.Exists(refFile))
                return null;

            var lines = File.ReadAllLines(refFile);
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



        private static void SaveReferences(string rootPath, IList<string> references) {
            if (references == null)
                return;

            for (var r = 0; r < references.Count; r++)
                references[r] = "/// <reference path=\"" + references[r] + "\"/>";
            references.Sort();
            var refFile = Path.Combine(rootPath, "_references.js");
            references.Insert(0, "// This file is auto-generated by XTools.WebTool.AutoScript.");
            File.WriteAllLines(refFile, references);
        } // end method

    } // end class
} // end namespace