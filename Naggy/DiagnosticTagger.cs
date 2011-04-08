using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using NaggyClang;

namespace Naggy
{
    class DiagnosticTagger : ITagger<ErrorTag>
    {
        private readonly ITextBuffer buffer;
        private readonly ITextDocument document;
        readonly List<Tuple<SnapshotSpan, string>> spansAndErrorMessages = new List<Tuple<SnapshotSpan, string>>();
        DelayedRequestExecutor<int> debouncer = new DelayedRequestExecutor<int>(1000);

        public DiagnosticTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.buffer.Changed += new EventHandler<TextContentChangedEventArgs>(buffer_Changed);
            buffer.Properties.TryGetProperty(typeof (ITextDocument), out document);
            //document.FileActionOccurred += new EventHandler<TextDocumentFileActionEventArgs>(document_FileActionOccurred);
        }

        private void buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            debouncer.Add(0, FindDiagnostics);
        }

        private SnapshotSpan lastTotalDiagnosticsSpan;

        private void FindDiagnostics()
        {
            spansAndErrorMessages.Clear();
            var filePath = document.FilePath;

            int minPosition = buffer.CurrentSnapshot.Length;
            int maxPosition = 0; 

            ClangAdapter c = new ClangAdapter();
            foreach (var diagnostic in c.GetDiagnostics(filePath))
            {
                if (diagnostic.StartLine != 0)
                    diagnostic.StartLine--;

                var textLine = buffer.CurrentSnapshot.GetLineFromLineNumber(diagnostic.StartLine);
                var startPosition = textLine.Start.Position;
                var endPosition = textLine.End.Position;

                minPosition = Math.Min(minPosition, startPosition);
                maxPosition = Math.Max(maxPosition, endPosition);

                SnapshotSpan span = new SnapshotSpan(buffer.CurrentSnapshot, Span.FromBounds(startPosition, endPosition));
                spansAndErrorMessages.Add(Tuple.Create(span, diagnostic.Message));
            }

            if (spansAndErrorMessages.Any())
            {
                RaiseTagsChanged(minPosition, maxPosition);
            }
            else
            {
                if (TagsChanged != null)
                    TagsChanged(this, new SnapshotSpanEventArgs(lastTotalDiagnosticsSpan));
            }
        }

        private void RaiseTagsChanged(int minPosition, int maxPosition)
        {
            SnapshotSpan changedSpan = new SnapshotSpan(buffer.CurrentSnapshot,
                                                        Span.FromBounds(minPosition, maxPosition));
            lastTotalDiagnosticsSpan = changedSpan;

            if (TagsChanged != null)
                TagsChanged(this, new SnapshotSpanEventArgs(changedSpan));
        }


        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach(var spanAndErrorMessage in spansAndErrorMessages)
                yield return new TagSpan<ErrorTag>(spanAndErrorMessage.Item1, new ErrorTag("SyntaxError", spanAndErrorMessage.Item2));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
