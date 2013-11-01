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
        string cppSourceFilePath;

        [TestMethod]
        public void GetDiagnostics_EmptyCFile_NoDiagnosticsReturned()
        {
            File.WriteAllText(sourceFilePath, "");
            var adapter = new ClangAdapter(sourceFilePath);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_EmptyCppFile_NoDiagnosticsReturned()
        {
            File.WriteAllText(cppSourceFilePath, "");
            var adapter = new ClangAdapter(cppSourceFilePath, new List<string>(), new List<string>(), Language.Cpp);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskWithOneWarning_OneDiagnosticsReturned()
        {
            var sourceText =  "int func(){}";
            File.WriteAllText(sourceFilePath, sourceText);
            var adapter = new ClangAdapter(sourceFilePath);

            adapter.Process(null);
            
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(1, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CppFileInDiskWithOneWarning_OneDiagnosticsReturned()
        {
            var sourceText =  "int func(){}";
            File.WriteAllText(cppSourceFilePath, sourceText);
            var adapter = new ClangAdapter(cppSourceFilePath, new List<string>(), new List<string>(), Language.Cpp);

            adapter.Process(null);
            
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(1, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskHasNoWarningsTextHasOneWarning_OneDiagnosticsReturned()
        {
            var sourceInFile =  "int func(){ return 0; }";
            var sourceInEditor =  "int func(){ }";

            File.WriteAllText(sourceFilePath, sourceInFile);
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(sourceInEditor);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(1, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskHasPreprocessorCodeAndNoWarningsTextHasOneWarning_OneDiagnosticsReturned()
        {
            var sourceInFile =
@"#if defined(__GNUC__)
    int x = 20;
#elif defined (__ICCAVR__)
    int x = 30;
    int y = 20;
#else
#error Unsupported compiler
#endif
";
            var sourceInEditor =  sourceInFile + 
@"int func(){ }

int main(){}
";

            File.WriteAllText(sourceFilePath, sourceInFile);
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(sourceInFile);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);

            adapter.Process(sourceInEditor);
            diags = adapter.GetDiagnostics();

            Assert.AreEqual(1, diags.Count);
            Assert.AreEqual(9, diags.First().StartLine);
        }

        [TestMethod]
        public void GetDiagnostics_CFileInDiskHasOneWarningTextHasNoWarning_OneDiagnosticsReturned()
        {
            var sourceInEditor =  "int func(){ return 0; }";
            var sourceInFile =  "int func(){}";

            File.WriteAllText(sourceFilePath, sourceInFile);
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(sourceInEditor);
            var diags = adapter.GetDiagnostics();

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
            var diags = adapter.GetDiagnostics();
            Assert.AreEqual(0, diags.Count);

            adapter.Process(initialSourceInEditor);
            diags = adapter.GetDiagnostics();
            Assert.AreEqual(1, diags.Count);

            adapter.Process(currentSourceInEditor);
            diags = adapter.GetDiagnostics();
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
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diag = adapter.GetDiagnostics().Single();
            Assert.AreEqual(sourceFilePath, diag.FilePath);
            Assert.AreEqual(4, diag.StartLine);
            Assert.AreEqual(1, diag.StartColumn);
        }

        [TestMethod]
        public void GetDiagnostics_ClassDeclarationInCpp_NoDiagnosticsReturned()
        {
            File.WriteAllText(cppSourceFilePath, @"
class C{};
");
            var adapter = new ClangAdapter(cppSourceFilePath, new List<string>(), new List<string>(), Language.Cpp);
            adapter.Process(null);
            Assert.AreEqual(0, adapter.GetDiagnostics().Count());
        }

        [TestMethod]
        public void GetDiagnostics_SymbolInSourceCodeProvidedInPredefinedSymbolList_NoDiagnosticsReturned()
        {
            File.WriteAllText(sourceFilePath, @"int main() { return FOO; }");
            var adapter = new ClangAdapter(sourceFilePath, new List<string>(), new List<string>() { "FOO=2" });
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_C99StyleSyntaxUsedWithC99TurnedOn_NoDiagnosticsReturned()
        {
            File.WriteAllText(sourceFilePath, @"int main() { for (int i = 0; i < 10; i++){} for (int i = 0; i < 10; i++){} return 0;}");
            var adapter = new ClangAdapter(sourceFilePath, new List<string>(), new List<string>(), Language.C99);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        public void GetDiagnostics_Cpp11StyleSyntaxUsedWithCpp11TurnedOn_NoDiagnosticsReturned()
        {
            File.WriteAllText(cppSourceFilePath,
                              @"auto it = 4;");
            var adapter = new ClangAdapter(cppSourceFilePath, new List<string>(), new List<string>(), Language.Cpp11);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count);
        }

        [TestMethod]
        [Ignore]
        public void GetDiagnostics_MisspelledMemberName_DiagnosticIncludesSuggestedMember()
        {
            File.WriteAllText(sourceFilePath, @"struct A { int Foo; }; int main() { struct A a; a.Fo = 2; }");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            StringAssert.Contains(diags.First().Message, "did you mean 'Foo'?");
        }

        [TestMethod]
        public void GetDiagnostics_InlineKeyword_NoDiagnosticsReported()
        {
            File.WriteAllText(sourceFilePath, @"static inline void Foo(){}");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count());
        }

        [TestMethod]
        public void GetDiagnostics_asmExtension_NoDiagnosticsReported()
        {
            File.WriteAllText(sourceFilePath, @" #define barrier()  asm volatile("""" ::: ""memory"")
   int main() { barrier(); }");

            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count());
        }

        [TestMethod]
        public void GetDiagnostics_FunctionWithNoReturnType_WarningReported()
        {
            File.WriteAllText(sourceFilePath, @"func() { return 0; }");

            var adapter = new ClangAdapter(sourceFilePath, new List<string>(), new List<string>(), Language.C99);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics().ToList();

            Assert.AreEqual(1, diags.Count());
            var diag = diags[0];

            Assert.AreEqual(DiagnosticLevel.Warning, diag.Level);
        }

        [TestMethod]
        public void GetDiagnostics_UnknownAttribute_WarningReported()
        {
            File.WriteAllText(sourceFilePath, @"__attribute__((__interrupt__)) void rtc() {}");

            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics().ToList();

            Assert.AreEqual(1, diags.Count());
            var diag = diags[0];

            Assert.AreEqual(DiagnosticLevel.Warning, diag.Level);
        }
        [TestMethod]
        public void GetDiagnostics_boolKeyword_NoDiagnosticsReported()
        {
            File.WriteAllText(sourceFilePath, @" bool test = true;");

            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count());
        }

        [TestMethod]
        public void GetDiagnostics_SingleLineCommentWithAsterisk_NoDiagnosticsReported()
        {
            File.WriteAllText(sourceFilePath, @"
//*
int main() { return 0; }
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var diags = adapter.GetDiagnostics();

            Assert.AreEqual(0, diags.Count());
        }

        [TestMethod]
        public void ExpandMacro_MacroDefinitionIncludesAnotherMacro_ExpansionExpandsInnerMacro()
        {
            File.WriteAllText(sourceFilePath, @"
#define x 2
#define foo x*y
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                Assert.AreEqual("2*y", preprocessor.ExpandMacro("foo"));
            }
        }

        
        [TestInitialize]
        public void Setup()
        {
            sourceFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".c");
            cppSourceFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".cpp");
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(sourceFilePath);
            File.Delete(cppSourceFilePath);
        }
    }
}
