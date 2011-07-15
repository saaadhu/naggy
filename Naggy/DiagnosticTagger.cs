using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using NaggyClang;
using System.IO;
using EnvDTE;

namespace Naggy
{
    class DiagnosticTagger : ITagger<ErrorTag>
    {
        private readonly ITextBuffer buffer;
        private readonly DTE dte;
        ITextDocument document;

        readonly List<Tuple<SnapshotSpan, string>> spansAndErrorMessages = new List<Tuple<SnapshotSpan, string>>();
        DelayedRequestExecutor<int> debouncer = new DelayedRequestExecutor<int>(1200);

        public DiagnosticTagger(DTE dte, ITextBuffer buffer)
        {
            this.dte = dte;
            this.buffer = buffer;
            this.buffer.Changed += new EventHandler<TextContentChangedEventArgs>(buffer_Changed);
            buffer.Properties.TryGetProperty(typeof(ITextDocument), out document);
            debouncer.Add(0, FindDiagnostics);
        }

        private void buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            debouncer.Add(0, FindDiagnostics);
        }

        private SnapshotSpan lastTotalDiagnosticsSpan;

        private void FindDiagnostics()
        {
            spansAndErrorMessages.Clear();

            int minPosition = buffer.CurrentSnapshot.Length;
            int maxPosition = 0;

            ClangServices.Process(buffer);
            foreach (var diagnostic in ClangServices.GetDiagnostics(buffer))
            {
                if (!diagnostic.FilePath.Any(c => Path.GetInvalidPathChars().Contains(c)))
                {
                    // Crude check, should find a more sophisticated way to check if two paths are equal, ignoring different directory separator chars.
                    if (Path.GetFileName(diagnostic.FilePath) == Path.GetFileName(document.FilePath))
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
                }
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
