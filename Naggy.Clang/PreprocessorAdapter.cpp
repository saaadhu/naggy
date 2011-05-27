#include "StdAfx.h"
#include "PreprocessorAdapter.h"
#include "StringUtilities.h"
#include "ClangPreprocessor.h"
#include <string>

using namespace NaggyClang;
using namespace System;
using namespace System::Collections::Generic;

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

array<Tuple<int, int>^> ^PreprocessorAdapter::GetSkippedBlockLineNumbers()
{
    List<Tuple<int, int>^> ^blocks = gcnew List<Tuple<int, int>^>();

	ClangPreprocessor::skipped_blocks_iterator iter = m_pPreprocessor->skipped_blocks_begin();
	for(; iter != m_pPreprocessor->skipped_blocks_end(); ++iter)
	{
		blocks->Add(Tuple::Create<int, int>(iter->first, iter->second));
	}

	return blocks->ToArray();
}