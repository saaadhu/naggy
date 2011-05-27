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
    [ContentType("AVRGcc")]
    public sealed class DiagnosticTaggerProvider : ITaggerProvider
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
    [Export(typeof(IClassifierProvider))]
    [ContentType("AVRGcc")]
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
            return new PreprocessorClassifier(dte, textBuffer, classificationTypeRegistry);
        }
    }
//;
//    [Export(typeof(ITaggerProvider))]
//    [TagType(typeof(ClassificationTag))]
//    [ContentType("AVRGcc")]
//    public sealed class PreprocessorHighlightTaggerProvider : ITaggerProvider
//    {
//        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
//        {
//            var dte = (DTE)ServiceProvider.GetService(typeof(DTE));
//            var agg = Aggregator.CreateTagAggregator<ClassificationTag>(buffer);
//            Func<ITagger<T>> taggerFunc = () => new PreprocessorHighlighterTagger(dte, buffer, ClassificationRegistry, agg) as ITagger<T>;
//            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(taggerFunc);
//        }

//        [Import]
//        internal IClassificationTypeRegistryService ClassificationRegistry = null;
//        [Import]
//        internal IBufferTagAggregatorFactoryService Aggregator = null;

//        [Import]
//        internal SVsServiceProvider ServiceProvider = null;

//    }
}

