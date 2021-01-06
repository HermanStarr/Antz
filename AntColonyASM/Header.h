#pragma once
#ifdef ANTCOLONYASMs_EXPORTS
#define ANTCOLONYASM_API __declspec(dllexport)
#else
#define ANTCOLONYASM_API __declspec(dllimport)
#endif

extern "C" ANTCOLONYASM_API void GetDistancesLoopASM(float* xs, float* ys, float* dist, float x, float y);