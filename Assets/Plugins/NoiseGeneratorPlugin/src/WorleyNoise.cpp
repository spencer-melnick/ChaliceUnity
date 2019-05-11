#include "WorleyNoise.h"

#include "Generic.h"

namespace WorleyNoise
{
	struct Vector3
	{
		float x, y, z;
	};

	int wrapValue(int k, int n)
	{
		return ((k %= n) < 0) ? k + n : k;
	}

	unsigned int hashCoord(unsigned int x, unsigned int y, unsigned int z, unsigned int offset)
	{
		return Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(Generic::hashLookup(x) + y) + z) + offset);
	}

	Vector3 getRandomOffset(int x, int y, int z, unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ)
	{
		x = wrapValue(x, tilesX);
		y = wrapValue(y, tilesY);
		z = wrapValue(z, tilesZ);

		Vector3 offset;

		offset.x = static_cast<float>(hashCoord(x, y, z, 0)) / static_cast<float>(Generic::numPermutations);
		offset.y = static_cast<float>(hashCoord(x, y, z, 1)) / static_cast<float>(Generic::numPermutations);
		offset.z = static_cast<float>(hashCoord(x, y, z, 2)) / static_cast<float>(Generic::numPermutations);

		return offset;
	}

	float sampleNoise(float x, float y, float z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ)
	{
		int startingCellX = static_cast<int>(std::floor(x));
		int startingCellY = static_cast<int>(std::floor(y));
		int startingCellZ = static_cast<int>(std::floor(z));

		Vector3 localPosition;

		localPosition.x = x - static_cast<float>(startingCellX);
		localPosition.y = y - static_cast<float>(startingCellY);
		localPosition.z = z - static_cast<float>(startingCellZ);

		int cellX, cellY, cellZ;

		Vector3 lookupPosition, difference;

		float minDistance = 1.0f;

		for (int i = -1; i <= 1; i++)
		{
			cellX = startingCellX + i;

			for (int j = -1; j <= 1; j++)
			{
				cellY = startingCellY + j;

				for (int k = -1; k <= 1; k++)
				{
					cellZ = startingCellZ + k;

					lookupPosition = getRandomOffset(cellX, cellY, cellZ, tilesX, tilesY, tilesZ);

					lookupPosition.x += static_cast<float>(i);
					lookupPosition.y += static_cast<float>(j);
					lookupPosition.z += static_cast<float>(k);

					difference.x = lookupPosition.x - localPosition.x;
					difference.y = lookupPosition.y - localPosition.y;
					difference.z = lookupPosition.z - localPosition.z;

					minDistance = std::min(minDistance, std::sqrtf(difference.x * difference.x + difference.y * difference.y + difference.z * difference.z));
				}
			}
		}

		return minDistance;
	}
}
