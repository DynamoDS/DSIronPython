﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Dynamo.Extensions;
using Dynamo.Logging;
using Dynamo.PythonServices;

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
        /// <param name="sp"></param>
        public void Ready(ReadyParams sp)
        {
            var extraPath = Path.Combine(new FileInfo(Assembly.GetAssembly(typeof(IronPythonExtension)).Location).Directory.Parent.FullName, "extra");
            var alc = new IsolatedPythoContext(Path.Combine(extraPath,"DSIronPython.dll"));
            alc.LoadFromAssemblyName(new AssemblyName("DSIronPython"));
        }

        /// <summary>
        /// Action to be invoked when shutdown has begun.
        /// </summary>
        public void Shutdown()
        {
            // Do nothing for now
        }
    }
    internal class IsolatedPythoContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver resolver;

        public IsolatedPythoContext(string libPath)
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
