#pragma once
#ifdef GENERATOR_EXPORTS
#define GENERATOR_API __declspec(dllexport)
#else
#define GENERATOR_API __declspec(dllimport)
#endif

extern "C" GENERATOR_API void CreateRandomCities(int number_of_cities, char* path);