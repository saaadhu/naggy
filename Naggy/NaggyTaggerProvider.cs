using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using NaggyClang;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace Naggy
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(ErrorTag))]
    [ContentType("AVRGcc")]
    public sealed class NaggyTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            Func<ITagger<T>> taggerFunc = () => new DiagnosticTagger(dte, buffer) as ITagger<T>;
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(taggerFunc);
        }

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

    }
}

