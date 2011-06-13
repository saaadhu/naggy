// This is the main DLL file.

#include "stdafx.h"
#include "ClangAdapter.h"
#include "StringUtilities.h"
#include "PreprocessorAdapter.h"
#include "clang\Lex\Preprocessor.h"
#include "clang\Frontend\ASTUnit.h"
#include "clang\Frontend\CompilerInvocation.h"
#include "clang\Frontend\FrontendAction.h"
#include "clang\Frontend\FrontendActions.h"
#include "clang\Frontend\CompilerInstance.h"
#include <vector>
#include <vcclr.h>

using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;
using namespace NaggyClang;
using namespace std;

class StoredDiagnosticClient : public clang::DiagnosticClient
{
	std::vector<clang::StoredDiagnostic> diags;

public:
	virtual void HandleDiagnostic(clang::Diagnostic::Level DiagLevel,
		const clang::DiagnosticInfo &Info)
	{
		diags.push_back(clang::StoredDiagnostic(DiagLevel, Info));
	}

	void clear() { diags.clear(); }

	unsigned int size() { return diags.size(); }
	clang::StoredDiagnostic& getDiagInfo(unsigned int elementIndex)
	{
		return diags[elementIndex];
	}
};

static NaggyClang::Diagnostic^ ToDiagnostic(clang::StoredDiagnostic& diag)
{
	Diagnostic^ managedDiag = gcnew Diagnostic;
	managedDiag->Message = ToManagedString(diag.getMessage().data());

	const clang::FullSourceLoc &start = diag.getLocation();

	const clang::SourceManager &sourceManager = start.getManager();
	clang::PresumedLoc &loc = sourceManager.getPresumedLoc(start);

	managedDiag->StartLine = loc.getLine();
	managedDiag->StartColumn = loc.getColumn();
	managedDiag->FilePath = ToManagedString(loc.getFilename());

	return managedDiag;
}

ClangAdapter::ClangAdapter(String^ fileName)
{
	Initialize(fileName, gcnew List<String^>(), gcnew List<String^>());
}

ClangAdapter::ClangAdapter(String ^fileName, List<String^> ^includePaths)
{ 
	Initialize(fileName, includePaths, gcnew List<String^>()); 
}

ClangAdapter:: ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols)
{ 
	Initialize(fileName, includePaths, symbols); 
}

void ClangAdapter::Process(String ^contents)
{
	const char* pContents = ToCString(contents);

	m_pInvocation->getPreprocessorOpts().addRemappedFile(m_filePath, llvm::MemoryBuffer::getMemBufferCopy(pContents));

	Process();

	m_pInvocation->getPreprocessorOpts().clearRemappedFiles();
	Marshal::FreeHGlobal(IntPtr((void *)pContents));
}

class PreprocessorBlockCaptureAction : public clang::SyntaxOnlyAction
{
public:
	gcroot<PreprocessorAdapter^> preprocessorAdapter;

	virtual bool BeginSourceFileAction(clang::CompilerInstance &CI,
		llvm::StringRef Filename) {
			preprocessorAdapter = gcnew PreprocessorAdapter(CI.getPreprocessor());
			return true;
	}
};

void ClangAdapter::Process()
{
	m_pDiagnosticClient->clear();	

	PreprocessorBlockCaptureAction action;
	m_pInstance->ExecuteAction(action);
	m_preprocessorAdapter = action.preprocessorAdapter;
}

List<Diagnostic^>^ ClangAdapter::GetDiagnostics()
{
	StoredDiagnosticClient *client = dynamic_cast<StoredDiagnosticClient*>(m_pInstance->getDiagnostics().takeClient());
	unsigned int numDiagnostics = client->size();
	List<Diagnostic^> ^diagnostics = gcnew List<Diagnostic^>();

	for (unsigned int i = 0; i<numDiagnostics; i++)
	{
		Diagnostic ^managedDiag = ToDiagnostic(client->getDiagInfo(i));
		diagnostics->Add(managedDiag);
	}
	return diagnostics;
}

void ClangAdapter::Initialize(String ^filePath, List<String^> ^includePaths, List<String ^>^ predefinedSymbols)
{
	m_filePath = (char *) ToCString(filePath);

	m_pInvocation = new clang::CompilerInvocation();
	m_pInvocation->getPreprocessorOpts().RetainRemappedFileBuffers = true;
	m_pInvocation->getFrontendOpts().Inputs.push_back(std::pair<clang::InputKind, std::string>(clang::IK_CXX, m_filePath));
	m_pInvocation->getFrontendOpts().ProgramAction = clang::frontend::ParseSyntaxOnly;
	m_pInvocation->getTargetOpts().Triple = "i386-unknown-linux-gnu";

	for each(String^ path in includePaths)
	{
		m_pInvocation->getHeaderSearchOpts().AddPath(ToCString(path), clang::frontend::Angled, true, false, true);
	}

	for each(String^ symbol in predefinedSymbols)
	{
		m_pInvocation->getPreprocessorOpts().addMacroDef(ToCString(symbol));
	}

	m_pInstance = new clang::CompilerInstance();
	m_pInstance->setInvocation(m_pInvocation);
	m_pDiagnosticClient = new StoredDiagnosticClient();
	m_pInstance->createDiagnostics(0, NULL, m_pDiagnosticClient);

	Process(System::IO::File::ReadAllText(filePath));
}

PreprocessorAdapter^ ClangAdapter::GetPreprocessor()
{
	return m_preprocessorAdapter;
}