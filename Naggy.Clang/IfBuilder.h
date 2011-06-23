#pragma once

#include <vector>
class IfBuilder
{
public:
	IfBuilder(std::vector<std::pair<unsigned int, bool>> &skippedBlocks);
	~IfBuilder(void);

	void AddBlockStart(int startLine, bool entered);
private:
	std::vector<std::pair<unsigned int, bool>> &m_blockStarts;
};

