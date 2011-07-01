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
			m_conditionalStack.clear();
		}

		clang::Preprocessor* GetPreprocessor()
		{
			return m_pPreprocessor;
		}

		void CalculateSkippedBlocks()
		{
			m_skippedBlocks.clear();
			m_conditionalStack.clear();

			std::sort(m_blockStarts.begin(), m_blockStarts.end(), CompareBlocks);
			int numBlocks = m_blockStarts.size();

			if (numBlocks == 0)
				return;

			for (unsigned int i = 0; i<m_blockStarts.size();)
			{
				auto currentBlock = m_blockStarts[i];

				if (!currentBlock.WasEntered())
				{
					int nextBranchIndex = FindNextBranchIndex(i);

					unsigned int endLine = 0;
					if (nextBranchIndex == -1) // No branch found
					{
						nextBranchIndex = numBlocks;
						endLine = 0;
					}
					else
					{
						endLine =  m_blockStarts[nextBranchIndex].GetStartLine() - 1;
					}

					const std::pair<unsigned int, unsigned int> block = std::make_pair(currentBlock.GetStartLine() + 1, endLine);
					m_skippedBlocks.push_back(block);

					i = nextBranchIndex;
				}
				else
				{
					++i;
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

		std::vector<Block> m_blockStarts;
		std::vector<std::pair<unsigned int, unsigned int>> m_skippedBlocks;
		std::vector<Block> m_conditionalStack;

		static bool CompareBlocks(const Block &b1, const Block &b2)
		{
			return b1.GetStartLine() < b2.GetStartLine();
		}

		int FindNextBranchIndex(int blockStartIndex)
		{
			std::vector<Block> stack;

			for (unsigned int i = blockStartIndex + 1; i<m_blockStarts.size(); ++i)
			{
				auto currentBlock = m_blockStarts[i];
				if (!currentBlock.IsStartOfNewCondition())
				{
					if (stack.empty())
						return i;

					if (currentBlock.IsEndOfCondition())
						stack.pop_back();
				}
				else
				{
					stack.push_back(currentBlock);
				}
			}

			return -1;
		}
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
		Callback(clang::Preprocessor *pPreprocessor, std::vector<Block> &blockStarts) : m_pPreprocessor(pPreprocessor), ifBuilder(blockStarts), previousConditionalStackSize(0),
			elifSeen(false)
		{ }

		virtual void Ifdef(const clang::Token &tok, bool entering)
		{
			ifBuilder.AddBlockStart(BlockType::Ifdef, GetLine(tok.getLocation()), entering);
		}
		
		virtual void Ifndef(const clang::Token &tok, bool entering)
		{
			ifBuilder.AddBlockStart(BlockType::Ifndef, GetLine(tok.getLocation()), entering);
		}

		virtual void If(clang::SourceRange range, bool entering)
		{
			ifBuilder.AddBlockStart(BlockType::If, GetLine(range.getBegin()), entering);
		}

		virtual void Elif(clang::SourceRange range, bool entering)
		{
			ifBuilder.AddBlockStart(BlockType::Elif, GetLine(range.getBegin()), entering);
		}

		virtual void Endif(bool entering)
		{
			ifBuilder.AddBlockStart(BlockType::Endif, GetLine(GetCurrentLocation()), entering);
		}

		virtual void Else(clang::SourceRange range, bool entering)
		{
			ifBuilder.AddBlockStart(BlockType::Else, GetLine(range.getBegin()), entering);
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