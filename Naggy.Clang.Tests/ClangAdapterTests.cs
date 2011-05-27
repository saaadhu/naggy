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
        public void GetDiagnostics_IntFunctionNotReturningAValue_DiagnosticDetailsAreCorrect()
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

        [TestMethod]
        public void GetDiagnostics_SymbolInSourceCodeProvidedInPredefinedSymbolList_NoDiagnosticsReturned()
        {
            File.WriteAllText(sourceFilePath, @"int main() { return FOO; }");
            var adapter = new ClangAdapter(sourceFilePath, new List<string>(), new List<string>() { "FOO=2" });
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        [Ignore]
        public void GetDiagnostics_MisspelledMemberName_DiagnosticIncludesSuggestedMember()
        {
            File.WriteAllText(sourceFilePath, @"struct A { int Foo; }; int main() { struct A a; a.Fo = 2; }");
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics();

            StringAssert.Contains(diags.First().Message, "did you mean 'Foo'?");
        }

        [TestMethod]
        public void ExpandMacro_MacroDefinitionIncludesAnotherMacro_ExpansionExpandsInnerMacro()
        {
            File.WriteAllText(sourceFilePath, @"
#define x 2
#define foo x*y
");
            var adapter = new ClangAdapter(sourceFilePath);
            using (var preprocessor = adapter.GetPreprocessor())
            {
                Assert.AreEqual("2*y", preprocessor.ExpandMacro("foo"));
            }
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDefDirectiveWithoutDefinition_SkippedBlockIncludesIfdefBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#ifdef x
#define foo x*y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            using (var preprocessor = adapter.GetPreprocessor())
            {
                var skippedBlock = preprocessor.GetSkippedBlockLineNumbers().Single();
                Assert.AreEqual(1, skippedBlock.Item1);
                Assert.AreEqual(3, skippedBlock.Item2);
            }
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
