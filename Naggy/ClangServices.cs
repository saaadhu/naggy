using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using EnvDTE;
using NaggyClang;

namespace Naggy
{
    public static class ClangServices
    {
        static ITextSnapshot lastProcessedSnapshot;
        static DTE dte;
        static object sync = new object();

        public static void Initialize(DTE dteInstance)
        {
            dte = dteInstance;
        }

        public static void Process(ITextBuffer buffer)
        {
            if (lastProcessedSnapshot == buffer.CurrentSnapshot)
                return;

            lock (sync)
            {
                var clangAdapter = GetClangAdapterForBuffer(buffer);
                clangAdapter.Process(buffer.CurrentSnapshot.GetText());
                lastProcessedSnapshot = buffer.CurrentSnapshot;
            }
        }

        public static IEnumerable<Diagnostic> GetDiagnostics(ITextBuffer buffer)
        {
            lock (sync)
            {
                return GetClangAdapterForBuffer(buffer).GetDiagnostics();
            }
        }

        public static PreprocessorAdapter GetPreprocessorAdapter(ITextBuffer buffer)
        {
            lock (sync)
            {
                return GetClangAdapterForBuffer(buffer).GetPreprocessor();
            }
        }

        private static ClangAdapter GetClangAdapterForBuffer(ITextBuffer buffer)
        {
            var clangAdapter = buffer.Properties.GetOrCreateSingletonProperty<ClangAdapter>(() =>
            {
                ITextDocument document;
                buffer.Properties.TryGetProperty(typeof(ITextDocument), out document);

                var filePath = document.FilePath;
                var includePaths = AVRStudio.GetIncludePaths(filePath, dte);
                var symbols = AVRStudio.GetPredefinedSymbols(filePath, dte);
                return new ClangAdapter(filePath, new List<string>(includePaths), new List<string>(symbols));
            });
            return clangAdapter;
        }
    }
}
