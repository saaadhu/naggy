using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Naggy
{
    static class NaggyTriggers
    {
        class TriggerActions
        {
            public List<Action> Delayed = new List<Action>();
            public List<Action> Disabled = new List<Action>();

            public void ExecuteDelayedActions()
            {
                foreach (var delayedAction in Delayed)
                    delayedAction();
            }

            public void ExecuteDisabledActions()
            {
                foreach (var disabledAction in Disabled)
                    disabledAction();
            }
        }

        static DTE dte;
        static DocumentEvents documentEvents;
        static WindowEvents windowEvents;
        static Dictionary<ITextBuffer, TriggerActions> buffersAndActions = new Dictionary<ITextBuffer, TriggerActions>();
        static SolutionEventsHandler solutionEventsHandler;

        public static void Initialize(DTE d)
        {
            if (dte != null)
                return;

            dte = d;
            documentEvents = dte.Events.DocumentEvents;
            windowEvents = dte.Events.WindowEvents;
            documentEvents.DocumentSaved += DocumentEventsOnDocumentSaved;
            documentEvents.DocumentClosing += DocumentEventsOnDocumentClosing;
            windowEvents.WindowActivated += windowEvents_WindowActivated;
            solutionEventsHandler = new SolutionEventsHandler();
            solutionEventsHandler.ActiveProjectConfigurationChanged += ActiveProjectConfigurationChanged;

            DiagnosticsFinder.Toggled += new EventHandler<EventArgs>(DiagnosticsFinder_Toggled);
        }

        private static void ActiveProjectConfigurationChanged(object sender, EventArgs e)
        {
            foreach (var buffer in buffersAndActions.Keys)
            {
                ClangServices.ClearCache(buffer);
            }

            var activeBuffer = GetBufferForDocument(dte.ActiveDocument);

            if (activeBuffer != null)
            {
                buffersAndActions[activeBuffer].ExecuteDelayedActions();
            }
        }

        static void DiagnosticsFinder_Toggled(object sender, EventArgs e)
        {
            foreach (var t in buffersAndActions)
            {
                if (DiagnosticsFinder.Enabled)
                {
                    foreach (var buffer in buffersAndActions.Keys)
                    {
                        ClangServices.ClearCache(buffer);
                    }

                    t.Value.ExecuteDelayedActions();
                }
                else
                {
                    t.Value.ExecuteDisabledActions();
                }
            }
        }

        public static void Register(ITextBuffer buffer, Action delayed, Action disabled)
        {
            TriggerActions triggerActions;
            if (buffersAndActions.TryGetValue(buffer, out triggerActions))
            {
                triggerActions.Delayed.Add(delayed);
                triggerActions.Disabled.Add(disabled);
            }
            else
            {
                var t = new TriggerActions();
                t.Delayed.Add (delayed);
                t.Disabled.Add(disabled);
                buffersAndActions[buffer] = t;
            }
            buffer.Changed += new EventHandler<TextContentChangedEventArgs>(buffer_Changed);
        }

        static void buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            var buffer = buffersAndActions[e.Before.TextBuffer];
            buffer.ExecuteDelayedActions();
        }

        private static bool IsDocumentBuffer(ITextBuffer b, Document d)
        {
            ITextDocument thisDocument;
            if (!b.Properties.TryGetProperty(typeof(ITextDocument), out thisDocument) || thisDocument == null)
                return false;

            return thisDocument.FilePath == d.FullName;
        }

        static ITextBuffer GetBufferForDocument(Document d)
        {
            if (d == null)
                return null;

            return buffersAndActions
                .Select(kv => kv.Key)
                .Where(b => IsDocumentBuffer(b, d))
                .FirstOrDefault();
        }

        static void windowEvents_WindowActivated(Window GotFocus, Window LostFocus)
        {
            if (GotFocus.Document == null)
                return;

            var buffer = GetBufferForDocument(GotFocus.Document);

            if (buffer == null)
                return;

            ClangServices.ClearCache(buffer);
            buffersAndActions[buffer].ExecuteDelayedActions();
        }

        static void DocumentEventsOnDocumentClosing(Document document)
        {
            var buffer = GetBufferForDocument(document);
            if (buffer == null)
                return;

            buffersAndActions.Remove(buffer);
        }

        static void DocumentEventsOnDocumentSaved(Document document)
        {
            var buffer = GetBufferForDocument(document);
            if (buffer == null)
                return;

            buffersAndActions[buffer].ExecuteDelayedActions();
        }
    }

    class SolutionEventsHandler : IVsUpdateSolutionEvents
    {
        private uint useCookie;
        public event EventHandler<EventArgs> ActiveProjectConfigurationChanged;

        public SolutionEventsHandler()
        {
            IVsSolutionBuildManager vsSlnBldMgr = (IVsSolutionBuildManager)Package.GetGlobalService(typeof(SVsSolutionBuildManager));
            vsSlnBldMgr.AdviseUpdateSolutionEvents(this, out useCookie);
        }
        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            if (ActiveProjectConfigurationChanged != null)
                ActiveProjectConfigurationChanged(this, new EventArgs());
            return 0;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return 0;
        }

        public int UpdateSolution_Cancel()
        {
            return 0;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return 0;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return 0;
        }
    }
}
