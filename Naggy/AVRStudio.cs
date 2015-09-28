using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.IO;
using NaggyClang;

namespace Naggy
{
    static class AVRStudio
    {
        public static IEnumerable<string> GetPredefinedSymbols(string fileName, DTE dte)
        {
            var project = GetProject(dte, fileName);

            if (project == null)
            {
                return Enumerable.Empty<string>();
            }

            string deviceName = (string)project.Properties.Item("DeviceName").Value;
            var arch = GetArch(fileName, dte);
            var implicitSymbol = DeviceNameToPredefinedSymbolMapper.GetSymbols(deviceName, arch);

            dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
            var symbolsInProject = GetPredefinedSymbols(IsCPP(fileName, project) ? toolchainOptions.CppCompiler: toolchainOptions.CCompiler);

            var predefinedSymbols = new List<string>();
            predefinedSymbols.AddRange(implicitSymbol);

            predefinedSymbols.AddRange(symbolsInProject);
            return predefinedSymbols;
        }

        public static NaggyClang.Language GetLanguage(string filePath, DTE dte)
        {
            var project = GetProject(dte, filePath);

            if (project == null)
                return NaggyClang.Language.C;

            if (IsCPP(filePath, project))
            {
                if (IsCPP11Enabled(project))
                    return NaggyClang.Language.Cpp11;
                return NaggyClang.Language.Cpp;
            }

            if (IsC99Enabled(project))
                return NaggyClang.Language.C99;
            return NaggyClang.Language.C;
        }


        public static NaggyClang.Arch GetArch (string filePath, DTE dte)
        {
            var project = GetProject(dte, filePath);

            if (project == null)
                return NaggyClang.Arch.AVR;

            dynamic toolchainName = project.Properties.Item("ToolchainName").Value;
            if (toolchainName == null)
                return NaggyClang.Arch.AVR;
            return GetArchFromCommandLine(toolchainName);
        }

        private static NaggyClang.Arch GetArchFromCommandLine (string toolchainName)
        {
            if (toolchainName.Contains("AVRGCC32"))
                return NaggyClang.Arch.AVR32;
            if (toolchainName.Contains("ARM"))
                return NaggyClang.Arch.ARM;
            return NaggyClang.Arch.AVR;
        }

        private static bool IsCPP(string filename, dynamic project)
        {
            return Path.GetExtension(filename) != ".c" && project.Object.GetProjectProperty("Language") == "CPP";
        }

        public static bool IsC99Enabled(Project project)
        {
            dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
            var commandLine = (string) toolchainOptions.CCompiler.CommandLine;
            return commandLine != null && (commandLine.Contains("-std=gnu99") || commandLine.Contains("-std=c99"));
        }

        public static bool IsCPP11Enabled(Project project)
        {
            dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
            var commandLine = (string) toolchainOptions.CppCompiler.CommandLine;
            return commandLine != null && (commandLine.Contains("-std=gnu++11") || commandLine.Contains("-std=c++11"));
        }

        public static IEnumerable<string> GetIncludePaths(string fileName, DTE dte)
        {
            var project = GetProject(dte, fileName);

            // Before giving up, see if it is a file inside the toolchain dirs, if so, find 
            // a project with the same toolchain dir and returns the DefaultIncludePaths
            if (project == null)
                return Enumerable.Empty<string>();

            dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
            var compiler = IsCPP(fileName, project) ? toolchainOptions.CppCompiler : toolchainOptions.CCompiler;
            IEnumerable<string> defaultIncludePaths = compiler.DefaultIncludePaths;

            var adjustedDefaultIncludePaths = defaultIncludePaths
                .Select(p => p.Replace("bin\\", string.Empty));
            
            IEnumerable<string> projectSpecificIncludePaths = compiler.IncludePaths;
            var expandedProjectSpecificIncludePaths = projectSpecificIncludePaths
                .Select(p => p.Replace("$(ToolchainDir)", (string)((dynamic)project).Object.GetProjectProperty("ToolchainDir")));
            string outputFolder = ((dynamic)project.Object).GetProjectProperty("OutputDirectory");
            var absoluteProjectSpecificFolderPaths = expandedProjectSpecificIncludePaths
                .Select(p => Path.IsPathRooted(p) ? p : Path.Combine(outputFolder, p));
            
            return adjustedDefaultIncludePaths.Concat(absoluteProjectSpecificFolderPaths);
        }

        private static string[] GetPredefinedSymbols(dynamic compiler)
        {
            var commandLine = (string)compiler.CommandLine;
            var options = commandLine.Split(new char[] { ' ' });
            var symbols = options.Where(p => p.StartsWith("-D")).Select(p => p.Substring("-D".Length)).ToList();
            symbols.AddRange(compiler.SymbolDefines);
            return symbols.Distinct().ToArray();
        }

        internal static ProjectItem GetProjectItem(DTE dte, string fileName)
        {
            if (dte.Solution == null)
                return null;

            return dte.Solution.FindProjectItem(fileName);
        }

        static Project GetProject(DTE dte, string fileName)
        {
            var projectItem = GetProjectItem(dte, fileName);
            if (projectItem != null && projectItem.ContainingProject != null && projectItem.ContainingProject.Properties != null)
                return projectItem.ContainingProject;

            // The file was not a project item. See if we can find it any project's toolchain header paths.
            var project = GetPossibleProjectBasedOnToolchainHeaderPath(fileName, dte);
            if (project != null)
                return project;

            // Otherwise we're out of options, just return the first project we find.
            var projects = GetProjectsInSolution(dte);
            return projects.FirstOrDefault();
        }

        static IEnumerable<Project> GetProjectsInSolution(DTE dte)
        {
            var projects = dte.Solution.Projects;
            for (int i = 1; i <= projects.Count; ++i)
                yield return projects.Item(i);
        }

        private static Project GetPossibleProjectBasedOnToolchainHeaderPath(string fileName, DTE dte)
        {
            foreach (var project in GetProjectsInSolution(dte))
            {
                dynamic toolchainOptions = project.Properties.Item("ToolchainOptions").Value;
                var compiler= IsCPP(fileName, project) ? toolchainOptions.CppCompiler: toolchainOptions.CCompiler;
                IEnumerable<string> defaultIncludePaths = compiler.DefaultIncludePaths;
                if (defaultIncludePaths.Any(fileName.Contains))
                    return project;
            }

            return null;
        }

    }
}
