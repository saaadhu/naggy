#pragma once

#include "clang/Basic/Diagnostic.h"
#include "clang\Basic\LangOptions.h"
#include "clang\Basic\TargetOptions.h"

#include "clang\Basic\SourceManager.h"
#include "clang\Basic\FileManager.h"
#include "clang\Lex\HeaderSearch.h"
#include "clang\Lex\Preprocessor.h"
#include "clang\Basic\TargetInfo.h"
#include <string>

namespace NaggyClang
{
	class ClangPreprocessor
	{
	public:
		ClangPreprocessor(const char* file);
		void Process();
		std::string ExpandMacro(const char* macroName);

		~ClangPreprocessor()
		{
			delete m_pPreprocessor;
			delete target_info;
		}

	private:

		bool Expand(const char* macroName, std::string &expansion);

		const char* m_szSourceFile;
		clang::Diagnostic diag;
		clang::LangOptions lang_opts;
		clang::TargetOptions to;
		clang::TargetInfo *target_info;
		clang::SourceManager sm;
		clang::FileManager fm;
		clang::HeaderSearch hs;
		clang::Preprocessor *m_pPreprocessor;
	};
}