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
#include "IfBuilder.h"

namespace NaggyClang
{
	class Callback;

	class ClangPreprocessor
	{
	public:
		ClangPreprocessor(clang::Preprocessor &preprocessor);
		~ClangPreprocessor();

		std::string ExpandMacro(const char* macroName);
		void Reset()
		{
			m_blockStarts.clear();
			m_skippedBlocks.clear();
		}

		clang::Preprocessor* GetPreprocessor()
		{
			return m_pPreprocessor;
		}

		void CalculateSkippedBlocks()
		{
			m_skippedBlocks.clear();
			std::sort(m_blockStarts.begin(), m_blockStarts.end());

			for (unsigned int i = 0; i<m_blockStarts.size(); ++i)
			{
				if (i != m_blockStarts.size() - 1)
				{
					std::pair<int, bool> currentBlockStart = m_blockStarts[i];

					if (!currentBlockStart.second) // this block was NOT entered, so include in skipped blocks
					{
						const std::pair<unsigned int, unsigned int> block = std::make_pair(currentBlockStart.first + 1, m_blockStarts[i+1].first - 1);
						m_skippedBlocks.push_back(block);
					}
				}
			}
		}

		typedef std::vector<std::pair<unsigned int, unsigned int>>::const_iterator skipped_blocks_iterator;

		skipped_blocks_iterator skipped_blocks_begin() { return m_skippedBlocks.begin(); }
		skipped_blocks_iterator skipped_blocks_end() { return m_skippedBlocks.end(); }

	private:
		bool Expand(const char* macroName, std::string &expansion);

		Callback *m_pCallback;
		clang::Preprocessor *m_pPreprocessor;

		std::vector<std::pair<unsigned int, bool>> m_blockStarts;
		std::vector<std::pair<unsigned int, unsigned int>> m_skippedBlocks;
	};

	class Callback : public clang::PPCallbacks
	{
		int previousConditionalStackSize;
		bool elifSeen;
		clang::SourceRange firstElifSourceRange;
		clang::SourceRange previousElifStart;
		clang::SourceLocation lastEndIfStart;
		IfBuilder ifBuilder;

	public:
		Callback(clang::Preprocessor *pPreprocessor, std::vector<std::pair<unsigned int, bool>> &blockStarts) : m_pPreprocessor(pPreprocessor), ifBuilder(blockStarts), previousConditionalStackSize(0),
			elifSeen(false)
		{ }

		virtual void Ifdef(const clang::Token &tok, bool entering)
		{
			ifBuilder.AddBlockStart(GetLine(tok.getLocation()), entering);
		}

		virtual void If(clang::SourceRange range, bool entering)
		{
			ifBuilder.AddBlockStart(GetLine(range.getBegin()), entering);
		}

		virtual void Elif(clang::SourceRange range, bool entering)
		{
			ifBuilder.AddBlockStart(GetLine(range.getBegin()), entering);
		}

		virtual void Endif()
		{
			ifBuilder.AddBlockStart(GetLine(GetCurrentLocation()), true);
		}

		virtual void Else(clang::SourceRange range, bool entering)
		{
			ifBuilder.AddBlockStart(GetLine(range.getBegin()), entering);
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
			if (sm.isFromMainFile(loc))
			{
				line = sm.getSpellingLineNumber(loc);
			}

			return line;
		}

		clang::Preprocessor *m_pPreprocessor;
	};
}