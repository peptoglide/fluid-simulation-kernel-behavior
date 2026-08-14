using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;

public class ParticleFluid : MonoBehaviour
{
    public static ParticleFluid Instance { get; private set; }
    // Initial configuration
    [Header("Initial Configuration")]
    public int sidelength; 
    public float particleSpacing;
    public float mass = 1f;
    public float smoothingRadius = 1f;
    [Header("Random Config")]
    public bool isRandom;
    public int particleCountRandom;
    public Vector2 boundSize;
    [Header("Physics")]
    public float gravity = 9.81f;
    public float bounceDamping = 0.8f;
    public Vector2 simulationBounds = new Vector2(10f, 10f);
    public float predictStep = 0.016f; 
    [Header("Rendering")]
    public Color particleColor = Color.white;

    public Vector2[] velocities { get; private set; }
    public Vector2[] positions { get; private set; }
    public Vector2[] predictedPositions { get; private set; }
    public float[] fieldQuantities { get; private set; }
    public float[] densities { get; private set; }
    // Store grid cell of particles
    public int[] sortedParticles { get; private set; }
    public int particleCount { get; private set; }
    public float functionVolume { get; private set; }
    private Pressure pressureCalculator;
    private SpatialGrid grid;
    private bool isRunning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
        CalculatePositions();

        densities = new float[particleCount];
        velocities = new Vector2[particleCount];
        predictedPositions = new Vector2[particleCount];
        fieldQuantities = new float[particleCount];
        sortedParticles = new int[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            sortedParticles[i] = i;
        }

        functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 4) / 6f;

        pressureCalculator = GetComponent<Pressure>();
        grid = GetComponent<SpatialGrid>();
        isRunning = true;
    }

    // Simulation step
    void Update()
    {
        float deltaTime = Time.deltaTime;

        // Step ahead of time to stabilize simulation faster
        Parallel.For(0, particleCount, i =>
        {
            velocities[i] += Vector2.down * gravity * deltaTime;
            predictedPositions[i] = positions[i] + velocities[i] * predictStep;
            // Clamping positions
            predictedPositions[i].x = Mathf.Clamp(predictedPositions[i].x, -simulationBounds.x, simulationBounds.x);
            predictedPositions[i].y = Mathf.Clamp(predictedPositions[i].y, -simulationBounds.y, simulationBounds.y);
        });

        grid.UpdateSpatialGrid(predictedPositions);
        Parallel.For(0, particleCount, i =>
        {
            densities[i] = CalculateDensity(predictedPositions, i);
        });
        
        // Pressure
        Parallel.For(0, particleCount, i =>
        {
            Vector2 pressureForce = pressureCalculator.PressureGradientAtParticle(predictedPositions, i);
            velocities[i] += pressureForce / densities[i] * deltaTime;
        });

        // Updating positions
        Parallel.For(0, particleCount, i =>
        {
            positions[i] += velocities[i] * deltaTime;
            ResolveCollision(i);
        });
    }

    void ResolveCollision(int i)
    {
        if ((positions[i].x < -simulationBounds.x && velocities[i].x < 0) ||
        (positions[i].x > simulationBounds.x && velocities[i].x > 0))
        {
            velocities[i].x *= -bounceDamping; // Bounce back with damping
        }

        if ((positions[i].y < -simulationBounds.y && velocities[i].y < 0) || 
        (positions[i].y > simulationBounds.y && velocities[i].y > 0))
        {
            velocities[i].y *= -bounceDamping; // Bounce back with damping
        }

        float eps = 0f;
        // Clamping positions
        positions[i].x = Mathf.Clamp(positions[i].x, -simulationBounds.x + eps, simulationBounds.x - eps);
        positions[i].y = Mathf.Clamp(positions[i].y, -simulationBounds.y + eps, simulationBounds.y - eps);
    }

    void CalculatePositions()
    {
        if (!isRandom)
        {
            int numParticles = sidelength * sidelength;
            particleCount = numParticles;
            positions = new Vector2[numParticles];

            float midpoint = (sidelength - 1) * particleSpacing / 2.0f;
            for (int i = 0; i < sidelength; i++)
            {
                for (int j = 0; j < sidelength; j++)
                {
                    float x = i * particleSpacing - midpoint;
                    float y = j * particleSpacing - midpoint;
                    positions[i * sidelength + j] = new Vector2(x, y);
                }
            }
        }
        else
        {
            // Distribute randomly
            int seed = 3679;
            System.Random rng = new System.Random(seed);
            particleCount = particleCountRandom;
            positions = new Vector2[particleCountRandom];

            for (int i = 0; i < particleCountRandom; i++)
            {
                float x = -boundSize.x + (float)rng.NextDouble() * (boundSize.x * 2);
                float y = -boundSize.y + (float)rng.NextDouble() * (boundSize.y * 2);
                positions[i] = new Vector2(x, y);
            }
        }
    }

    // Divide by volume to ensure normalized kernel i.e Integral all = 1
    public float SmoothingKernel(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        return (radius - distance) * (radius - distance) / functionVolume; // (r-d)^2 for steeper derivatives near 0
    }

    // Derivative of kernel function
    public float SmoothingKernelDerivative(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        return -2f * (radius - distance) / functionVolume; // Derivative
    }

    public float CalculateDensity(Vector2[] positions, int particleId)
    {
        float density = 0f;
        // Only look at particles within a 3x3 square
        Vector2 position = positions[particleId];
        Vector2Int gridCell = grid.GetGridCell(position);
        for (int x = gridCell.x - 1; x <= gridCell.x + 1; x++)
        {
            for (int y = gridCell.y - 1; y <= gridCell.y + 1; y++)
            {
                if (x < 0 || x >= grid.width || y < 0 || y >= grid.height)
                    continue;

                int gridId = y * grid.width + x;
                int startIdx = grid.startLocations[gridId];
                int endIdx = (gridId + 1 == grid.width * grid.height) ? particleCount : grid.startLocations[gridId + 1];

                for (int i = startIdx; i < endIdx; i++)
                {
                    Vector2 particlePos = predictedPositions[sortedParticles[i]];
                    float distance = Vector2.Distance(position, particlePos);
                    density += SmoothingKernel(smoothingRadius, distance);
                }
            }
        }
        if (density == 0) {
            Debug.Log($"Cell {gridCell} at {position}");
            int gridId = gridCell.y * grid.width + gridCell.x;
            int start = grid.startLocations[gridId];
            int end = (gridId + 1 == grid.width * grid.height) ? particleCount : grid.startLocations[gridId + 1];
            Debug.Log($"Start: {start}, End: {end}");
            for (int i = start; i < end; i++)
            {
                Vector2 particlePos = positions[sortedParticles[i]];
                float distance = Vector2.Distance(position, particlePos);
                Debug.Log($"Particle {sortedParticles[i]} at {particlePos}, distance: {distance}");
            }
            Debug.LogError($"Particle {position} has zero density.");
        }
        return density;
    }

    void OnDrawGizmos()
    {
        if (!isRunning)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector2.zero, simulationBounds * 2f);
            if (sidelength >= 75) return; // Avoid drawing too many particles in the editor

            CalculatePositions();

            Gizmos.color = particleColor;
            for (int i = 0; i < particleCount; i++)
            {
                Gizmos.DrawSphere(positions[i], 0.05f);
            }
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector2.zero, simulationBounds * 2f);

        Gizmos.color = particleColor;
        for (int i = 0; i < particleCount; i++)
        {
            Gizmos.DrawSphere(positions[i], 0.05f);
        }
    }
}
