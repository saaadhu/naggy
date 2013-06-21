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

class StoredDiagnosticClient : public clang::DiagnosticConsumer
{
	std::vector<clang::StoredDiagnostic> diags;

public:
	virtual void HandleDiagnostic(clang::DiagnosticsEngine::Level DiagLevel,
		const clang::Diagnostic& Info)
	{
		diags.push_back(clang::StoredDiagnostic(DiagLevel, Info));
	}

	virtual bool IncludeInDiagnosticsCount() const { return true; }

	void clear() { diags.clear(); }

	unsigned int size() { return diags.size(); }
	clang::StoredDiagnostic& getDiagInfo(unsigned int elementIndex)
	{
		return diags[elementIndex];
	}

	virtual DiagnosticConsumer *clone(clang::DiagnosticsEngine &Diags) const
	{
		StoredDiagnosticClient *client = new StoredDiagnosticClient();
		client->diags = diags;
		return client;
	}
};

static NaggyClang::Diagnostic^ ToDiagnostic(clang::StoredDiagnostic& diag)
{
	Diagnostic^ managedDiag = gcnew Diagnostic;
	managedDiag->ID = diag.getID();
	managedDiag->Message = ToManagedString(diag.getMessage().data());

	const clang::FullSourceLoc &start = diag.getLocation();

	const clang::SourceManager &sourceManager = start.getManager();
	clang::PresumedLoc &loc = sourceManager.getPresumedLoc(start);

	managedDiag->StartLine = loc.getLine();
	managedDiag->StartColumn = loc.getColumn();
	managedDiag->FilePath = ToManagedString(loc.getFilename());

	switch(diag.getLevel())
	{
	case clang::DiagnosticsEngine::Level::Warning:
	case clang::DiagnosticsEngine::Level::Note:
	case clang::DiagnosticsEngine::Level::Ignored:
		managedDiag->Level = DiagnosticLevel::Warning;
		break;
	default:
		managedDiag->Level = DiagnosticLevel::Error;
		break;
	}

	return managedDiag;
}

ClangAdapter::ClangAdapter(String^ fileName)
{
	Initialize(fileName, gcnew List<String^>(), gcnew List<String^>(), false);
}

ClangAdapter::ClangAdapter(String ^fileName, List<String^> ^includePaths)
{
	Initialize(fileName, includePaths, gcnew List<String^>(), false);
}

ClangAdapter:: ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols)
{
	Initialize(fileName, includePaths, symbols, false);
}

ClangAdapter:: ClangAdapter(String ^fileName, List<String^> ^includePaths, List<String ^> ^symbols, bool isC99Enabled)
{
	Initialize(fileName, includePaths, symbols, isC99Enabled);
}

class PreprocessorBlockCaptureAction : public clang::SyntaxOnlyAction
{
public:
	gcroot<PreprocessorAdapter^> m_preprocessorAdapter;

	PreprocessorBlockCaptureAction(PreprocessorAdapter^ preprocessorAdapter) : m_preprocessorAdapter(preprocessorAdapter)
	{}

	virtual bool BeginSourceFileAction(clang::CompilerInstance &CI,
		llvm::StringRef Filename) {
				m_preprocessorAdapter = gcnew PreprocessorAdapter(CI.getPreprocessor());
			return true;
	}
};
void ClangAdapter::Process(String ^contents)
{
	DestroyClangCompiler();

	const char* pContents = ToCString(contents);
	CreateClangCompiler();

	m_pDiagnosticClient->clear();	
	clang::CompilerInvocation *pInvocation = new clang::CompilerInvocation();
	InitializeInvocation(pInvocation);

	if (pContents)
		pInvocation->getPreprocessorOpts().addRemappedFile(m_filePath, llvm::MemoryBuffer::getMemBufferCopy(pContents));

	m_pInstance->setInvocation(pInvocation);

	PreprocessorBlockCaptureAction action(m_preprocessorAdapter);
	m_pInstance->ExecuteAction(action);
	m_preprocessorAdapter = action.m_preprocessorAdapter;

	Marshal::FreeHGlobal(IntPtr((void *)pContents));
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

void ClangAdapter::Initialize(String ^filePath, List<String^> ^includePaths, List<String ^>^ predefinedSymbols, bool isC99Enabled)
{
	this->includePaths = includePaths;
	this->predefinedSymbols = predefinedSymbols;
	this->isC99Enabled = isC99Enabled;

	m_filePath = (char *) ToCString(filePath);
	m_pInstance = NULL;
	CreateClangCompiler();
}

PreprocessorAdapter^ ClangAdapter::GetPreprocessor()
{
	return m_preprocessorAdapter;
}

void ClangAdapter::CreateClangCompiler()
{
	m_pInstance = new clang::CompilerInstance();
	m_pDiagnosticClient = new StoredDiagnosticClient();
	m_pInstance->createDiagnostics(m_pDiagnosticClient, true);
}

void ClangAdapter::DestroyClangCompiler()
{
	if (m_pInstance)
		delete m_pInstance;

	m_pInstance = NULL;
}

void ClangAdapter::InitializeInvocation(clang::CompilerInvocation *pInvocation)
{
	pInvocation->getFrontendOpts().Inputs.push_back(clang::FrontendInputFile(m_filePath, clang::IK_CXX));
	pInvocation->getFrontendOpts().ProgramAction = clang::frontend::ParseSyntaxOnly;
	pInvocation->getTargetOpts().Triple = "i386-unknown-linux-gnu";

	for each(String^ path in includePaths)
	{
		pInvocation->getHeaderSearchOpts().AddPath(ToCString(path), clang::frontend::Angled, false, true);
	}

	for each(String^ symbol in predefinedSymbols)
	{
		pInvocation->getPreprocessorOpts().addMacroDef(ToCString(symbol));
	}

	// HACK: Use preprocessor hacking to reduce named address spaces to blanks, so that they won't show up as errors
	// Really dangerous, but until Clang knows about these, it will keep flagging them as errors.
	array<String^> ^addressSpaces = { "__flash", "__flash1", "__flash2", "__flash3", "__flash4", "__flash5", "__memx" };
	for each(String ^addressSpace in addressSpaces)
	{
		pInvocation->getPreprocessorOpts().addMacroDef(ToCString(addressSpace + "="));
	}

	pInvocation->getLangOpts()->C99 = isC99Enabled ? 1 : 0;
	pInvocation->getLangOpts()->GNUMode = 1;
	pInvocation->getLangOpts()->GNUKeywords = 1;
	pInvocation->getLangOpts()->Bool = 1;
	pInvocation->getLangOpts()->LineComment = 1;
}