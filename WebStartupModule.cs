using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Web;

namespace XTools {
    public class WebStartupModule : IHttpModule {

        private static bool isStarted = false;
        private static object startupLock = new object();

        public void Dispose() {
        } // end method


 
        public void Init(HttpApplication context) {
            if (isStarted)
                return;

            lock (startupLock) {
                if (!isStarted) {
                    try {
                        Startup(context);
                    } finally {
                        isStarted = true;
                    } // end try-finally
                } // end if
            } // end lock
        } // end method



        private static void Startup(HttpApplication application) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var name = assembly.GetName().Name;
                
                // Skip Microsoft and .NET assemblies
                if (name.StartsWith("Microsoft") ||
                    name.StartsWith("mscorlib") ||
                    name.StartsWith("System"))
                    continue;

                foreach (var type in assembly.GetTypes()) {
                    if (!typeof(IWebStartupHandler).IsAssignableFrom(type) || !type.IsClass)
                        continue;

                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    var handler = constructor.Invoke(null) as IWebStartupHandler;
                    handler.HandleStartup(application);
                } // end foreach
            } // end foreach
        } // end method

    } // end class
} // end namespace
