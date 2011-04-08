// Naggy.Clang.h

#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace NaggyClang {

	public ref class Diagnostic
	{
	public:
		String ^Message;
		String ^FileName;
		int StartLine;
		int StartColumn;
		int EndLine;
		int EndColumn;
	};

	public ref class ClangAdapter
	{
	public:
		List<Diagnostic^> ^GetDiagnostics(String ^fileName);
	};
}
