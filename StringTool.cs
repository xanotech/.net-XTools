using System;

namespace Xanotech.Tools {
    public static class StringTool {

        public static bool Is(this string str, string value) {
            return str.Equals(value, StringComparison.OrdinalIgnoreCase);
        } // end method

    } // end class
} // end namespace