// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the PERLINNOISEPLUGIN_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// PERLINNOISEPLUGIN_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef PERLINNOISEPLUGIN_EXPORTS
#define PERLINNOISEPLUGIN_API __declspec(dllexport)
#else
#define PERLINNOISEPLUGIN_API __declspec(dllimport)
#endif

extern "C"
{
	PERLINNOISEPLUGIN_API void SeedGenerator(unsigned int seed);
	PERLINNOISEPLUGIN_API float SamplePerlinNoise(float x, float y, float z, bool repeat);
	PERLINNOISEPLUGIN_API float SamplePerlinNoiseOctaves(float x, float y, float z, float octaves, float persistence);

	PERLINNOISEPLUGIN_API void GeneratePerlinNoiseImage2D(unsigned int resolutionX, unsigned int resolutionY,
		float scaleX, float scaleY,
		float octaves, float persistence, float contrast, float data[]);

	PERLINNOISEPLUGIN_API void GeneratePerlinNoiseImage3D(unsigned int resolutionX, unsigned int resolutionY, unsigned int resolutionZ,
		float scaleX, float scaleY, float scaleZ,
		float octaves, float persistence, float contrast, float data[]);
}
