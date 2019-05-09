
#define NOISEGENERATORPLUGIN_API __declspec(dllexport)

extern "C"
{
	NOISEGENERATORPLUGIN_API void SeedGenerator(unsigned int seed);
	NOISEGENERATORPLUGIN_API float SamplePerlinNoise(float x, float y, float z, bool repeat);
	NOISEGENERATORPLUGIN_API float SamplePerlinNoiseOctaves(float x, float y, float z, float octaves, float persistence);

	NOISEGENERATORPLUGIN_API void GeneratePerlinNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
		float scaleX, float scaleY,
		float octaves, float persistence, float contrast, float data[]);

	NOISEGENERATORPLUGIN_API void GeneratePerlinNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
		float scaleX, float scaleY, float scaleZ,
		float octaves, float persistence, float contrast, float data[]);
}
