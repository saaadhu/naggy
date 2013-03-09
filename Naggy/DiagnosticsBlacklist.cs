using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using NaggyClang;

namespace Naggy
{
    static class DiagnosticsBlacklist
    {
        private static List<int> blacklistedDiagIds;
        public static void Initialize()
        {
            if (blacklistedDiagIds != null)
                return;

            blacklistedDiagIds = new List<int>();
            var extensionManager = (IVsExtensionManager) Package.GetGlobalService(typeof (SVsExtensionManager));
            var blacklistFilePath = extensionManager.GetEnabledExtensionContentLocations("DiagnosticsBlacklist").First();
            LoadBlacklistedDiags(blacklistFilePath);
        }

        private static void LoadBlacklistedDiags(string blacklistFilePath)
        {
            foreach (var line in File.ReadLines(blacklistFilePath))
            {
                int id;
                if (int.TryParse(line, out id))
                    blacklistedDiagIds.Add(id);
            }
        }

        public static bool Contains(Diagnostic diag)
        {
            return blacklistedDiagIds.Exists(id => id == diag.ID);
        }
    }
}
