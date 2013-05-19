using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using NaggyClang;

namespace Naggy
{
    static class DiagnosticsFinder
    {
        public static void Initialize(IServiceProvider serviceProvider, DTE dte)
        {
            ErrorList.Initialize(serviceProvider, dte);
            ClangServices.Initialize(dte);
            DiagnosticsBlacklist.Initialize();
        }

        public static IEnumerable<Diagnostic> Find(ITextBuffer buffer)
        {
            ITextDocument document;
            if (!buffer.Properties.TryGetProperty(typeof(ITextDocument), out document) || document == null)
                return Enumerable.Empty<Diagnostic>();

            ClangServices.Process(buffer);
            ErrorList.ClearDiagnosticsFromFile(document.FilePath);

            var diags = (from diag in ClangServices.GetDiagnostics(buffer)
                   where DiagnosticsBlacklist.Contains(diag) == false &&
                         !diag.FilePath.Any(c => Path.GetInvalidPathChars().Contains(c)) &&
                         Path.GetFileName(diag.FilePath) == Path.GetFileName(document.FilePath)
                   select diag).ToList();

            ErrorList.Show(diags);
            return diags;
        }

    }
}
