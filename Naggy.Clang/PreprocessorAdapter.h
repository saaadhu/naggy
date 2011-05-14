#pragma once

#include <string>

using namespace System;

namespace NaggyClang
{
	class ClangPreprocessor;

	public ref class PreprocessorAdapter
	{

	internal:
		PreprocessorAdapter(const char* sourceFile);
		void Preprocess();

	public:
		String^ ExpandMacro(String ^macroName);

		~PreprocessorAdapter()
		{
			delete m_pPreprocessor;
		}

		!PreprocessorAdapter()
		{
			delete m_pPreprocessor;
		}

	private:
		ClangPreprocessor *m_pPreprocessor;
	};
}
