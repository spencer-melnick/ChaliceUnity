// PerlinNoisePlugin.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "PerlinNoisePlugin.h"

#include <array>
#include <numeric>
#include <algorithm>
#include <random>
#include <cmath>

const int permutations = 256;
std::array<int, permutations * 2> hashTable;
bool seeded = false;

void SeedGenerator(unsigned int seed)
{
	std::default_random_engine random;
	random.seed(seed);

	std::iota(hashTable.begin(), hashTable.begin() + permutations, 0);
	std::shuffle(hashTable.begin(), hashTable.end(), random);
	std::copy(hashTable.begin(), hashTable.begin() + (permutations - 1), hashTable.begin() + permutations);

	seeded = true;
}

struct HashResult
{
	int aaa, aab, aba, abb, baa, bab, bba, bbb;
};

float fade(float t)
{
	return t * t * t * (t * (t * 6 - 15) + 10);
}

int hashLookup(int x)
{
	return hashTable[static_cast<unsigned int>(x) % permutations];
}

HashResult hashCube(int x, int y, int z, bool repeat = true)
{
	HashResult result;

	int xi = x + 1;
	int yi = y + 1;
	int zi = z + 1;

	if (repeat)
	{
		xi %= permutations;
		yi %= permutations;
		zi %= permutations;
	}

	result.aaa = hashLookup(hashLookup(hashLookup( x) +  y)  + z);
	result.aab = hashLookup(hashLookup(hashLookup( x) +  y) + zi);
	result.aba = hashLookup(hashLookup(hashLookup( x) + yi) + z);
	result.abb = hashLookup(hashLookup(hashLookup( x) + yi) + zi);
	result.baa = hashLookup(hashLookup(hashLookup(xi) +  y) + z);
	result.bab = hashLookup(hashLookup(hashLookup(xi) +  y) + zi);
	result.bba = hashLookup(hashLookup(hashLookup(xi) + yi) + z);
	result.bbb = hashLookup(hashLookup(hashLookup(xi) + yi) + zi);

	return result;
}

float perlinGradient(int hashValue, float x, float y, float z)
{
	// Switch using the first 4 bits of the hash value
	switch (hashValue & 0xF)
	{
		case 0x0: return  x + y; break;
		case 0x1: return -x + y; break;
		case 0x2: return  x - y; break;
		case 0x3: return -x - y; break;
		case 0x4: return  x + z; break;
		case 0x5: return -x + z; break;
		case 0x6: return  x - z; break;
		case 0x7: return -x - z; break;
		case 0x8: return  y + z; break;
		case 0x9: return -y + z; break;
		case 0xA: return  y - z; break;
		case 0xB: return -y - z; break;
		case 0xC: return  y + x; break;
		case 0xD: return -y + z; break;
		case 0xE: return  y - x; break;
		case 0xF: return -y - z; break;
		default: return 0;
	}
}

float lerp(float a, float b, float t)
{

	return a + (b - a) * t;
}

float SamplePerlinNoise(float x, float y, float z, bool repeat = true)
{
	// Wrap coords if we are repeating
	if (repeat)
	{
		x = std::fmod(x, static_cast<float>(permutations));
		y = std::fmod(y, static_cast<float>(permutations));
		z = std::fmod(z, static_cast<float>(permutations));
	}

	int xi = static_cast<int>(std::floor(x));
	int yi = static_cast<int>(std::floor(y));
	int zi = static_cast<int>(std::floor(z));

	float dx = x - static_cast<float>(xi);
	float dy = y - static_cast<float>(yi);
	float dz = z - static_cast<float>(zi);

	float fx = fade(dx);
	float fy = fade(dy);
	float fz = fade(dz);
	HashResult hashResult = hashCube(xi, yi, zi, permutations);

	float x1, x2, y1, y2, value;

	x1 = lerp(perlinGradient(hashResult.aaa, dx, dy, dz),
			  perlinGradient(hashResult.baa, dx - 1.0f, dy, dz),
			  fx);

	x2 = lerp(perlinGradient(hashResult.aba, dx, dy - 1.0f, dz),
			  perlinGradient(hashResult.bba, dx - 1.0f, dy - 1.0f, dz),
			  fx);

	y1 = lerp(x1, x2, fy);

	x1 = lerp(perlinGradient(hashResult.aab, dx, dy, dz - 1.0f),
			  perlinGradient(hashResult.bab, dx - 1.0f, dy, dz - 1.0f),
			  fx);

	x2 = lerp(perlinGradient(hashResult.abb, dx, dy - 1.0f, dz - 1.0f),
			  perlinGradient(hashResult.bbb, dx - 1.0f, dy - 1.0f, dz - 1.0f),
			  fx);

	y2 = lerp(x1, x2, fy);

	value = lerp(y1, y2, fz);
	value = (value + 1.0f) / 2.0f;

	return value;
}

float SamplePerlinNoiseOctaves(float x, float y, float z, float octaves, float persistence)
{
	float total = 0;
	float frequency = 1;
	float amplitude = 1;
	float maxValue = 0;

	for (int i = 0; i < octaves; i++)
	{
		total += SamplePerlinNoise(x * frequency, y * frequency, z * frequency, true) * amplitude;

		maxValue += amplitude;

		amplitude *= persistence;
		frequency *= 2;
	}

	return total / maxValue;
}


float applyContrast(float input, float contrast)
{
	input *= 2.0f;
	input -= 1.0f;
	contrast = -contrast;
	return 0.5f + ((input - input * contrast) / (contrast - std::abs(input) * contrast + 1.0f)) / 2.0f;
}

PERLINNOISEPLUGIN_API void GeneratePerlinNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
	float scaleX, float scaleY,
	float octaves, float persistence, float contrast, float data[])
{
	if (!seeded)
	{
		SeedGenerator(0);
	}

	unsigned int pixelNum = 0;

	float xCoord;
	float yCoord;

	for (unsigned int j = 0; j < resolutionY; j++)
	{
		yCoord = static_cast<float>(j) * static_cast<float>(scaleY) / static_cast<float>(resolutionY);

		for (unsigned int i = 0; i < resolutionX; i++)
		{
			xCoord = static_cast<float>(i) * static_cast<float>(scaleX) / static_cast<float>(resolutionX);

			data[pixelNum++] = applyContrast(SamplePerlinNoiseOctaves(xCoord, yCoord, 0.5f, octaves, persistence), contrast);
		}
	}
}

PERLINNOISEPLUGIN_API void GeneratePerlinNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
	float scaleX, float scaleY, float scaleZ,
	float octaves, float persistence, float contrast, float data[])
{
	if (!seeded)
	{
		SeedGenerator(0);
	}

	unsigned int pixelNum = 0;

	float xCoord;
	float yCoord;
	float zCoord;

	for (unsigned int k = 0; k < resolutionZ; k++)
	{
		zCoord = static_cast<float>(k) * static_cast<float>(scaleZ) / static_cast<float>(resolutionZ);

		for (unsigned int j = 0; j < resolutionY; j++)
		{
			yCoord = static_cast<float>(j) * static_cast<float>(scaleY) / static_cast<float>(resolutionY);

			for (unsigned int i = 0; i < resolutionX; i++)
			{
				xCoord = static_cast<float>(i) * static_cast<float>(scaleX) / static_cast<float>(resolutionX);

				data[pixelNum++] = applyContrast(SamplePerlinNoiseOctaves(xCoord, yCoord, zCoord, octaves, persistence), contrast);
			}
		}
	}
}