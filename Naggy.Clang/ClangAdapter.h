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
		ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols, bool isC99Enabled);

		List<Diagnostic^> ^GetDiagnostics();
		PreprocessorAdapter^ GetPreprocessor();

		void Process(String ^contents);
	private:
		void Initialize(String ^fileName, List<String^> ^includePaths, List<String^>^ symbols, bool isC99Enabled);

		void InitializeInvocation(clang::CompilerInvocation *pInvocation);
		void CreateClangCompiler();
		void DestroyClangCompiler();

	private:
		clang::CompilerInstance * m_pInstance;
		StoredDiagnosticClient *m_pDiagnosticClient;
		char* m_filePath;
		List<String^> ^includePaths;
		List<String^> ^predefinedSymbols;
		PreprocessorAdapter ^m_preprocessorAdapter;
		bool isC99Enabled;
	};
}
