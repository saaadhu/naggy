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
			m_skippedBlocks.clear();
		}

		void SortSkippedBlocks()
		{
			std::sort(m_skippedBlocks.begin(), m_skippedBlocks.end());
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
		clang::SourceRange previousElifStart;
		clang::SourceLocation lastEndIfStart;
		IfBuilder ifBuilder;

	public:
		Callback(clang::Preprocessor *pPreprocessor, std::vector<std::pair<unsigned int, unsigned int>> &skippedBlocks) : m_pPreprocessor(pPreprocessor), ifBuilder(skippedBlocks), previousConditionalStackSize(0),
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

			//if (previousElifStart.isValid())
			//{
			//	AddSkippedBlock(previousElifStart.getBegin(), range.getBegin());
			//	previousElifStart = clang::SourceRange();
			//}	

			//
			//if (!entering)
			//{
			//	previousElifStart = range;

			//	if (firstElifSourceRange.isValid())
			//	{
			//		AddSkippedBlock(range.getBegin(), firstElifSourceRange.getBegin());
			//	}
			//}

			//if (!elifSeen)
			//	firstElifSourceRange = range;

			//elifSeen = true;
		}

		virtual void Endif()
		{
			ifBuilder.AddBlockStart(GetLine(GetCurrentLocation()), true);
			//if (previousElifStart.isValid())
			//{
			//	AddSkippedBlock(previousElifStart.getBegin(), GetCurrentLocation());
			//	previousElifStart = clang::SourceRange();
			//}	
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
				line = sm.getLineNumber(sm.getMainFileID(), sm.getFileOffset(loc));
			}

			return line;
		}

		//void AddSkippedBlock(const clang::SourceLocation &start, const clang::SourceLocation &end)
		//{
		//	clang::SourceManager &sm = m_pPreprocessor->getSourceManager();
		//	if (sm.isFromMainFile(start))
		//	{
		//		unsigned int startLine = sm.getLineNumber(sm.getMainFileID(), sm.getFileOffset(start));
		//		unsigned int endLine = sm.getLineNumber(sm.getMainFileID(), sm.getFileOffset(end));

		//		if (startLine > endLine)
		//			return;

		//		startLine++;
		//		endLine--;

		//		m_skippedBlocks.push_back(std::pair<unsigned int, unsigned int>(startLine, endLine));
		//	}
		//}

		clang::Preprocessor *m_pPreprocessor;
	};
}