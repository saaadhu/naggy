using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
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
            lock (sync)
            {
                if (lastProcessedSnapshot == buffer.CurrentSnapshot)
                    return;

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
            ClangAdapter clangAdapter = null;
            ThreadHelper.Generic.Invoke(new Action(()=>
                               {
                                   clangAdapter = buffer.Properties.GetOrCreateSingletonProperty <ClangAdapter>( () => CreateClangAdapter(buffer));
                               }));
            return clangAdapter;
        }

        private static ClangAdapter CreateClangAdapter(ITextBuffer buffer)
        {
            ITextDocument document;
            buffer.Properties.TryGetProperty(typeof (ITextDocument), out document);

            var filePath = document.FilePath;
            var includePaths = AVRStudio.GetIncludePaths(filePath, dte);
            var symbols = AVRStudio.GetPredefinedSymbols(filePath, dte);
            return new ClangAdapter(filePath, new List<string>(includePaths), new List<string>(symbols));
        }
    }
}
