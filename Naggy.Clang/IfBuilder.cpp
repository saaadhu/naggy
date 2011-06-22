#include "StdAfx.h"
#include "IfBuilder.h"
#include <algorithm>

IfBuilder::IfBuilder(std::vector<std::pair<unsigned int, unsigned int>> &blocks) : m_blocks(blocks)
{
}


IfBuilder::~IfBuilder(void)
{
}

void IfBuilder::AddBlockStart(int line, bool entered)
{
	m_blockStarts.push_back(std::make_pair(line, entered));
	CreateBlocks();
}

void IfBuilder::CreateBlocks()
{
	std::sort(m_blockStarts.begin(), m_blockStarts.end());

	for (unsigned int i = 0; i<m_blockStarts.size(); ++i)
	{
		if (i != m_blockStarts.size() - 1)
		{
			std::pair<int, bool> currentBlockStart = m_blockStarts[i];

			if (!currentBlockStart.second) // this block was NOT entered, so include in skipped blocks
			{
				const std::pair<unsigned int, unsigned int> block = std::make_pair(currentBlockStart.first + 1, m_blockStarts[i+1].first - 1);
				if (std::find(m_blocks.begin(), m_blocks.end(), block) == m_blocks.end())
					m_blocks.push_back(block);
			}
		}
	}
}
