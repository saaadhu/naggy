#include "StdAfx.h"
#include "ClangPreprocessor.h"
#include "clang\Frontend\TextDiagnosticBuffer.h"
#include "clang\Lex\MacroInfo.h"
#include "clang\Lex\PPCallbacks.h"
#include "clang\Lex\PreprocessorLexer.h"
#include "clang\Basic\FileSystemOptions.h"
#include "clang\Basic\DiagnosticIDs.h"
#include "llvm\ADT\IntrusiveRefCntPtr.h"

#include <vector>
using namespace NaggyClang;

ClangPreprocessor::ClangPreprocessor(clang::Preprocessor &preprocessor) : m_pPreprocessor(&preprocessor), m_pCallback(new Callback(&preprocessor, m_skippedBlocks))
{
	preprocessor.addPPCallbacks(std::unique_ptr<clang::PPCallbacks>(m_pCallback));
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
