#pragma once

#include <array>

namespace Generic
{
	const unsigned int numPermutations = 256;
	extern std::array<unsigned int, numPermutations * 2> hashTable;

	void seedGenerator(unsigned int seed);
	unsigned int hashLookup(unsigned int value);
	float lerp(float a, float b, float t);
	float applyContrast(float input, float contrast);
	float remap(float value, float oldMin, float oldMax, float newMin, float newMax);
};
