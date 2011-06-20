#pragma once

#include <vector>
class IfBuilder
{
public:
	IfBuilder(std::vector<std::pair<unsigned int, unsigned int>> &skippedBlocks);
	~IfBuilder(void);

	void AddBlockStart(int startLine, bool entered);
private:
	void CreateBlocks();

	std::vector<std::pair<unsigned int, unsigned int>> &m_blocks;
	std::vector<std::pair<int, bool>> m_blockStarts;
};

