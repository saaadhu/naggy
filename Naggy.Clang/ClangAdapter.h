// Naggy.Clang.h

#pragma once

using namespace System;
using namespace System::Collections::Generic;
#include <vector>

namespace clang
{
	class CompilerInstance;
	class CompilerInvocation;
}

class StoredDiagnosticClient;

namespace NaggyClang {
	ref class PreprocessorAdapter;

	public enum class DiagnosticLevel
	{
		Error,
		Warning
	};

	public enum class Language
	{
		C,
		C99,
		Cpp,
		Cpp11
	};

	public ref class Diagnostic
	{
	public:
		int ID;
		String ^Message;
		String ^FilePath;
		int StartLine;
		int StartColumn;
		int EndLine;
		int EndColumn;
		DiagnosticLevel Level;
	};

	public ref class ClangAdapter
	{
	public:
		ClangAdapter(String^ fileName);
		ClangAdapter(String ^fileName, List<String^> ^includePaths);
		ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols);
		ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols, Language language);

		List<Diagnostic^> ^GetDiagnostics();
		PreprocessorAdapter^ GetPreprocessor();

		void Process(String ^contents);
	private:
		void Initialize(String ^fileName, List<String^> ^includePaths, List<String^>^ symbols, Language language);

		void InitializeInvocation(clang::CompilerInvocation *pInvocation);
		void CreateClangCompiler();
		void DestroyClangCompiler();
		List<Diagnostic^> ^ComputeDiagnostics();

	private:
		clang::CompilerInstance * m_pInstance;
		StoredDiagnosticClient *m_pDiagnosticClient;
		char* m_filePath;
		List<Diagnostic^> ^diagnostics;
		List<String^> ^includePaths;
		List<String^> ^predefinedSymbols;
		PreprocessorAdapter ^m_preprocessorAdapter;
		bool isC99Enabled;
		bool isCPP;
		bool isCPP11;
	};
}
