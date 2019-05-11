#include "PerlinNoise.h"

#include <cmath>
#include <algorithm>

#include "Generic.h"

namespace PerlinNoise
{
	float fade(float t)
	{
		return t * t* t* (t * (t * 6 - 15) + 10);
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

		result.aaa = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(x) + y) + z);
		result.aab = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(x) + y) + zi);
		result.aba = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(x) + yi) + z);
		result.abb = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(x) + yi) + zi);
		result.baa = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(xi) + y) + z);
		result.bab = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(xi) + y) + zi);
		result.bba = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(xi) + yi) + z);
		result.bbb = Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(xi) + yi) + zi);

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
