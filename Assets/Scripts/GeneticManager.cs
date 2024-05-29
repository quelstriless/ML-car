using System.Collections.Generic;
using UnityEngine;

public class GeneticManager : MonoBehaviour
{
    [Header("References")]
    public CarController controller;

    [Header("Controls")]
    public int initialPopulation = 85;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.055f;
    public int tournamentSize = 5;

    [Header("Crossover Controls")]
    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> genePool = new List<int>();

    private int naturallySelected;

    private NNet[] population;

    [Header("Public View")]
    public int currentGeneration;
    public int currentGenome;

    private void Start()
    {
        CreatePopulation();
    }

    private void CreatePopulation()
    {
        population = new NNet[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
    }

    private void ResetToCurrentGenome()
    {
        controller.ResetWithNetwork(population[currentGenome]);
    }

    private void FillPopulationWithRandomValues(NNet[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NNet();
            newPopulation[startingIndex].Initialize(controller.LAYERS, controller.NEURONS);
            startingIndex++;
        }
    }

    public void Death(float fitness, NNet network)
    {
        if (currentGenome < population.Length - 1)
        {
            population[currentGenome].fitness = fitness;
            currentGenome++;
            ResetToCurrentGenome();
        }
        else
        {
            RePopulate();
        }
    }

    private void RePopulate()
    {
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;
        SortPopulation();

        NNet[] newPopulation = PickBestPopulation();

        Crossover(newPopulation);
        Mutate(newPopulation);
        FillPopulationWithRandomValues(newPopulation, naturallySelected);

        population = newPopulation;
        currentGenome = 0;
        ResetToCurrentGenome();
    }

    private void Mutate(NNet[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++)
        {
            AdaptiveMutate(newPopulation[i], mutationRate);
        }
    }

    private void Crossover(NNet[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i += 2)
        {
            NNet parent1 = TournamentSelection(tournamentSize);
            NNet parent2 = TournamentSelection(tournamentSize);

            NNet child1 = new NNet();
            NNet child2 = new NNet();

            child1.Initialize(controller.LAYERS, controller.NEURONS);
            child2.Initialize(controller.LAYERS, controller.NEURONS);

            child1.fitness = 0;
            child2.fitness = 0;

            UniformCrossover(parent1, parent2, child1, child2);

            newPopulation[naturallySelected] = child1;
            naturallySelected++;
            newPopulation[naturallySelected] = child2;
            naturallySelected++;
        }
    }

    private NNet TournamentSelection(int tournamentSize)
    {
        NNet best = null;

        for (int i = 0; i < tournamentSize; i++)
        {
            int randomIndex = Random.Range(0, population.Length);
            NNet contender = population[randomIndex];
            if (best == null || contender.fitness > best.fitness)
            {
                best = contender;
            }
        }

        return best;
    }

    private void UniformCrossover(NNet parent1, NNet parent2, NNet child1, NNet child2)
    {
        for (int w = 0; w < parent1.weights.Count; w++)
        {
            for (int row = 0; row < parent1.weights[w].RowCount; row++)
            {
                for (int col = 0; col < parent1.weights[w].ColumnCount; col++)
                {
                    if (Random.Range(0f, 1f) < 0.5f)
                    {
                        child1.weights[w][row, col] = parent1.weights[w][row, col];
                        child2.weights[w][row, col] = parent2.weights[w][row, col];
                    }
                    else
                    {
                        child1.weights[w][row, col] = parent2.weights[w][row, col];
                        child2.weights[w][row, col] = parent1.weights[w][row, col];
                    }
                }
            }
        }

        for (int b = 0; b < parent1.biases.Count; b++)
        {
            if (Random.Range(0f, 1f) < 0.5f)
            {
                child1.biases[b] = parent1.biases[b];
                child2.biases[b] = parent2.biases[b];
            }
            else
            {
                child1.biases[b] = parent2.biases[b];
                child2.biases[b] = parent1.biases[b];
            }
        }
    }

    private void AdaptiveMutate(NNet network, float mutationRate)
    {
        for (int w = 0; w < network.weights.Count; w++)
        {
            for (int row = 0; row < network.weights[w].RowCount; row++)
            {
                for (int col = 0; col < network.weights[w].ColumnCount; col++)
                {
                    if (Random.Range(0f, 1f) < mutationRate)
                    {
                        network.weights[w][row, col] = Mathf.Clamp(network.weights[w][row, col] + Random.Range(-1f, 1f), -1f, 1f);
                    }
                }
            }
        }

        for (int b = 0; b < network.biases.Count; b++)
        {
            if (Random.Range(0f, 1f) < mutationRate)
            {
                network.biases[b] = Mathf.Clamp(network.biases[b] + Random.Range(-1f, 1f), -1f, 1f);
            }
        }
    }

    private NNet[] PickBestPopulation()
    {
        NNet[] newPopulation = new NNet[initialPopulation];

        for (int i = 0; i < bestAgentSelection; i++)
        {
            newPopulation[naturallySelected] = population[i].InitiliseCopy(controller.LAYERS, controller.NEURONS);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;

            int f = Mathf.RoundToInt(population[i].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(i);
            }
        }

        for (int i = 0; i < worstAgentSelection; i++)
        {
            int last = population.Length - 1;
            last -= i;

            int f = Mathf.RoundToInt(population[i].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(last);
            }
        }

        return newPopulation;
    }

    private void SortPopulation()
    {
        System.Array.Sort(population, (a, b) => b.fitness.CompareTo(a.fitness));
    }
}
