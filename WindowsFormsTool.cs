using System;
using System.Windows.Forms;

namespace Xanotech.Tools {
    public static class WindowsFormsTool {

        public static void InvokeAction(this Control control, Action action) {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        } // End method



        public static T InvokeFunc<T>(this Control control, Func<T> func) {
            if (control.InvokeRequired)
                return (T)control.Invoke(func);
            else
                return func();
        } // End method

    } // End class
} // End namespace