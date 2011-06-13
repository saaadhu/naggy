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

		typedef std::vector<std::pair<unsigned int, unsigned int>>::const_iterator skipped_blocks_iterator;

		skipped_blocks_iterator skipped_blocks_begin() { return m_skippedBlocks.begin(); }
		skipped_blocks_iterator skipped_blocks_end() { return m_skippedBlocks.end(); }

	private:
		bool Expand(const char* macroName, std::string &expansion);

		Callback *m_pCallback;
		clang::Preprocessor *m_pPreprocessor;

		std::vector<std::pair<unsigned int, unsigned int>> m_skippedBlocks;
	};

	class Callback : public clang::PPCallbacks
	{
		int previousConditionalStackSize;
		bool elifSeen;
		clang::SourceRange firstElifSourceRange;
		std::vector<std::pair<unsigned int, unsigned int>> &m_skippedBlocks;

	public:
		Callback(clang::Preprocessor *pPreprocessor, std::vector<std::pair<unsigned int, unsigned int>> &skippedBlocks) : m_pPreprocessor(pPreprocessor), m_skippedBlocks(skippedBlocks), previousConditionalStackSize(0),
			elifSeen(false)
		{ }

		virtual void Ifdef(const clang::Token &tok)
		{
			if (IfBlockSkipped())
				AddSkippedBlock(tok.getLocation(), GetCurrentLocation());
		}

		virtual void If(clang::SourceRange range)
		{
			if (elifSeen || IfBlockSkipped())
				AddSkippedBlock(range.getBegin(), elifSeen ? firstElifSourceRange.getBegin() : GetCurrentLocation());

			elifSeen = false;
		}

		virtual void Elif(clang::SourceRange range)
		{
			if (!elifSeen)
				firstElifSourceRange = range;

			elifSeen = true;
			
			if (FoundNonSkip())
				AddSkippedBlock(range.getBegin(), GetCurrentLocation());
		}
	private:
		bool IfBlockSkipped()
		{
			return previousConditionalStackSize == GetConditionalStackSize();

		}

		const clang::SourceLocation GetCurrentLocation()
		{
			return ((clang::Lexer*)m_pPreprocessor->getCurrentLexer())->getSourceLocation();
		}

		void AddSkippedBlock(const clang::SourceLocation &start, const clang::SourceLocation &end)
		{
			clang::SourceManager &sm = m_pPreprocessor->getSourceManager();
			if (sm.isFromMainFile(start))
			{
				unsigned int startLine = sm.getLineNumber(sm.getMainFileID(), sm.getFileOffset(start));
				unsigned int endLine = sm.getLineNumber(sm.getMainFileID(), sm.getFileOffset(end));

				startLine++;
				endLine--;

				m_skippedBlocks.push_back(std::pair<unsigned int, unsigned int>(startLine, endLine));
			}
		}

		unsigned int GetConditionalStackSize()
		{
			clang::PreprocessorLexer *pLexer = m_pPreprocessor->getCurrentLexer();
			clang::PreprocessorLexer::conditional_iterator iter = pLexer->conditional_begin();

			unsigned int size = 0;
			for(; iter != pLexer->conditional_end(); ++iter)
				size++;

			return size;
		}

		bool FoundNonSkip()
		{
			clang::PreprocessorLexer *pLexer = m_pPreprocessor->getCurrentLexer();
			clang::PreprocessorLexer::conditional_iterator iter = pLexer->conditional_begin();

			if (iter == pLexer->conditional_end())
				return true;

			return iter->FoundNonSkip;
		}

		clang::Preprocessor *m_pPreprocessor;
	};
}