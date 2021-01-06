#pragma once
#include "pch.h"
#include "Header.h"


void CreateRandomCities(int number_of_cities, char* path)
{
    std::ofstream cities;
    cities.open(path);
    if (cities.is_open())
    {
        std::random_device r;

        std::default_random_engine e1(r());
        std::uniform_int_distribution<int> uniform_dist(3, 15);
        std::uniform_int_distribution<int> uniform_dist2(97, 122);
        std::seed_seq seed2{ r(), r(), r(), r(), r(), r(), r(), r() };
        std::mt19937 e2(r());
        std::uniform_real_distribution<> normal_dist(-500, 500);

        for (int i = 0; i < number_of_cities; i++)
        {

            std::string name;

            int nLetters = uniform_dist(e1);
            
            name = name + static_cast<char>(uniform_dist2(e1) - 32);

            for (int j = 0; j < nLetters - 1; j++)
            {
                name = name + static_cast<char>(uniform_dist2(e1));
            }

            float x = static_cast<float>(normal_dist(e2));
            float y = static_cast<float>(normal_dist(e2));

            cities << name + "," + std::to_string(x) + "," + std::to_string(y) << std::endl;
        }
        cities.close();
    }
}