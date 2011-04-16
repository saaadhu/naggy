using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using NaggyClang;

namespace Naggy.Clang.Tests
{
    [TestClass]
    public class ClangAdapterTests
    {
        string sourceFilePath;

        [TestMethod]
        public void GetDiagnostics_EmptyCFile_NoDiagnosticsReturned()
        {
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics(string.Empty);

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskWithOneWarning_OneDiagnosticsReturned()
        {
            var sourceText =  "int func(){}";
            File.WriteAllText(sourceFilePath, sourceText);
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics(sourceText);

            Assert.AreEqual(1, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskHasNoWarningsTextHasOneWarning_OneDiagnosticsReturned()
        {
            var sourceInFile =  "int func(){ return 0; }";
            var sourceInEditor =  "int func(){ }";

            File.WriteAllText(sourceFilePath, sourceInFile);
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics(sourceInEditor);

            Assert.AreEqual(1, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskHasOneWarningTextHasNoWarning_OneDiagnosticsReturned()
        {
            var sourceInEditor =  "int func(){ return 0; }";
            var sourceInFile =  "int func(){}";

            File.WriteAllText(sourceFilePath, sourceInFile);
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics(sourceInEditor);

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_WarningMadeAndCorrectedInEditor_ZeroDiagnosticsReturned()
        {
            var initialSourceInEditor =  "int func(){ }";
            var currentSourceInEditor =  "int func(){ return 0; }";
            var sourceInFile =  "";

            File.WriteAllText(sourceFilePath, sourceInFile);
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics(initialSourceInEditor);
            Assert.AreEqual(1, diags.Count);

            diags = adapter.GetDiagnostics(currentSourceInEditor);
            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_InvalidDereference_DiagnosticDetailsAreCorrect()
        {
            File.WriteAllText(sourceFilePath, @"
/* Some source file */
int fun() {
}
");
            var diag = new ClangAdapter(sourceFilePath).GetDiagnostics().Single();
            Assert.AreEqual(sourceFilePath, diag.FilePath);
            Assert.AreEqual(4, diag.StartLine);
            Assert.AreEqual(1, diag.StartColumn);
        }

        [TestInitialize]
        public void Setup()
        {
            sourceFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".c");
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(sourceFilePath);
        }
    }
}
