using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
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
        private DocumentEvents documentEvents;

        readonly List<Tuple<SnapshotSpan, Diagnostic>> spansAndDiagnostics = new List<Tuple<SnapshotSpan, Diagnostic>>();
        DelayedRequestExecutor<int> debouncer = new DelayedRequestExecutor<int>(1200);

        public DiagnosticTagger(DTE dte, ITextBuffer buffer)
        {
            this.dte = dte;
            this.documentEvents = dte.Events.DocumentEvents;
            this.buffer = buffer;
            this.buffer.Changed += new EventHandler<TextContentChangedEventArgs>(buffer_Changed);
            this.documentEvents.DocumentSaved += DocumentEventsOnDocumentSaved;
            this.documentEvents.DocumentClosing += DocumentEventsOnDocumentClosing;
            debouncer.Add(0, FindDiagnostics);
        }

        private void DocumentEventsOnDocumentClosing(Document document)
        {
            ITextDocument thisDocument;
            if (!buffer.Properties.TryGetProperty(typeof(ITextDocument), out thisDocument) || thisDocument == null)
                return;

            if (document.FullName == thisDocument.FilePath)
            {
                this.documentEvents.DocumentSaved -= DocumentEventsOnDocumentSaved;
                this.documentEvents.DocumentClosing -= DocumentEventsOnDocumentClosing;
                this.documentEvents = null;
            }
        }

        private void DocumentEventsOnDocumentSaved(Document document)
        {
            debouncer.AddAndRunImmediately(0, FindDiagnostics);
        }

        private void buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            debouncer.Add(0, FindDiagnostics);
        }

        private SnapshotSpan lastTotalDiagnosticsSpan;

        private void FindDiagnostics()
        {
            try
            {
                spansAndDiagnostics.Clear();

                int minPosition = buffer.CurrentSnapshot.Length;
                int maxPosition = 0;

                foreach (var diagnostic in DiagnosticsFinder.Find(buffer))
                {
                    if (diagnostic.StartLine != 0)
                        diagnostic.StartLine--;

                    var textLine = buffer.CurrentSnapshot.GetLineFromLineNumber(diagnostic.StartLine);
                    var startPosition = textLine.Start.Position;
                    var endPosition = textLine.End.Position;

                    minPosition = Math.Min(minPosition, startPosition);
                    maxPosition = Math.Max(maxPosition, endPosition);

                    SnapshotSpan span = new SnapshotSpan(buffer.CurrentSnapshot,
                                                         Span.FromBounds(startPosition, endPosition));
                    spansAndDiagnostics.Add(Tuple.Create(span, diagnostic));
                }

                if (spansAndDiagnostics.Any())
                {
                    RaiseTagsChanged(minPosition, maxPosition);
                }
                else
                {
                    if (TagsChanged != null && !lastTotalDiagnosticsSpan.IsEmpty)
                        TagsChanged(this, new SnapshotSpanEventArgs(lastTotalDiagnosticsSpan));
                }
            }
            catch (Exception e)
            {
                dte.StatusBar.Text = "Naggy Error: " + e.Message;
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
            foreach(var spanAndDiagnostic in spansAndDiagnostics)
                yield return new TagSpan<ErrorTag>(spanAndDiagnostic.Item1,
                    new ErrorTag(
                        GetErrorType(spanAndDiagnostic.Item2),
                        spanAndDiagnostic.Item2.Message));
        }

        private string GetErrorType(Diagnostic diag)
        {
            switch(diag.Level)
            {
                case DiagnosticLevel.Error:
                    return PredefinedErrorTypeNames.SyntaxError;
                case DiagnosticLevel.Warning:
                    return PredefinedErrorTypeNames.Warning;
                default:
                    throw new InvalidOperationException("Unknown diag level: " + diag.Level.ToString());
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
