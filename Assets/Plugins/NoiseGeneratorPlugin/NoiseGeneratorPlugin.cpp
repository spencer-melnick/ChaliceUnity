// PerlinNoisePlugin.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "NoiseGeneratorPlugin.h"

#include <array>
#include <numeric>
#include <algorithm>
#include <random>
#include <cmath>

const int permutations = 256;
std::array<int, permutations * 2> hashTable;
bool seeded = false;

enum class NoiseType
{
	PerlinNoise,
	WorleyNoise
};

void SeedGenerator(unsigned int seed)
{
	std::default_random_engine random;
	random.seed(seed);

	std::iota(hashTable.begin(), hashTable.begin() + permutations, 0);
	std::shuffle(hashTable.begin(), hashTable.end(), random);
	std::copy(hashTable.begin(), hashTable.begin() + (permutations - 1), hashTable.begin() + permutations);

	seeded = true;
}

namespace Generic
{
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

namespace PerlinNoise
{
	struct HashCubeResult
	{
		int aaa, aab, aba, abb, baa, bab, bba, bbb;
	};

	float fade(float t)
	{
		return t * t* t* (t * (t * 6 - 15) + 10);
	}

	int hashLookup(int x)
	{
		return hashTable[static_cast<unsigned int>(x) % permutations];
	}

	HashCubeResult hashCube(int x, int y, int z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ)
	{
		HashCubeResult result;

		int xi = x + 1;
		int yi = y + 1;
		int zi = z + 1;

		xi %= tilesX;
		yi %= tilesY;
		zi %= tilesZ;

		result.aaa = hashLookup(hashLookup(hashLookup(x) + y) + z);
		result.aab = hashLookup(hashLookup(hashLookup(x) + y) + zi);
		result.aba = hashLookup(hashLookup(hashLookup(x) + yi) + z);
		result.abb = hashLookup(hashLookup(hashLookup(x) + yi) + zi);
		result.baa = hashLookup(hashLookup(hashLookup(xi) + y) + z);
		result.bab = hashLookup(hashLookup(hashLookup(xi) + y) + zi);
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

	float sampleNoise(float x, float y, float z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ)
	{
		tilesX = max(tilesX, permutations);
		tilesY = max(tilesY, permutations);
		tilesZ = max(tilesZ, permutations);

		x = std::fmodf(x, static_cast<float>(tilesX));
		y = std::fmodf(y, static_cast<float>(tilesY));
		z = std::fmodf(z, static_cast<float>(tilesZ));

		int xi = static_cast<int>(std::floor(x));
		int yi = static_cast<int>(std::floor(y));
		int zi = static_cast<int>(std::floor(z));

		float dx = x - static_cast<float>(xi);
		float dy = y - static_cast<float>(yi);
		float dz = z - static_cast<float>(zi);

		float fx = fade(dx);
		float fy = fade(dy);
		float fz = fade(dz);
		HashCubeResult hashResult = hashCube(xi, yi, zi, tilesX, tilesY, tilesZ);

		float x1, x2, y1, y2, value;

		x1 = Generic::lerp(perlinGradient(hashResult.aaa, dx, dy, dz),
			perlinGradient(hashResult.baa, dx - 1.0f, dy, dz),
			fx);

		x2 = Generic::lerp(perlinGradient(hashResult.aba, dx, dy - 1.0f, dz),
			perlinGradient(hashResult.bba, dx - 1.0f, dy - 1.0f, dz),
			fx);

		y1 = Generic::lerp(x1, x2, fy);

		x1 = Generic::lerp(perlinGradient(hashResult.aab, dx, dy, dz - 1.0f),
			perlinGradient(hashResult.bab, dx - 1.0f, dy, dz - 1.0f),
			fx);

		x2 = Generic::lerp(perlinGradient(hashResult.abb, dx, dy - 1.0f, dz - 1.0f),
			perlinGradient(hashResult.bbb, dx - 1.0f, dy - 1.0f, dz - 1.0f),
			fx);

		y2 = Generic::lerp(x1, x2, fy);

		value = Generic::lerp(y1, y2, fz);
		value = (value + 1.0f) / 2.0f;

		return value;
	}
};


float SampleNoiseOctaves(float x, float y, float z,
	unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
	float octaves, float persistence,
	NoiseType type)
{
	float total = 0;
	float frequency = 1;
	float amplitude = 1;
	float maxValue = 0;

	for (int i = 0; i < octaves; i++)
	{
		switch (type)
		{
			case NoiseType::PerlinNoise:
				total += PerlinNoise::sampleNoise(x * frequency, y * frequency, z * frequency, scaleX, scaleY, scaleZ) * amplitude;
				break;

			case NoiseType::WorleyNoise:
				// Sample Worley noise
				break;

			default: break;
		}
		

		maxValue += amplitude;

		amplitude *= persistence;
		frequency *= 2;
	}

	return total / maxValue;
}

float SamplePerlinNoiseOctaves(float x, float y, float z,
	unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ,
	float octaves, float persistence)
{
	return SampleNoiseOctaves(x, y, z, tilesX, tilesY, tilesZ, octaves, persistence, NoiseType::PerlinNoise);
}



// Image generation functions

namespace Image
{
	void GenerateNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
		unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
		float octaves, float persistence,
		NoiseType type,
		float contrast,
		float valueMin, float valueMax, float remapMin, float remapMax,
		float data[])
	{
		if (!seeded)
		{
			SeedGenerator(0);
		}

		unsigned int pixelNum = 0;

		float xCoord;
		float yCoord;
		float zCoord;

		float value;

		for (unsigned int k = 0; k < resolutionZ; k++)
		{
			zCoord = static_cast<float>(k) * static_cast<float>(scaleZ) / static_cast<float>(resolutionZ);

			for (unsigned int j = 0; j < resolutionY; j++)
			{
				yCoord = static_cast<float>(j) * static_cast<float>(scaleY) / static_cast<float>(resolutionY);

				for (unsigned int i = 0; i < resolutionX; i++)
				{
					xCoord = static_cast<float>(i) * static_cast<float>(scaleX) / static_cast<float>(resolutionX);

					value = SampleNoiseOctaves(xCoord, yCoord, zCoord, scaleX, scaleY, scaleZ, octaves, persistence, type);
					value = Generic::remap(value, valueMin, valueMax, remapMin, remapMax);
					value = Generic::applyContrast(value, contrast);

					data[pixelNum++] = value;
				}
			}
		}
	}

	void GenerateNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
		unsigned int scaleX, unsigned int scaleY,
		float octaves, float persistence,
		NoiseType type,
		float contrast,
		float valueMin, float valueMax, float remapMin, float remapMax,
		float data[])
	{
		GenerateNoiseImage3D(resolutionX, resolutionY, 1,
			scaleX, scaleY, 1,
			octaves, persistence,
			type,
			contrast,
			valueMin, valueMax, remapMin, remapMax,
			data);
	}
}


// Plugin functions

NOISEGENERATORPLUGIN_API void GeneratePerlinNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
	unsigned int scaleX, unsigned int scaleY,
	float octaves, float persistence,
	float contrast,
	float valueMin, float valueMax, float remapMin, float remapMax,
	float data[])
{
	Image::GenerateNoiseImage2D(resolutionX, resolutionY, scaleX, scaleY,
		octaves, persistence, NoiseType::PerlinNoise,
		contrast, valueMin, valueMax, remapMin, remapMax, data);
}

NOISEGENERATORPLUGIN_API void GeneratePerlinNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
	unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
	float octaves, float persistence,
	float contrast,
	float valueMin, float valueMax, float remapMin, float remapMax,
	float data[])
{
	Image::GenerateNoiseImage3D(resolutionX, resolutionY, resolutionZ, scaleX, scaleY, scaleZ,
		octaves, persistence, NoiseType::PerlinNoise,
		contrast, valueMin, valueMax, remapMin, remapMax, data);
}