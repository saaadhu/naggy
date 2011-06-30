#pragma once

#include <vector>

typedef enum BlockType
{
	If,
	Ifdef,
	Ifndef,
	Elif,
	Else,
	Endif
};

class Block
{
public:
	Block(BlockType blockType, int startLine, bool entered)
		: m_blockType(blockType), m_startLine(startLine), m_entered(entered)
	{}

	bool IsStartOfNewCondition() const
	{
		return m_blockType == If || m_blockType == Ifdef || m_blockType == Ifndef;
	}
	
	bool IsEndOfCondition() const
	{
		return m_blockType == Endif;
	}

	unsigned int GetStartLine() const
	{
		return m_startLine;
	}

	bool WasEntered() const
	{
		return m_entered;
	}

private:
	BlockType m_blockType;
	unsigned int m_startLine;
	bool m_entered;
};

class IfBuilder
{
public:
	IfBuilder(std::vector<Block> &skippedBlocks);
	~IfBuilder(void);

	void AddBlockStart(BlockType blockType, int startLine, bool entered);
private:
	std::vector<Block> &m_blockStarts;
};

