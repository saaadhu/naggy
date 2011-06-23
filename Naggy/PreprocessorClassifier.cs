using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using NaggyClang;
using EnvDTE;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace Naggy
{
    class PreprocessorClassifier : IClassifier
    {
        private readonly ITextBuffer buffer;
        private readonly ITextDocument document;
        private readonly ClangAdapter clangAdapter;
        private IClassificationType excludedCodeClassificationType;
        private readonly DTE dte;
        private SnapshotSpan lastSpan;

        readonly List<SnapshotSpan> excludedSpans = new List<SnapshotSpan>();
        DelayedRequestExecutor<int> debouncer = new DelayedRequestExecutor<int>(1000);
        IClassificationTypeRegistryService classificationRegistry;

        public PreprocessorClassifier(DTE dte, ITextBuffer buffer, Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService classificationRegistry)
        {
            this.classificationRegistry = classificationRegistry;

            excludedCodeClassificationType = this.classificationRegistry.GetClassificationType(Constants.ClassificationName);

            this.dte = dte;
            this.buffer = buffer;
            this.buffer.Changed += new EventHandler<TextContentChangedEventArgs>(buffer_Changed);
            buffer.Properties.TryGetProperty(typeof (ITextDocument), out document);

            var filePath = document.FilePath;
            var includePaths = AVRStudio.GetIncludePaths(filePath, dte);
            var symbols = AVRStudio.GetPredefinedSymbols(filePath, dte);
            clangAdapter = new ClangAdapter(filePath, new List<string>(includePaths), new List<string>(symbols));

            debouncer.Add(0, FindSkippedRegions);
        }

        private void buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            FindSkippedRegions();
            //debouncer.Add(0, FindSkippedRegions);
        }

        object obj = new object();

        void FindSkippedRegions()
        {
            lock (obj)
            {
                excludedSpans.Clear();

                int minPosition = buffer.CurrentSnapshot.Length;
                int maxPosition = 0;

                clangAdapter.Process(buffer.CurrentSnapshot.GetText());
                var preprocessor = clangAdapter.GetPreprocessor();
                {
                    foreach (var skippedBlock in preprocessor.GetSkippedBlockLineNumbers())
                    {
                        var textLine = buffer.CurrentSnapshot.GetLineFromLineNumber(skippedBlock.Item1 - 1);
                        var startPosition = textLine.Start.Position;

                        var endTextLine = buffer.CurrentSnapshot.GetLineFromLineNumber(skippedBlock.Item2 - 1);
                        var endPosition = endTextLine.End.Position;

                        minPosition = Math.Min(minPosition, startPosition);
                        maxPosition = Math.Max(maxPosition, endPosition);

                        SnapshotSpan span = new SnapshotSpan(buffer.CurrentSnapshot, Span.FromBounds(startPosition, endPosition));
                        excludedSpans.Add(span);
                    }
                }

                if (excludedSpans.Any())
                {
                    RaiseTagsChanged(minPosition, maxPosition);
                }
                else
                {
                    if (ClassificationChanged != null)
                        ClassificationChanged(this, 
                            new ClassificationChangedEventArgs(lastSpan.IsEmpty ? new SnapshotSpan(buffer.CurrentSnapshot, Span.FromBounds(0, 0)) : lastSpan));
                }
            }
        }

        private void RaiseTagsChanged(int minPosition, int maxPosition)
        {
            SnapshotSpan changedSpan = new SnapshotSpan(buffer.CurrentSnapshot,
                                                        Span.FromBounds(minPosition, maxPosition));
            lastSpan = changedSpan;

            if (ClassificationChanged != null)
                ClassificationChanged(this, new ClassificationChangedEventArgs(changedSpan));
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> spans = new List<ClassificationSpan>();
            foreach (var excludedSpan in excludedSpans)
            {
                spans.Add(new ClassificationSpan(excludedSpan, excludedCodeClassificationType));
            }

            return spans;
        }
    }
}
