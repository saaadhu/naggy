using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NaggyClang;

namespace Naggy
{
    static class ErrorList
    {
        private static ErrorListProvider errorListProvider;

        public static void Initialize(IServiceProvider provider)
        {
            if (errorListProvider != null)
                return;

            errorListProvider = new ErrorListProvider(provider);
            errorListProvider.ProviderGuid = Guid.Parse("7C2C89EC-D368-4B15-B93A-E506EEA449E4");
            errorListProvider.ProviderName = "Naggy.DiagnosticsProvider";
        }

        public static void Clear()
        {
            errorListProvider.Tasks.Clear();
        }

        public static void Show(Diagnostic diag)
        {
            var task = new ErrorTask
                           {
                               Text = diag.Message,
                               Category = TaskCategory.CodeSense,
                               ErrorCategory =
                                   diag.Level == DiagnosticLevel.Warning
                                       ? TaskErrorCategory.Warning
                                       : TaskErrorCategory.Error,
                               Column = diag.StartColumn,
                               Line = diag.StartLine,
                               Document = diag.FilePath
                           };
            task.Navigate += (sender, args) => errorListProvider.Navigate(task, Guid.Parse(EnvDTE.Constants.vsViewKindCode));

            errorListProvider.Tasks.Add(task);
            errorListProvider.Show();
        }
    }
}
