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

            return Is32BitProject(project) ?
            new List<string>
                       {
                           @"C:\Program Files (x86)\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr32\4.4.3\include",
                           @"C:\Program Files (x86)\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr32\4.4.3\include-fixed",
                           @"C:\Program Files (x86)\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\avr32\include",
                           @"C:\Program Files\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr32\4.4.4\include",
                           @"C:\Program Files\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr32\4.4.3\include-fixed",
                           @"C:\Program Files\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\avr32\include"
                       }
                       :
            new List<string>
                       {
                           @"C:\Program Files (x86)\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr\4.5.1\include",
                           @"C:\Program Files (x86)\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr\4.5.1\include-fixed",
                           @"C:\Program Files (x86)\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\avr\include",
                           @"C:\Program Files\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr\4.5.1\include",
                           @"C:\Program Files\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\lib\gcc\avr\4.5.1\include-fixed",
                           @"C:\Program Files\Atmel\AVR Studio 5.1\extensions\Atmel\AVRGCC\3.3.1\AVRToolchain\avr\include"
                       };
        }

        static bool Is32BitProject(dynamic project)
        {
            dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
            System.Type type = toolchainOptions.GetType();

            return type.FullName.Contains("32");
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
