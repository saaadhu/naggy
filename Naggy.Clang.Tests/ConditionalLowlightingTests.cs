using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaggyClang;
using System.IO;

namespace Naggy.Clang.Tests
{
    [TestClass]
    public class ConditionalLowlightingTests
    {
        string sourceFilePath;

        [TestMethod]
        public void GetSkippedBlocks_EmptyFile_NoSkippedBlocksReported()
        {
            File.WriteAllText(sourceFilePath, 
@"");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new Tuple<int, int>[] { });
        }

        [TestMethod]
        public void GetSkippedBlocks_UnmatchedExcludedIf_EverythingAfterIfReportedAsSkippedBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"int nothing = 0;
#if 0
#define blah 0
#define blah 2
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(3, 0) });
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
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(2,2)});
        }

        [TestMethod]
        public void GetSkippedBlocks_NestedIfDefDirectiveWithOuterBlockSkipped_SkippedBlockIncludesEntireOuterBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 0
#if 0
#define foo x*y
#elif 0
#define foo x/y
#else
#define blah x-y
#endif
#define blah
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(2, 9)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfNDefDirectiveWithDefinition_SkippedBlockIncludesElseBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#define HUHU 1
#ifndef HUHU
#define foo x*y
#else
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(3, 3)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDirectiveWithoutDefinition_SkippedBlockIncludesIfBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 2 - 2 
#define foo x*y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(2, 2)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDirectiveWithElseDirectiveEvaluationFalse_SkippedBlockIncludesIfBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 2 - 2 
#define foo x*y
#else
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(2, 2)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDirectiveWithElseDirectiveEvaluationTrue_SkippedBlockIncludesElseBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 2 
#define foo x*y
#else
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(4, 4)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDirectiveWithElifAndElseDirectiveEvaluationsTrue_SkippedBlockIncludesElseBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 0 
#define foo x*y
#elif 2
#define foo x+y
#else
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(2, 2), Tuple.Create(6, 6)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfSingleElifDirectiveWithIfDirectiveFalse_SkippedBlockIncludesIfBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#define X 0
#define Y 1
#if X 
#define foo x*y
#elif Y
#define foo x/y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(4, 4)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfMultipleElifDirectiveWithIfDirectiveFalseAndFirstElifDirectiveTrue_SkippedBlockIncludesFirstElifBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#define X 0
#define Y 1
#define Z 2
#if X 
#define foo x*y
#elif Y
#define foo x/y
#elif Z
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(5, 5), Tuple.Create(9, 9)});
        }

        [TestMethod]
        public void GetSkippedBlocks_IfMultipleElifDirectiveWithIfDirectiveFalseAndSecondElifDirectiveTrue_SkippedBlockIncludesSecondElifBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#define X 0
#define Y 0
#define Z 2
#if X 
#define foo x*y
#elif Y
#define foo x/y
#elif Z
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();
                var skippedBlock = skippedBlocks.First();
                Assert.AreEqual(5, skippedBlock.Item1);
                Assert.AreEqual(5, skippedBlock.Item2);

                skippedBlock = skippedBlocks.ElementAt(1);
                Assert.AreEqual(7, skippedBlock.Item1);
                Assert.AreEqual(7, skippedBlock.Item2);
            }
        }

        [TestMethod]
        public void GetSkippedBlocks_IfSingleElifDirectiveWithElifDirectiveFalse_SkippedBlockIncludesElifBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#define X 1
#define Y 0
#define Z 0
#if X 
#define foo x*y
#elif Z + 0
#define foo x+y
#elif Y
#define foo x/y
#elif Z
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();

                var skippedBlock = skippedBlocks.First();
                Assert.AreEqual(7, skippedBlock.Item1);
                Assert.AreEqual(7, skippedBlock.Item2);

                skippedBlock = skippedBlocks.ElementAt(1);
                Assert.AreEqual(9, skippedBlock.Item1);
                Assert.AreEqual(9, skippedBlock.Item2);

                skippedBlock = skippedBlocks.ElementAt(2);
                Assert.AreEqual(11, skippedBlock.Item1);
                Assert.AreEqual(11, skippedBlock.Item2);
            }
        }

        [TestMethod]
        public void GetSkippedBlocks_IfBlockWithMultiplePreprocessorDefinitionsIsTrue_SkippedBlockIsEmpty()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 1
#define foo x+y
#define fat boo
#define fal laf
#endif
int x = 20;
#if 1
#define foo x+y
#define fat boo
#define fal laf
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();
                Assert.IsFalse(skippedBlocks.Any());
            }
        }

        public void GetSkippedBlocks_IfElseBlockWithElseBlockExcludedAndCodeTypedBeforeBlock_SkippedBlockIncludesExcludedCode()
        {
            var mainCode =
@"#if BLAH
#define foo x+y
#define bar x-y
#else
#define fal laf
#endif";
            File.WriteAllText(sourceFilePath, mainCode);
            var adapter = new ClangAdapter(sourceFilePath);
            
            var preprocessor = adapter.GetPreprocessor();
            adapter.Process(mainCode);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(3, 4) });

            var initialText = "#define BLAH 0" + Environment.NewLine + mainCode;
            adapter.Process(initialText);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(3, 4) });

            var laterText = "#define BLAH 2" + Environment.NewLine + mainCode;
            adapter.Process(laterText);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(6, 6) });

            adapter.Process(initialText);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(3, 4) });

            adapter.Process(laterText);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(6, 6) });

            adapter.Process(initialText);
            AssertSkippedBlocksMatch(adapter, new[] { Tuple.Create(3, 4) });
        }

        [TestMethod]
        public void GetSkippedBlocks_IfBlockWithMultiplePreprocessorDefinitions_SkippedBlockIncludesAllLines()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 0
#define foo x+y
#define fat boo
#define fal laf
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();
                var skippedBlock = skippedBlocks.First();
                Assert.AreEqual(2, skippedBlock.Item1);
                Assert.AreEqual(4, skippedBlock.Item2);
            }
        }

        [TestMethod]
        public void GetSkippedBlocks_TwoIfBlocksBothFalse_SkippedBlockIncludesBothBlocks()
        {
            File.WriteAllText(sourceFilePath, 
@"#if 0
#define foo x+y
#endif
#if 0
#define foo x-y
#endif
");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();
                var skippedBlock = skippedBlocks.First();
                Assert.AreEqual(2, skippedBlock.Item1);
                Assert.AreEqual(2, skippedBlock.Item2);

                skippedBlock = skippedBlocks.ElementAt(1);
                Assert.AreEqual(5, skippedBlock.Item1);
                Assert.AreEqual(5, skippedBlock.Item2);
            }
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDefinedBlockWithUndefinedMacro_SkippedBlockIncludesIfDefinedBlock()
        {
            File.WriteAllText(sourceFilePath, 
@"#if defined(OOLALALA)
#  include <avr/io.h>
#endif");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();
                var skippedBlock = skippedBlocks.First();
                Assert.AreEqual(2, skippedBlock.Item1);
                Assert.AreEqual(2, skippedBlock.Item2);
            }
        }

        [TestMethod]
        public void GetSkippedBlocks_IfDefinedBlockWithDefinedMacro_SkippedBlockIncludesIfDefinedBlock()
        {
            File.WriteAllText(sourceFilePath,
@"#if defined(__GNUC__)
#  include <avr/io.h>
#elif defined(__ICCAVR__)
#  include <ioavr.h>
#  include <intrinsics.h>
#else
#  error Unsupported compiler.
#endif");
            var adapter = new ClangAdapter(sourceFilePath);
            adapter.Process(null);
            var preprocessor = adapter.GetPreprocessor();
            {
                var skippedBlocks = preprocessor.GetSkippedBlockLineNumbers();
                var skippedBlock = skippedBlocks.First();
                Assert.AreEqual(4, skippedBlock.Item1);
                Assert.AreEqual(5, skippedBlock.Item2);

                skippedBlock = skippedBlocks.ElementAt(1);
                Assert.AreEqual(7, skippedBlock.Item1);
                Assert.AreEqual(7, skippedBlock.Item2);

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

        void AssertSkippedBlocksMatch(ClangAdapter adapter, IEnumerable<Tuple<int, int>> expected)
        {
            var preprocessor = adapter.GetPreprocessor();
            var skippedBlockLineNumbers = preprocessor.GetSkippedBlockLineNumbers();

            Assert.AreEqual(expected.Count(), skippedBlockLineNumbers.Count());

            for (int i = 0; i < expected.Count(); ++i)
            {
                Assert.AreEqual(expected.ElementAt(i), skippedBlockLineNumbers.ElementAt(i));
            }
        }
    }
}
