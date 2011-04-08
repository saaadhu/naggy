// This is the main DLL file.

#include "stdafx.h"

#include "ClangAdapter.h"

#include "clang-c\Index.h"
#include "clang/Basic/Diagnostic.h"

using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;
using namespace NaggyClang;

const char* ToCString(String ^str)
{
	System::IntPtr ptr = Marshal::StringToHGlobalAnsi(str);
	return (const char *)ptr.ToPointer();
}

String^ ToManagedString(const CXString &str)
{
	const char* strData = clang_getCString(str);
	
	String^ managedString = Marshal::PtrToStringAnsi(System::IntPtr((int)strData));
	return managedString;
}

Diagnostic^ ToDiagnostic(const CXDiagnostic &diag)
{
	Diagnostic^ managedDiag = gcnew Diagnostic;
	managedDiag->Message = ToManagedString(clang_getDiagnosticSpelling(diag));

	
	CXSourceLocation start = clang_getDiagnosticLocation(diag);

	CXFile file; unsigned line, col, offset;
	clang_getInstantiationLocation(start, &file, &line, &col, &offset);
	// Ignore FILE for now
	managedDiag->StartLine = line;
	managedDiag->StartColumn = col;

	return managedDiag;
}

List<Diagnostic^>^ ClangAdapter::GetDiagnostics(String ^fileName)
{
	CXIndex idx = clang_createIndex(0, 1);
	const char* strFileName = ToCString(fileName);
	CXTranslationUnit tu = clang_createTranslationUnitFromSourceFile(idx, strFileName , 0, NULL, 0, NULL);

	unsigned int numDiagnostics = clang_getNumDiagnostics(tu);
	List<Diagnostic^>^ diagnostics = gcnew List<Diagnostic^>();	

	for (unsigned int i = 0; i<numDiagnostics; i++)
	{
		CXDiagnostic diag = clang_getDiagnostic(tu, i);
		Diagnostic ^managedDiag = ToDiagnostic(diag);
		diagnostics->Add(managedDiag);
	}

	return diagnostics;
}