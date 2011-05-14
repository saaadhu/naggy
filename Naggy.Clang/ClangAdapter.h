// Naggy.Clang.h

#pragma once

#include "clang-c\Index.h"
#include "PreprocessorAdapter.h"
using namespace System;
using namespace System::Collections::Generic;

namespace NaggyClang {

	public ref class Diagnostic
	{
	public:
		String ^Message;
		String ^FilePath;
		int StartLine;
		int StartColumn;
		int EndLine;
		int EndColumn;
	};

	public ref class ClangAdapter
	{
		CXTranslationUnit m_translationUnit;
		char* m_filePath;
	public:
		ClangAdapter(String ^fileName) {Initialize(fileName, gcnew List<String^>(), gcnew List<String^>());}
		ClangAdapter(String ^fileName, List<String^> ^includePaths) { Initialize(fileName, includePaths, gcnew List<String^>()); }
		ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols) { Initialize(fileName, includePaths, symbols); }
		List<Diagnostic^> ^GetDiagnostics(String ^contents);
		List<Diagnostic^> ^GetDiagnostics();
		PreprocessorAdapter^ GetPreprocessor();

	private:
		void Initialize(String ^fileName, List<String^> ^includePaths, List<String^>^ symbols);
	};
}
