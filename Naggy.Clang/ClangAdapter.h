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
	public:
		ClangAdapter(String^ fileName);
		ClangAdapter(String ^fileName, List<String^> ^includePaths);
		ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols);

		List<Diagnostic^> ^GetDiagnostics();
		PreprocessorAdapter^ GetPreprocessor();

		void Process(String ^contents);
	private:
		void Process();
		void Initialize(String ^fileName, List<String^> ^includePaths, List<String^>^ symbols);

	private:
		clang::CompilerInvocation* m_pInvocation;
		clang::CompilerInstance * m_pInstance;
		StoredDiagnosticClient *m_pDiagnosticClient;
		char* m_filePath;
		PreprocessorAdapter ^m_preprocessorAdapter;
	};
}
