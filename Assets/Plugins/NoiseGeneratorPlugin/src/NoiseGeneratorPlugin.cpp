// PerlinNoisePlugin.cpp : Defines the exported functions for the DLL.
//
#include "NoiseGeneratorPlugin.h"

#include "Generic.h"
#include "PerlinNoise.h"
#include "WorleyNoise.h"

#include <array>
#include <numeric>
#include <algorithm>
#include <random>
#include <cmath>

enum class NoiseType
{
	PerlinNoise,
	WorleyNoise
};

float SampleNoiseOctaves(float x, float y, float z,
	unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
	float octaves, float persistence,
	NoiseType type)
{
	float total = 0;
	unsigned int frequency = 1;
	float amplitude = 1;
	float maxValue = 0;

	for (int i = 0; i < octaves; i++)
	{
		switch (type)
		{
			case NoiseType::PerlinNoise:
				total += PerlinNoise::sampleNoise(x * frequency, y * frequency, z * frequency,
					scaleX * frequency, scaleY * frequency, scaleZ * frequency) * amplitude;
				break;

			case NoiseType::WorleyNoise:
				total += WorleyNoise::sampleNoise(x * frequency, y * frequency, z * frequency,
					scaleX * frequency, scaleY * frequency, scaleZ * frequency) * amplitude;
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

void SeedGenerator(unsigned int seed)
{
	Generic::seedGenerator(seed);
}

void GeneratePerlinNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
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

void GeneratePerlinNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
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


float SampleWorleyNoiseOctaves(float x, float y, float z,
	unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ,
	float octaves, float persistence)
{
	return SampleNoiseOctaves(x, y, z, tilesX, tilesY, tilesZ, octaves, persistence, NoiseType::WorleyNoise);
}

void GenerateWorleyNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
	unsigned int scaleX, unsigned int scaleY,
	float octaves, float persistence,
	float contrast,
	float valueMin, float valueMax, float remapMin, float remapMax,
	float data[])
{
	Image::GenerateNoiseImage2D(resolutionX, resolutionY, scaleX, scaleY,
		octaves, persistence, NoiseType::WorleyNoise,
		contrast, valueMin, valueMax, remapMin, remapMax, data);
}

void GenerateWorleyNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
	unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
	float octaves, float persistence,
	float contrast,
	float valueMin, float valueMax, float remapMin, float remapMax,
	float data[])
{
	Image::GenerateNoiseImage3D(resolutionX, resolutionY, resolutionZ, scaleX, scaleY, scaleZ,
		octaves, persistence, NoiseType::WorleyNoise,
		contrast, valueMin, valueMax, remapMin, remapMax, data);
}