using Dynamo.Extensions;
using Dynamo.Graph.Workspaces;
using Dynamo.Logging;
using Dynamo.PythonServices;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace IronPythonExtension
{
    /// <summary>
    /// This extension does nothing but loading DSIronPython to make IronPython engine 
    /// available as one alternative Python evaluation option
    /// </summary>
    public class IronPythonExtension : IExtension, ILogSource
    {
        private const string PythonEvaluatorAssembly = "DSIronPython";

        /// <summary>
        /// Dispose function
        /// </summary>
        public void Dispose()
        {
            // Do nothing for now
        }

        /// <summary>
        /// Extension unique GUID
        /// </summary>
        public string UniqueId => "D7B449D7-4D54-47EF-B742-30C7BEDFBE92";

        /// <summary>
        /// Extension name
        /// </summary>
        public string Name => "IronPythonExtension";

        #region ILogSource

        public event Action<ILogMessage> MessageLogged;
        internal void OnMessageLogged(ILogMessage msg)
        {
            if (this.MessageLogged != null)
            {
                MessageLogged?.Invoke(msg);
            }
        }
        #endregion

        /// <summary>
        /// Action to be invoked when Dynamo begins to start up. 
        /// </summary>
        /// <param name="sp"></param>
        public void Startup(StartupParams sp)
        {
            // Do nothing for now
        }

        /// <summary>
        /// Action to be invoked when the Dynamo has started up and is ready
        /// for user interaction. 
        /// </summary>
        /// <param name="rp"></param>
        public void Ready(ReadyParams rp)
        {
            var extraPath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.Parent.FullName, "extra");
            var alc = new IsolatedPythonContext(Path.Combine(extraPath,$"{PythonEvaluatorAssembly}.dll"));
            var dsIronAssem = alc.LoadFromAssemblyName(new AssemblyName(PythonEvaluatorAssembly));

            //load the engine into Dynamo ourselves.
            LoadPythonEngine(dsIronAssem);
            
            //we used to do this:
            //but it's not neccesary to load anything into the VM.
            //instead we skip all the extra work and trigger the side effect we want
            //which is re executing the graph after the dsIronPython evaluator is loaded into the PythonEngineManager.
            //rp.StartupParams.LibraryLoader.LoadNodeLibrary(dsIronAssem);

            if(rp.CurrentWorkspaceModel is HomeWorkspaceModel hwm)
            {
                foreach (var n in hwm.Nodes)
                {
                    n.MarkNodeAsModified(true);
                }
                hwm.Run();
            }
        }
        public void Shutdown()
        {
            // Do nothing for now
        }

        private static void LoadPythonEngine(Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }

            // Currently we are using try-catch to validate loaded assembly and Singleton Instance method exist
            // but we can optimize by checking all loaded types against evaluators interface later
            try
            {
                Type eType = null;
                PropertyInfo instanceProp = null;
                try
                {
                    eType = assembly.GetTypes().FirstOrDefault(x => typeof(PythonEngine).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
                    if (eType == null) return;

                    instanceProp = eType?.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static);
                    if (instanceProp == null) return;
                }
                catch
                {
                    // Ignore exceptions from iterating assembly types.
                    return;
                }

                PythonEngine engine = (PythonEngine)instanceProp.GetValue(null);
                if (engine == null)
                {
                    throw new Exception($"Could not get a valid PythonEngine instance by calling the {eType.Name}.Instance method");
                }

                if (PythonEngineManager.Instance.AvailableEngines.All(x => x.Name != engine.Name))
                {
                    PythonEngineManager.Instance.AvailableEngines.Add(engine);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add a Python engine from assembly {assembly.GetName().Name}.dll with error: {ex.Message}");
            }
        }


    }
    internal class IsolatedPythonContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver resolver;

        public IsolatedPythonContext(string libPath)
        {
            resolver = new AssemblyDependencyResolver(libPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
