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



        private static void MapToAssemblies(List<StartupTaskInfo> startupTaskInfos) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    foreach (var info in startupTaskInfos)
                        if (info.assemblyName == assembly.GetName().Name) {
                            info.assembly = assembly;
                            info.type = assembly.GetType(info.typeName);
                            if (info.type == null)
                                throw new TypeLoadException("Unable to load type \"" +
                                    info.typeName + "\" from assembly \"" +
                                    info.assemblyName + "\".");

                            info.method = info.type.GetMethod(info.methodName, new[] {typeof(HttpApplication)});
                            if (info.method == null)
                                info.method = info.type.GetMethod(info.methodName, new Type[0]);
                            if (info.method == null)
                                throw new MissingMethodException("Unable to find method \"" +
                                    info.methodName + "\" in type \"" +
                                    info.typeName + "\".");
                        } // end if
                } catch (SecurityException) {
                    // In certain production systems, this code is not
                    // allowed to pull assembly information.  If that
                    // is the case, simply continue onto the next one.
                    continue;
                } // end try-catch
            } // end if

            var infoMissingAssembly = startupTaskInfos.FirstOrDefault(sti => sti.assembly == null);
            if (infoMissingAssembly != null)
                throw new DllNotFoundException("No assembly loaded with the name \"" +
                    infoMissingAssembly.assemblyName + "\".");
        } // end method



        private static void Startup(HttpApplication application) {
            var configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            configPath = Path.GetDirectoryName(configPath);
            configPath = Path.Combine(configPath, "WebStartupModule.config");
            if (!File.Exists(configPath))
                return;

            var configLines = File.ReadAllLines(configPath);
            var startupTaskInfos = configLines.Select(cl => StartupTaskInfo.ParseConfigLine(cl)).ToList();

            MapToAssemblies(startupTaskInfos);
            foreach (var info in startupTaskInfos)
                info.Execute(application);
        } // end method



        private class StartupTaskInfo {
            public Assembly assembly;
            public string assemblyName;
            public Type type;
            public string typeName;
            public MethodInfo method;
            public string methodName;

            public void Execute(HttpApplication application) {
                var constructor = method.IsStatic ?
                    null : type.GetConstructor(Type.EmptyTypes);
                if (!method.IsStatic && constructor == null)
                    throw new MissingMethodException("No default constructor for \"" +
                        typeName + "\" / the method \"" +
                        methodName + "\" is not static.");

                var obj = constructor == null ?
                    null : constructor.Invoke(null);
                var parameters = method.GetParameters().Length == 0 ?
                    null : new[] {application};
                method.Invoke(obj, parameters);
            } // end method

            public static StartupTaskInfo ParseConfigLine(string configLine) {
                if (string.IsNullOrWhiteSpace(configLine))
                    return null;

                configLine = configLine.Trim();
                if (configLine.StartsWith(";"))
                    return null;

                var barSplit = configLine.Split('|');
                if (barSplit.Length != 2)
                    ThrowFormatException(configLine);

                var dotSplit = barSplit[1].Split('.');
                if (dotSplit.Length < 3)
                    ThrowFormatException(configLine);

                var info = new StartupTaskInfo();
                info.assemblyName = barSplit[0];
                info.typeName = string.Join(".", dotSplit.Take(dotSplit.Length - 1));
                info.methodName = dotSplit[dotSplit.Length - 1];
                return info;
            } // end constructor

            private static void ThrowFormatException(string configLine) {
                throw new FormatException("The WebStartupModule.config line \"" +
                    configLine + "\" is invalid.  The valid format for config lines is " +
                    "\"AssemblyNameWithoutDll|Some.Name.Space.TypeName.MethodName\".");
            } // end method
        } // end class

    } // end class
} // end namespace
