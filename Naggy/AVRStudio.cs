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

            var deviceName = project.Properties.Item("DeviceName").Value;
            var implicitSymbol = DeviceNameToPredefinedSymbolMapper.GetSymbol(deviceName);

            dynamic toolchainData = project.Properties.Item("ToolchainData").Value;
            var symbolsInProject = GetPredefinedSymbols(toolchainData);

            var predefinedSymbols = new List<string>();
            predefinedSymbols.Add(implicitSymbol);

            predefinedSymbols.AddRange(symbolsInProject);
            return predefinedSymbols;
        }

        public static IEnumerable<string> GetIncludePaths(string fileName, DTE dte)
        {
            var project = GetProject(dte, fileName);

            if (project == null)
                return Enumerable.Empty<string>();

            dynamic toolchainData = project.Properties.Item("ToolchainData").Value;
            var outputFolder = project.Object.GetProjectProperty("OutputDirectory");

            const string toolchainIncludePathPropertyId = "avr32gcc.toolchain.directories.ToolchainDeviceFiles";

            // The property queried is actually the device header file, so we need to go two directories up to go to the include directory
            var toolchainIncludeFolder = Path.GetDirectoryName(Path.GetDirectoryName(toolchainData.GetPropertyValue(toolchainIncludePathPropertyId)));

            var splitPaths = GetCompilerIncludePaths(toolchainData, outputFolder);

            var allPaths = new List<string>();
            allPaths.Add(toolchainIncludeFolder);
            allPaths.AddRange(splitPaths);

            return allPaths;
        }

        private static string[] GetPredefinedSymbols(dynamic toolchainData)
        {
            var symbols8BitPropertyId = "avrgcc.compiler.symbols.DefSymbols";
            var symbols32BitPropertyId = "avr32gcc.compiler.symbols.DefSymbols";

            string symbols = GetPropertyValue(toolchainData, symbols8BitPropertyId);

            if (string.IsNullOrEmpty(symbols))
                symbols = GetPropertyValue(toolchainData, symbols32BitPropertyId);

            return string.IsNullOrEmpty(symbols) ? new string[]{} : symbols.Split(',');
        }

        private static string[] GetCompilerIncludePaths(dynamic toolchainData, string outputDirectory)
        {
            var includePaths8BitPropertyId = "avrgcc.compiler.directories.IncludePaths";
            var includePaths32BitPropertyId = "avr32gcc.compiler.directories.IncludePaths";

            string includePath = GetPropertyValue(toolchainData, includePaths8BitPropertyId);

            if (string.IsNullOrEmpty(includePath))
            {
                includePath = GetPropertyValue(toolchainData, includePaths32BitPropertyId);
            }
            var splitPaths = includePath.Split(',');

            for(int i = 0; i<splitPaths.Length; ++i)
            {
                var path = splitPaths[i];
                if (!Path.IsPathRooted(path))
                {
                    splitPaths[i] = Path.Combine(outputDirectory, path);
                }
            }
            return splitPaths;
        }

        static Project GetProject(DTE dte, string fileName)
        {
            if (dte.Solution == null)
                return null;

            var projectItem = dte.Solution.FindProjectItem(fileName);
            if (projectItem == null || projectItem.ContainingProject == null || projectItem.ContainingProject.Properties == null)
                return null;

            return projectItem.ContainingProject;
        }

        private static string GetPropertyValue(dynamic toolchainData, string propertyId)
        {
            try
            {
                return toolchainData.GetPropertyValue(propertyId);
            }
            catch (Exception e) { }
            return string.Empty;
        }
    }
}
