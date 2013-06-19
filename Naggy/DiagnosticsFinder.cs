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
        public static event EventHandler<EventArgs> Toggled;

        private static bool enabled = true;
        public static bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                if (!enabled)
                    ErrorList.Clear();

                if (Toggled != null)
                    Toggled(null, new EventArgs());
            }
        }
        public static void Initialize(IServiceProvider serviceProvider, DTE dte)
        {
            ErrorList.Initialize(serviceProvider, dte);
            ClangServices.Initialize(dte);
            DiagnosticsBlacklist.Initialize();
        }

        public static IEnumerable<Diagnostic> Find(ITextBuffer buffer)
        {
            if (!Enabled)
                return Enumerable.Empty<Diagnostic>();

            ITextDocument document;
            if (!buffer.Properties.TryGetProperty(typeof(ITextDocument), out document) || document == null)
                return Enumerable.Empty<Diagnostic>();

            var extension = Path.GetExtension(document.FilePath);
            if (extension != null && extension.StartsWith(".h"))
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
