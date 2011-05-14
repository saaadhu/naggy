#include "StdAfx.h"
#include "PreprocessorAdapter.h"
#include "StringUtilities.h"
#include "ClangPreprocessor.h"
#include <string>

using namespace NaggyClang;
using namespace System;

PreprocessorAdapter::PreprocessorAdapter(const char* sourceFile)
{
	m_pPreprocessor = new ClangPreprocessor(sourceFile);
}

void PreprocessorAdapter::Preprocess()
{
	m_pPreprocessor->Process();
}

String^ PreprocessorAdapter::ExpandMacro(String^ macroName)
{
	const char* szMacroName = ToCString(macroName);
	std::string expansion = m_pPreprocessor->ExpandMacro(szMacroName);

	Cleanup(szMacroName);
	return ToManagedString(expansion.c_str());
}
