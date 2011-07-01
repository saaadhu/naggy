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
        private IClassificationType excludedCodeClassificationType;
        private SnapshotSpan lastSpan;

        readonly List<SnapshotSpan> excludedSpans = new List<SnapshotSpan>();
        DelayedRequestExecutor<int> debouncer = new DelayedRequestExecutor<int>(3000);
        IClassificationTypeRegistryService classificationRegistry;

        public PreprocessorClassifier(DTE dte, ITextBuffer buffer, Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService classificationRegistry)
        {
            this.classificationRegistry = classificationRegistry;

            excludedCodeClassificationType = this.classificationRegistry.GetClassificationType(Constants.ClassificationName);

            this.buffer = buffer;
            this.buffer.Changed += new EventHandler<TextContentChangedEventArgs>(buffer_Changed);

            debouncer.Add(1, FindSkippedRegions);
        }

        private void buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            debouncer.Add(1, FindSkippedRegions);
        }

        object obj = new object();

        void FindSkippedRegions()
        {
            lock (obj)
            {
                excludedSpans.Clear();

                int minPosition = buffer.CurrentSnapshot.Length;
                int maxPosition = 0;

                ClangServices.Process(buffer);
                var preprocessor = ClangServices.GetPreprocessorAdapter(buffer);
                {
                    foreach (var skippedBlock in preprocessor.GetSkippedBlockLineNumbers())
                    {
                        var textLine = buffer.CurrentSnapshot.GetLineFromLineNumber(skippedBlock.Item1 - 1);
                        var startPosition = textLine.Start.Position;

                        int endLineNumber = skippedBlock.Item2 == 0 ? buffer.CurrentSnapshot.LineCount : skippedBlock.Item2;
                        var endTextLine = buffer.CurrentSnapshot.GetLineFromLineNumber(endLineNumber - 1);
                        var endPosition = endTextLine.End.Position;

                        minPosition = Math.Min(minPosition, startPosition);
                        maxPosition = Math.Max(maxPosition, endPosition);

                        if (!lastSpan.IsEmpty)
                        {
                            minPosition = Math.Min(minPosition, lastSpan.Start);
                            maxPosition = Math.Max(maxPosition, lastSpan.End);
                        }

                        SnapshotSpan span = new SnapshotSpan(buffer.CurrentSnapshot, Span.FromBounds(startPosition, endPosition));
                        excludedSpans.Add(span);
                    }
                }

                minPosition = Math.Max(minPosition, 0);
                maxPosition = Math.Min(maxPosition, buffer.CurrentSnapshot.Length);

                if (excludedSpans.Any())
                {
                    RaiseTagsChanged(minPosition, maxPosition);
                }
                else
                {
                    if (ClassificationChanged != null)
                        ClassificationChanged(this, new ClassificationChangedEventArgs(new SnapshotSpan(buffer.CurrentSnapshot, Span.FromBounds(0, buffer.CurrentSnapshot.Length))));
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
