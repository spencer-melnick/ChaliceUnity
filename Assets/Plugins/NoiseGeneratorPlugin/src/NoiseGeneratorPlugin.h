#pragma once

#define NOISEGENERATORPLUGIN_API __declspec(dllexport)

extern "C"
{
	NOISEGENERATORPLUGIN_API void SeedGenerator(unsigned int seed);


	NOISEGENERATORPLUGIN_API float SamplePerlinNoiseOctaves(float x, float y, float z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ,
		float octaves, float persistence);

	NOISEGENERATORPLUGIN_API void GeneratePerlinNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
		unsigned int scaleX, unsigned int scaleY,
		float octaves, float persistence,
		float contrast,
		float valueMin, float valueMax, float remapMin, float remapMax,
		float data[]);

	NOISEGENERATORPLUGIN_API void GeneratePerlinNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
		unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
		float octaves, float persistence,
		float contrast,
		float valueMin, float valueMax, float remapMin, float remapMax,
		float data[]);


	NOISEGENERATORPLUGIN_API float SampleWorleyNoiseOctaves(float x, float y, float z,
		unsigned int tilesX, unsigned int tilesY, unsigned int tilesZ,
		float octaves, float persistence);

	NOISEGENERATORPLUGIN_API void GenerateWorleyNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
		unsigned int scaleX, unsigned int scaleY,
		float octaves, float persistence,
		float contrast,
		float valueMin, float valueMax, float remapMin, float remapMax,
		float data[]);

	NOISEGENERATORPLUGIN_API void GenerateWorleyNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
		unsigned int scaleX, unsigned int scaleY, unsigned int scaleZ,
		float octaves, float persistence,
		float contrast,
		float valueMin, float valueMax, float remapMin, float remapMax,
		float data[]);
}
