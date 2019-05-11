#pragma once

namespace PerlinNoise
{
	struct HashCubeResult
	{
		int aaa, aab, aba, abb, baa, bab, bba, bbb;
	};

	float fade(float t);

	int hashLookup(int x);

	HashCubeResult hashCube(int x, int y, int z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ);

	float perlinGradient(int hashValue, float x, float y, float z);

	float sampleNoise(float x, float y, float z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ);
};
