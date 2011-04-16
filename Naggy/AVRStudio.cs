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
        public static IEnumerable<string> GetIncludePaths(string fileName, DTE dte)
        {
            if (dte.Solution == null)
                return Enumerable.Empty<string>();

            var projectItem = dte.Solution.FindProjectItem(fileName);
            if (projectItem == null || projectItem.ContainingProject == null || projectItem.ContainingProject.Properties == null)
                return Enumerable.Empty<string>();

            dynamic toolchainData = projectItem.ContainingProject.Properties.Item("ToolchainData").Value;
            var outputFolder = projectItem.ContainingProject.Object.GetProjectProperty("OutputDirectory");

            const string toolchainIncludePathPropertyId = "avr32gcc.toolchain.directories.ToolchainDeviceFiles";

            // The property queried is actually the device header file, so we need to go two directories up to go to the include directory
            var toolchainIncludeFolder = Path.GetDirectoryName(Path.GetDirectoryName(toolchainData.GetPropertyValue(toolchainIncludePathPropertyId)));

            var splitPaths = GetCompilerIncludePaths(toolchainData, outputFolder);

            var allPaths = new List<string>();
            allPaths.Add(toolchainIncludeFolder);
            allPaths.AddRange(splitPaths);

            return allPaths;
        }

        private static string[] GetCompilerIncludePaths(dynamic toolchainData, string outputDirectory)
        {
            var includePaths8BitPropertyId = "avrgcc.compiler.directories.IncludePaths";
            var includePaths32BitPropertyId = "avr32gcc.compiler.directories.IncludePaths";

            string includePath = string.Empty;
            includePath = GetPropertyValue(toolchainData, includePaths8BitPropertyId);

            if (string.IsNullOrEmpty(includePath))
            {
                includePath = GetPropertyValue(toolchainData, includePaths32BitPropertyId);
            }
            var splitPaths = includePath.Split(',').ToArray();

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
