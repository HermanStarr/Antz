using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace AntColony
{
    public class ACO
    {
        private float c = 1f;
        private float alpha = 2f;
        private float beta = 10f;
        private float evaporation = 0.5f;
        private float Q = 5000f;
        private float antFactor = 0.8f;
        private float randomFactor = 0.01f;

        private int maxIterations = 10000;

        private int numberOfCities;
        private int numberOfAnts;
        private TwoDOneD<float> graph;
        public static TwoDOneD<float> trails;
        private List<Ant> ants = new List<Ant>();
        private Random random = new Random();
        private float[] probabilities;

        private int currentIndex;

        public int[] bestTourOrder;
        public float bestTourLength;

        public void setACO(float alpha = 2f, float beta = 12f, float evaporation = 0.5f, float Q = 500f, float antFactor = 8f, float randomFactor = 0.01f)
        {
            this.alpha = alpha;
            this.beta = beta;
            this.evaporation = evaporation;
            this.Q = Q;
            this.antFactor = antFactor;
            this.randomFactor = randomFactor;
        }

        public ACO(int nrOfCities, float[] externGraph)
        {
            graph = new TwoDOneD<float>(externGraph, nrOfCities);
            numberOfCities = nrOfCities;
            numberOfAnts = (int)(numberOfCities * antFactor);

            trails = new TwoDOneD<float>(new float[numberOfCities * numberOfCities], numberOfCities);
            probabilities = new float[numberOfCities];
            for (int i = 0; i < numberOfAnts; i++)
                ants.Add(new Ant(numberOfCities));
        }
        public void startOptimizing(int attempts)
        {
            for(int i = 0; i < attempts; i++)
            {
                solve(i);
            }
        }
        private void solve(int attempt)
        {
            setUpAnts();
            clearTrails();
            for(int i = 0; i < maxIterations; i++)
            {

                moveAnts();
                updateTrails();
                updateBest();
            }

        }
        private void setUpAnts()
        {
            foreach(var ant in ants)
            {
                ant.clear();
                ant.visitCity(-1, random.Next(0, numberOfCities));
            }
            currentIndex = 0;
        }
        private void moveAnts()
        {
            for (int i = currentIndex; i < numberOfCities - 1; i++)
            {
                foreach (var ant in ants)
                { 
                    ant.visitCity(currentIndex, selectNextCity(ant));
                }

                currentIndex++;
            }
        }
        private int selectNextCity(Ant ant)
        {
            //Check if ant should go to other city
            if((float)random.NextDouble() < randomFactor)
            {
                int t = random.Next(0, numberOfCities - currentIndex);
                //Scan for not visited cities
                for(int i = 0; i < numberOfCities; i++)
                {
                    if (i == t && !ant.getVisited(i))
                        return i;              
                }
            }
            calculateProbabilities(ant);
            float r = (float)random.NextDouble();
            float total = 0;
            for (int i = 0; i < numberOfCities; i++)
            {
                total += probabilities[i];
                if (total >= r)
                {
                    return i;
                }
            }


            throw new SystemException("There are no other cities");
        }
        private void calculateProbabilities(Ant ant)
        {
            int i = ant.trail[currentIndex];
            float pheromone = 0.0f;
            float[] numerators = new float[numberOfCities];
            for (int j = 0; j < numberOfCities; j++)
            {
                if (!ant.getVisited(j))
                {
                    numerators[j] = (float)(Math.Pow(trails[i, j], alpha) * Math.Pow(1.0 / graph[i, j], beta));
                    pheromone += numerators[j];
                }
            }
            for (int j = 0; j < numberOfCities; j++)
            {
                if (ant.getVisited(j))
                {
                    probabilities[j] = 0.0f;
                }
                else
                {
                    probabilities[j] = numerators[j] / pheromone;
                }
            }
        }
        private void updateTrails()
        {
            for (int i = 0; i < numberOfCities; i++)
            {
                for (int j = 0; j < numberOfCities; j++)
                {
                    trails[i,j] *= evaporation;
                }
            }
            foreach (var a in ants)
            {
                float contribution = Q / a.trailLength(graph.input);
                for (int i = 0; i < numberOfCities - 1; i++)
                {
                    trails[a.trail[i], a.trail[i + 1]] += contribution;
                }
                trails[a.trail[numberOfCities - 1], a.trail[0]] += contribution;
            }
        }
        private void updateBest()
        {
            if (bestTourOrder == null)
            {
                bestTourOrder = ants.ElementAt(0).trail;
                bestTourLength = ants.ElementAt(0).trailLength(graph.input);
            }
            foreach (var ant in ants)
            {
                if (ant.trailLength(graph.input) < bestTourLength)
                {
                    bestTourLength = ant.trailLength(graph.input);
                    bestTourOrder = (int[])ant.trail.Clone();
                }
                ant.clear();
            }
        }
        private void clearTrails()
        {
            for (int i = 0; i < numberOfCities; i++)
                for (int j = 0; j < numberOfCities; j++)
                    trails[i, j] = c;
        }
    }
}
