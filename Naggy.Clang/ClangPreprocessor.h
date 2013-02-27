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
#include <vector>

namespace NaggyClang
{
	class Callback;
	typedef std::pair<unsigned int, unsigned int> LineRange;

	class ClangPreprocessor
	{
	public:
		ClangPreprocessor(clang::Preprocessor &preprocessor);
		~ClangPreprocessor();

		std::string ExpandMacro(const char* macroName);
		void Reset()
		{
			m_skippedBlocks.clear();
		}

		clang::Preprocessor* GetPreprocessor()
		{
			return m_pPreprocessor;
		}


		typedef std::vector<LineRange>::const_iterator skipped_blocks_iterator;

		skipped_blocks_iterator skipped_blocks_begin() { return m_skippedBlocks.begin(); }
		skipped_blocks_iterator skipped_blocks_end() { return m_skippedBlocks.end(); }

	private:
		bool Expand(const char* macroName, std::string &expansion);

		Callback *m_pCallback;
		clang::Preprocessor *m_pPreprocessor;

		std::vector<LineRange> m_skippedBlocks;
	};

	class Callback : public clang::PPCallbacks
	{
	public:
		Callback(clang::Preprocessor *pPreprocessor, std::vector<LineRange> &blocks) : m_pPreprocessor(pPreprocessor), m_skippedBlocks(blocks)
		{ }

		virtual void SourceRangeSkipped(clang::SourceRange sourceRange)
		{
			clang::SourceLocation start = sourceRange.getBegin();
			clang::SourceLocation end = sourceRange.getEnd();

			clang::SourceManager &sm = m_pPreprocessor->getSourceManager();
			if (!sm.isFromMainFile(start))
				return;

			m_skippedBlocks.push_back(LineRange(GetLine(start), GetLine(end)));
		}

	private:
		const clang::SourceLocation GetCurrentLocation()
		{
			return ((clang::Lexer*)m_pPreprocessor->getCurrentLexer())->getSourceLocation();
		}

		const unsigned int GetLine(const clang::SourceLocation &loc)
		{
			unsigned int line = 0;
			clang::SourceManager &sm = m_pPreprocessor->getSourceManager();
			return sm.getSpellingLineNumber(loc);
		}

		clang::Preprocessor *m_pPreprocessor;
		std::vector<LineRange> &m_skippedBlocks;
	};
}