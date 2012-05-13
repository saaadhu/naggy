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
using Microsoft.VisualStudio.Text.Classification;

namespace Naggy
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(ErrorTag))]
    [ContentType("GCC")]
    public sealed class DiagnosticTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            ClangServices.Initialize(dte);
            Func<ITagger<T>> taggerFunc = () => new DiagnosticTagger(dte, buffer) as ITagger<T>;
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(taggerFunc);
        }

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

    }
    [Export(typeof(IClassifierProvider))]
    [ContentType("GCC")]
    [Name("Preprocessor Classifier")]
    internal class ClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistry { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            var dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            ClangServices.Initialize(dte);
            Func<IClassifier> preprocessorClassifierFunc = () => new PreprocessorClassifier(dte, textBuffer, classificationTypeRegistry) as IClassifier;
            return textBuffer.Properties.GetOrCreateSingletonProperty<IClassifier>(preprocessorClassifierFunc);
        }
    }
}

