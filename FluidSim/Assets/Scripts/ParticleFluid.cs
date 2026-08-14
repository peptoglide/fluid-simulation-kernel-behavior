using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Android.Gradle.Manifest;

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
    [Header("Rendering")]
    public Color particleColor = Color.white;

    public Vector2[] velocities { get; private set; }
    public Vector2[] positions { get; private set; }
    public Vector2[] previousLocations { get; private set; }
    public float[] fieldQuantities { get; private set; }
    public float[] densities { get; private set; }
    public int particleCount { get; private set; }
    public float functionVolume { get; private set; }
    private Pressure pressureCalculator;
    private bool isRunning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
        CalculatePositions();

        densities = new float[particleCount];
        velocities = new Vector2[particleCount];
        fieldQuantities = new float[particleCount];

        functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 4) / 6f;

        pressureCalculator = GetComponent<Pressure>();
        isRunning = true;
    }

    // Simulation step
    void Update()
    {
        float deltaTime = Time.deltaTime;
        Parallel.For(0, particleCount, i =>
        {
            velocities[i] += Vector2.down * gravity * deltaTime;
            densities[i] = CalculateDensity(positions[i]);
        });

        // Pressure
        Parallel.For(0, particleCount, i =>
        {
            Vector2 pressureForce = pressureCalculator.PressureGradientAtParticle(i);
            velocities[i] += pressureForce / densities[i] * deltaTime;
        });

        // Updating positions
        Parallel.For(0, particleCount, i =>
        {
            positions[i] += velocities[i] * deltaTime;
            ResolveCollision(i);
            previousLocations[i] = positions[i];
        });
    }

    void ResolveCollision(int i)
    {
        if (positions[i].x < -simulationBounds.x || positions[i].x > simulationBounds.x)
        {
            velocities[i].x *= -bounceDamping; // Bounce back with damping
        }

        if (positions[i].y < -simulationBounds.y || positions[i].y > simulationBounds.y)
        {
            velocities[i].y *= -bounceDamping; // Bounce back with damping
        }
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
        previousLocations = positions;
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

    public float CalculateDensity(Vector2 position)
    {
        float density = 0f;
        foreach (Vector2 particlePos in positions)
        {
            float distance = Vector2.Distance(position, particlePos);
            density += SmoothingKernel(smoothingRadius, distance);
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

        Gizmos.color = particleColor;
        for (int i = 0; i < particleCount; i++)
        {
            Gizmos.DrawSphere(positions[i], 0.05f);
        }
    }
}
