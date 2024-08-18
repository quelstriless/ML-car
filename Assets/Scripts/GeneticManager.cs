using MathNet.Numerics.LinearAlgebra;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticManager : MonoBehaviour
{
    [Header("References")]
    public CarController carPrefab; // Reference to the car prefab
    public Transform startPosition; // Start position for cars
    public Camera mainCamera; // Reference to the main camera

    [Header("Controls")]
    public int initialPopulation = 85;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")]
    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> genePool = new List<int>();

    private int naturallySelected;

    private NNet[] population;

    [Header("Public View")]
    public int currentGeneration;

    private CarController[] carControllers;
    private CarController bestCar;

    private void Start()
    {
        CreatePopulation();
        AttachCameraToBestCar();
    }

    private void Update()
    {
        AttachCameraToBestCar();
    }

    private void CreatePopulation()
    {
        population = new NNet[initialPopulation];
        carControllers = new CarController[initialPopulation];
        for (int i = 0; i < initialPopulation; i++)
        {
            carControllers[i] = Instantiate(carPrefab, startPosition.position, startPosition.rotation);
        }
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
    }

    private void ResetToCurrentGenome()
    {
        for (int i = 0; i < carControllers.Length; i++)
        {
            carControllers[i].ResetWithNetwork(population[i]);
        }
    }

    private void FillPopulationWithRandomValues(NNet[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation)
        {
            NNet nnet = new GameObject().AddComponent<NNet>();
            newPopulation[startingIndex] = nnet;
            newPopulation[startingIndex].Initialize(carControllers[0].LAYERS, carControllers[0].NEURONS);
            startingIndex++;
        }
    }

    public void Death(CarController carController, float fitness, NNet network)
    {
        for (int i = 0; i < carControllers.Length; i++)
        {
            if (carControllers[i] == carController)
            {
                population[i].fitness = fitness;
                carControllers[i].gameObject.SetActive(false); // Deactivate the car
                break;
            }
        }

        bool allDead = true;
        for (int i = 0; i < carControllers.Length; i++)
        {
            if (carControllers[i].gameObject.activeSelf)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
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

        ResetToCurrentGenome();
    }

    private void Mutate(NNet[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++)
        {
            for (int c = 0; c < newPopulation[i].weights.Count; c++)
            {
                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[i].weights[c] = MutateMatrix(newPopulation[i].weights[c]);
                }
            }
        }
    }

    private Matrix<float> MutateMatrix(Matrix<float> A)
    {
        int randomPoints = Random.Range(1, (A.RowCount * A.ColumnCount) / 7);

        Matrix<float> C = A;

        for (int i = 0; i < randomPoints; i++)
        {
            int randomColumn = Random.Range(0, C.ColumnCount);
            int randomRow = Random.Range(0, C.RowCount);

            C[randomRow, randomColumn] = Mathf.Clamp(C[randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
        }

        return C;
    }

    private void Crossover(NNet[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i += 2)
        {
            int AIndex = i;
            int BIndex = i + 1;

            if (genePool.Count >= 1)
            {
                for (int l = 0; l < 100; l++)
                {
                    AIndex = genePool[Random.Range(0, genePool.Count)];
                    BIndex = genePool[Random.Range(0, genePool.Count)];

                    if (AIndex != BIndex)
                        break;
                }
            }

            NNet Child1 = new GameObject().AddComponent<NNet>();
            NNet Child2 = new GameObject().AddComponent<NNet>();

            Child1.Initialize(carControllers[0].LAYERS, carControllers[0].NEURONS);
            Child2.Initialize(carControllers[0].LAYERS, carControllers[0].NEURONS);

            Child1.fitness = 0;
            Child2.fitness = 0;

            for (int w = 0; w < Child1.weights.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.weights[w] = population[AIndex].weights[w];
                    Child2.weights[w] = population[BIndex].weights[w];
                }
                else
                {
                    Child2.weights[w] = population[AIndex].weights[w];
                    Child1.weights[w] = population[BIndex].weights[w];
                }
            }

            for (int w = 0; w < Child1.biases.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.biases[w] = population[AIndex].biases[w];
                    Child2.biases[w] = population[BIndex].biases[w];
                }
                else
                {
                    Child2.biases[w] = population[AIndex].biases[w];
                    Child1.biases[w] = population[BIndex].biases[w];
                }
            }

            newPopulation[naturallySelected] = Child1;
            naturallySelected++;

            newPopulation[naturallySelected] = Child2;
            naturallySelected++;
        }
    }

    private NNet[] PickBestPopulation()
    {
        NNet[] newPopulation = new NNet[initialPopulation];

        for (int i = 0; i < bestAgentSelection; i++)
        {
            newPopulation[naturallySelected] = population[i].InitiliseCopy(carControllers[0].LAYERS, carControllers[0].NEURONS);
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

            int f = Mathf.RoundToInt(population[last].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(last);
            }
        }

        return newPopulation;
    }

    private void SortPopulation()
    {
        for (int i = 0; i < population.Length; i++)
        {
            for (int j = i; j < population.Length; j++)
            {
                if (population[i].fitness < population[j].fitness)
                {
                    NNet temp = population[i];
                    population[i] = population[j];
                    population[j] = temp;
                }
            }
        }
    }

    private void AttachCameraToBestCar()
    {
        if (carControllers.Length == 0) return;

        float bestFitness = float.MinValue;
        CarController bestCar = null;

        foreach (var car in carControllers)
        {
            if (car.gameObject.activeSelf && car.GetComponent<NNet>().fitness > bestFitness)
            {
                bestFitness = car.GetComponent<NNet>().fitness;
                bestCar = car;
            }
        }

        if (bestCar != null && bestCar != this.bestCar)
        {
            this.bestCar = bestCar;
            mainCamera.transform.SetParent(bestCar.transform);
            mainCamera.transform.localPosition = new Vector3(0, 2, -5); // Adjust the position as needed
            mainCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
