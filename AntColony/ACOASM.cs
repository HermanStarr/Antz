using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace AntColony
{

    class ACOASM
    {
        private float defaultWeight = 1f;
        private int alpha = 2;
        private int beta = 12;
        private float evaporation = 0.5f;
        private float Q = 500f;
        private float antFactor = 0.8f;
        private float randomFactor = 0.01f;


        private int numberOfCities;
        private int numberOfAnts;
        private int numberOfIterations = 1000;
        private int numberOfThreads;
        private int numberOfPackages;

        private TwoDOneD<float> graph;
        private TwoDOneD<float> contributions;
        private List<Ant> ants = new List<Ant>();
        private Random random = new Random();

        private int currentIndex;
        //private int antit;

        public int[] bestTourOrder;
        public float bestTourLength;

        IntPtr[] packedTrails;
        IntPtr[] packedContributions;
        IntPtr[] packedGraph;
        IntPtr numerators;

        bool allocHere = false;

        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void ClearTrails256ASM(IntPtr trails, float c, int size);
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern int CalculateCityNumberASM(
            IntPtr visited, IntPtr trailsColumn, IntPtr graphColumn, IntPtr numerators,
            int alpha, int beta, int pointerCount, float r);
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void UpdateEvaporationASM(IntPtr trails, float evaporation, int size);

        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void AddContributionsToTrails(IntPtr contributions, IntPtr trails, int number);

        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void UpdateTrails(IntPtr trails, IntPtr contributions, float evaporation, int size);
        [DllImport("C:/Users/KamilPC/source/repos/AntColony/x64/Release/AntColonyASM.dll")]
        public static extern void ChangeFloat(IntPtr trails, int index, float val);


        public ACOASM(int nC, float[] gr, int nT, IntPtr[] distances = null)
        {
            numberOfCities = nC;
            numberOfAnts = (int)(numberOfCities * antFactor);
            numberOfThreads = 1;

            graph = new TwoDOneD<float>(gr, numberOfCities);
            contributions = new TwoDOneD<float>(new float[numberOfCities * numberOfCities], numberOfCities);

            if (numberOfCities % 8 != 0)
                numberOfPackages = (int)(numberOfCities / 8) + 1;
            else
                numberOfPackages = numberOfCities / 8;

            Parallel.For(0, numberOfAnts, new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads }, i =>
            {
                ants.Add(new Ant(numberOfCities, numberOfPackages));
            });

            packedTrails = new IntPtr[numberOfCities];
            packedContributions = new IntPtr[numberOfCities];

            if (distances == null)
            {
                packedGraph = new IntPtr[numberOfCities];

                for (int i = 0; i < numberOfCities; i++)
                {
                    packedTrails[i] = Marshal.AllocHGlobal(sizeof(float) * numberOfPackages * 8);
                    packedContributions[i] = Marshal.AllocHGlobal(sizeof(float) * numberOfPackages * 8);
                    packedGraph[i] = Marshal.AllocHGlobal(sizeof(float) * numberOfPackages * 8);
                    Marshal.Copy(graph.GetRow(i), 0, packedGraph[i], numberOfCities);
                    for (int j = 0; j < (int)(numberOfPackages * 8 - numberOfCities); j++)
                    {
                        ChangeFloat(packedGraph[i], numberOfCities + j, float.PositiveInfinity);
                    }
                }
                allocHere = true;
            }
            else
            {
                packedGraph = distances;

                for (int i = 0; i < numberOfCities; i++)
                {
                    packedTrails[i] = Marshal.AllocHGlobal(sizeof(float) * numberOfPackages * 8);
                    packedContributions[i] = Marshal.AllocHGlobal(sizeof(float) * numberOfPackages * 8);

                }
            }
            numerators = Marshal.AllocHGlobal(sizeof(float) * numberOfPackages * 8);
            bestTourLength = float.MaxValue;
        }



        ~ACOASM()
        {
            if (allocHere)
            {
                for (int i = 0; i < numberOfCities; i++)
                {
                    Marshal.FreeHGlobal(packedTrails[i]);
                    Marshal.FreeHGlobal(packedContributions[i]);
                    Marshal.FreeHGlobal(packedGraph[i]);
                }
                Marshal.FreeHGlobal(numerators);
            }
            else
            {
                for (int i = 0; i < numberOfCities; i++)
                {
                    Marshal.FreeHGlobal(packedTrails[i]);
                    Marshal.FreeHGlobal(packedContributions[i]);
                }
                Marshal.FreeHGlobal(numerators);
            }
        }

        public void start(int numberOfAttempts)
        {
            for(int i = 0; i < numberOfAttempts; i++)
            {
                solve();
            }
        }
        private void solve()
        {
            //Place ants in their first cities
            setUpAnts();
            //Set trail weigths to default state
            clearTrails();

            for (int i = 0; i < numberOfIterations; i++)
            {
                //Move all ants through all the cities
                moveAnts();
                //Update trail weights
                updateTrails();
                //Check for new best length and order
                updateBest();
            }

        }

        private void setUpAnts()
        {
            //Choose random starting city for each ant
            foreach (var ant in ants)
            {
                //Clear visitation flags
                ant.packedClear();
                //Set random city
                int cN = random.Next(0, numberOfCities);
                ant.trail[0] = cN;
                ant.reversedVisited[cN] = true;
            }
            currentIndex = 0;
        }

        private void moveAnts()
        {

            //For number of cities in a list
            for (int i = currentIndex; i < numberOfCities - 1; i++)
            {

                //For every ant
                Parallel.For(0, ants.Count(), new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads }, antit =>
                {
                    int numberOfCity = -1;
                    //Should the city be found on random
                    if ((float)random.NextDouble() < randomFactor)
                    {
                        //Choose random count of non visited cities
                        int t = random.Next(0, numberOfCities - currentIndex);
                        for (int j = 0; j < numberOfCities; j++)
                        {
                            if (!ants[antit].reversedVisited[j])
                            {
                                if (t == 0)
                                {
                                    numberOfCity = j;
                                    break;
                                }
                                t--;
                            }
                        }
                    }
                    //If no random guessing today
                    if(numberOfCity == -1)
                    {
                        int currentIndexIterator = ants[antit].trail[currentIndex];
                        //Calculate city number based on the probabilities
                
                            numberOfCity = CalculateCityNumberASM(ants[antit].packedVisited, packedTrails[currentIndexIterator], packedGraph[currentIndexIterator], numerators,
                                alpha, beta, numberOfPackages, (float)random.NextDouble());
                
                    }
                    //Visit the city
                    ants[antit].trail[currentIndex + 1] = numberOfCity;
                    ants[antit].reversedVisited[numberOfCity] = true;                  
                });

                currentIndex++;
            }
        }


        private void updateTrails()
        {
            //For every ant get weeight contribution
            foreach (var a in ants)
            {
                float contribution = Q / a.trailLength(graph.input);
                //Update contributions between each city
                for (int i = 0; i < numberOfCities - 1; i++)
                {
                    contributions[a.trail[i], a.trail[i + 1]] += contribution;
                }
                contributions[a.trail[numberOfCities - 1], a.trail[0]] += contribution;
            }
            //Add contributions to trails
            Parallel.For(0, numberOfCities, new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads }, i =>
            {
                //Copy contributions to contributions pointer
                Marshal.Copy(contributions.GetRow(i), 0, packedContributions[i], numberOfCities);
                //Update trails and add given contributions to trails
                UpdateTrails(packedTrails[i], packedContributions[i], evaporation, numberOfPackages);
            });    
            //Set contributions to 0
            Array.Clear(contributions.input, 0, numberOfCities * numberOfCities);
        }

        private void updateBest()
        {
            foreach (var ant in ants)
            {
                if (ant.trailLength(graph.input) < bestTourLength)
                {
                    bestTourLength = ant.trailLength(graph.input);
                    bestTourOrder = (int[])ant.trail.Clone();
                }
                ant.packedClear();
            }
        }

        private void clearTrails()
        {
            Parallel.For(0, numberOfCities, new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads }, i =>
            {
                ClearTrails256ASM(packedTrails[i], defaultWeight, numberOfPackages);
            });
        }
    }

}
