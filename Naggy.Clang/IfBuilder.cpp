#include "StdAfx.h"
#include "IfBuilder.h"
#include <algorithm>

IfBuilder::IfBuilder(std::vector<Block> &blockStarts) : m_blockStarts(blockStarts)
{
}

IfBuilder::~IfBuilder(void)
{
}

void IfBuilder::AddBlockStart(BlockType blockType, int line, bool entered)
{
	// Invalid line, don't add
	if (line == 0)
		return;

	Block block(blockType, line, entered);
	m_blockStarts.push_back(block);
}
