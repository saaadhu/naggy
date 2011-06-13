#pragma once

#include <string>
#include "clang\Lex\Preprocessor.h"

using namespace System;

namespace NaggyClang
{
	class ClangPreprocessor;

	public ref class PreprocessorAdapter
	{

	public:
		PreprocessorAdapter(clang::Preprocessor &preprocessor);
		void Reset();

		String^ ExpandMacro(String ^macroName);
		array<Tuple<int, int>^> ^GetSkippedBlockLineNumbers();

		//~PreprocessorAdapter()
		//{
		//	delete m_pPreprocessor;
		//}

		//!PreprocessorAdapter()
		//{
		//	delete m_pPreprocessor;
		//}

	private:
		ClangPreprocessor *m_pPreprocessor;
	};
}
