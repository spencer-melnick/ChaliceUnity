#include "Generic.h"

#include <array>
#include <cmath>
#include <random>
#include <algorithm>
#include <numeric>

namespace Generic
{
	std::array<unsigned int, numPermutations * 2> hashTable{};

	void seedGenerator(unsigned int seed)
	{
		std::default_random_engine random;
		random.seed(seed);

		std::iota(hashTable.begin(), hashTable.begin() + Generic::numPermutations, 0);
		
		for (unsigned int i = 0; i < numPermutations; i++)
		{
			unsigned int swapIndex = random() % numPermutations;
			unsigned int temp = hashTable[swapIndex];

			hashTable[swapIndex] = hashTable[i];
			hashTable[i] = temp;
		}

		std::copy(hashTable.begin(), hashTable.begin() + (Generic::numPermutations - 1), hashTable.begin() + Generic::numPermutations);
	}

	unsigned int hashLookup(unsigned int value)
	{
		return hashTable[value % numPermutations];
	}

	float lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	float applyContrast(float input, float contrast)
	{
		input *= 2.0f;
		input -= 1.0f;
		contrast = -contrast;
		return 0.5f + ((input - input * contrast) / (contrast - std::abs(input) * contrast + 1.0f)) / 2.0f;
	}

	float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
	{
		float normalizedValue = (value - oldMin) / (oldMax - oldMin);
		return normalizedValue * (newMax - newMin) + newMin;
	}
};
