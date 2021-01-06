#pragma once
#include "pch.h"
#include "Header.h"
#include <iostream>
#include <stdio.h>
#include <emmintrin.h>
#include <immintrin.h>

void GetDistancesC(float* distances, float* xs, float* ys, float x, float y)
{
	for (int i = 0; i < 8; i++)
	{
		distances[i] = static_cast<float>(std::sqrt(pow(xs[i] - x, 2) + pow(ys[i] - y, 2)));
	}
	return;
}


void GetDistances256C(float* distances, float* xs, float* ys, float x, float y)
{
	__m256* distancesr = (__m256*)distances;		
	__m256* xsr = (__m256*)xs;						
	__m256* ysr = (__m256*)ys;						
							
	__m256* xr = &_mm256_broadcast_ss(&x);			
	__m256* yr = &_mm256_broadcast_ss(&y);			
	__m256* sub1 = &_mm256_sub_ps(*xr, *xsr);		
	__m256* sub2 = &_mm256_sub_ps(*yr, *ysr);		
	__m256* square1 = &_mm256_mul_ps(*sub1, *sub1);	
	__m256* square2 = &_mm256_mul_ps(*sub2, *sub2);	
	__m256* sum = &_mm256_add_ps(*square1, *square2);			
		__m256* root = &_mm256_sqrt_ps(*sum);						
	*distancesr = *root;										
	return;
}
