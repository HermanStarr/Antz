#pragma once
#ifdef ANTCOLONYC_EXPORTS
#define ANTCOLONYC_API __declspec(dllexport)
#else
#define ANTCOLONYC_API __declspec(dllimport)
#endif

extern "C" ANTCOLONYC_API void GetDistancesC(float* distances, float* xs, float* ys, float x, float y);
extern "C" ANTCOLONYC_API void GetDistances256C(float* distances, float* xs, float* ys, float x, float y);
