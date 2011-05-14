#include "StdAfx.h"
#include "ClangPreprocessor.h"
#include "clang\Frontend\TextDiagnosticBuffer.h"
#include "clang\Lex\MacroInfo.h"
#include "clang\Lex\PPCallbacks.h"
#include "clang\Lex\PreprocessorLexer.h"
#include "clang\Basic\FileSystemOptions.h"
#include "clang\Basic\DiagnosticIDs.h"
#include "llvm\ADT\IntrusiveRefCntPtr.h"

using namespace NaggyClang;

class Callback : public clang::PPCallbacks
{
public:
	Callback(clang::Preprocessor *pPreprocessor) : m_pPreprocessor(pPreprocessor)
	{
	}

	void Ifdef(const clang::Token &tok)
	{
		clang::PreprocessorLexer *pLexer = m_pPreprocessor->getCurrentLexer();
		clang::PreprocessorLexer::conditional_iterator iter = pLexer->conditional_begin();

		if (iter->WasSkipping)
		{
			iter;
		}
	}

	void Endif()
	{

	}

private:
	clang::Preprocessor *m_pPreprocessor;
};

ClangPreprocessor::ClangPreprocessor(const char* szSourceFile):
m_szSourceFile(szSourceFile),
	diag(clang::Diagnostic(llvm::IntrusiveRefCntPtr<clang::DiagnosticIDs>(new clang::DiagnosticIDs()), new clang::TextDiagnosticBuffer())),
	fm(clang::FileSystemOptions()),
	sm(diag, fm),
	hs(fm)
{
	to.Triple = "i686-pc-win32";
	target_info = clang::TargetInfo::CreateTargetInfo(diag, to);
	m_pPreprocessor = new clang::Preprocessor(diag, lang_opts, *target_info, sm, hs);
}

void ClangPreprocessor::Process()
{
	const clang::FileEntry* file = fm.getFile(m_szSourceFile);
	sm.createMainFileID(file);
	m_pPreprocessor->EnterMainSourceFile();

	Callback callback(m_pPreprocessor);
	m_pPreprocessor->addPPCallbacks(&callback);

	clang::Token token;
	do
	{
		m_pPreprocessor->Lex(token);

	}while (token.isNot(clang::tok::eof));
}

bool ClangPreprocessor::Expand(const char* macroName, std::string &expansion)
{
	clang::IdentifierInfo *pIdentifierInfo = m_pPreprocessor->getIdentifierInfo(macroName);
	clang::MacroInfo* pMacroInfo = m_pPreprocessor->getMacroInfo(pIdentifierInfo);

	if (pMacroInfo == NULL)
		return false;

	clang::MacroInfo::tokens_iterator iter = pMacroInfo->tokens_begin();
	expansion = "";
	for(;iter != pMacroInfo->tokens_end(); ++iter)
	{
		std::string &tokenText = m_pPreprocessor->getSpelling(*iter);	
		std::string recursiveExpansion = tokenText;
		Expand(tokenText.c_str(), recursiveExpansion);

		expansion += recursiveExpansion;
	}

	return true;
}

std::string ClangPreprocessor::ExpandMacro(const char* macroName)
{
	std::string expansion;
	Expand(macroName, expansion);

	return expansion;
}
