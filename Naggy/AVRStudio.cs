using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.IO;

namespace Naggy
{
    static class AVRStudio
    {
        public static IEnumerable<string> GetPredefinedSymbols(string fileName, DTE dte)
        {
            var project = GetProject(dte, fileName);

            if (project == null)
                return Enumerable.Empty<string>();

            string deviceName = (string)project.Properties.Item("DeviceName").Value;
            var implicitSymbol = DeviceNameToPredefinedSymbolMapper.GetSymbol(deviceName);

            dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
            var symbolsInProject = GetPredefinedSymbols(toolchainOptions);

            var predefinedSymbols = new List<string>();
            predefinedSymbols.AddRange(implicitSymbol);

            predefinedSymbols.AddRange(symbolsInProject);
            return predefinedSymbols;
        }

        public static IEnumerable<string> GetIncludePaths(string fileName, DTE dte)
        {
            var project = GetProject(dte, fileName);

            if (project == null)
                return Enumerable.Empty<string>();

            dynamic toolchainProperties = project.Properties.Item("ToolchainOptions").Value;
            var cCompilerOptions = toolchainProperties.CCompiler;

            var allPaths = new List<string>();
            allPaths.AddRange(cCompilerOptions.IncludePaths);

            IEnumerable<string> defaultIncludePaths = cCompilerOptions.DefaultIncludePaths;
            allPaths.AddRange(defaultIncludePaths.Select(path => path.Replace(@"\bin", "")));

            return allPaths;
        }

        private static string[] GetPredefinedSymbols(dynamic toolchainOptions)
        {
            return toolchainOptions.CCompiler.SymbolDefines.ToArray();
        }

        static Project GetProject(DTE dte, string fileName)
        {
            if (dte.Solution == null)
                return null;

            var projectItem = dte.Solution.FindProjectItem(fileName);
            if (projectItem != null && projectItem.ContainingProject != null && projectItem.ContainingProject.Properties != null)
                return projectItem.ContainingProject;

            Array arr = (Array)dte.ActiveSolutionProjects;
            if (arr.Length == 0)
                return null;

            return (Project)arr.GetValue(0);
        }

        private static string GetPropertyValue(dynamic toolchainData, string propertyId)
        {
            try
            {
                return toolchainData.GetPropertyValue(propertyId);
            }
            catch (Exception) { }
            return string.Empty;
        }
    }
}
