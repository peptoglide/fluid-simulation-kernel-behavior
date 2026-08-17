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
    [Header("Interaction")]
    public float mouseForceStrength = 10f;
    public float mouseRadius = 2f;
    [Header("Determinism")]
    public float timestepSeconds = 1f / 60f;
    [Header("Rendering")]
    public Color particleColor = Color.white;

    public Vector2[] velocities { get; private set; }
    public Vector2[] accelerations { get; private set; }
    public Vector2[] positions { get; private set; }
    public Vector2[] predictedPositions { get; private set; }
    public float[] fieldQuantities { get; private set; }
    public float[] densities { get; private set; }
    public float[] nearDensities { get; private set; }
    // Store grid cell of particles
    public int[] sortedParticles;
    public int particleCount { get; private set; }
    public float functionVolume { get; private set; }
    private Pressure pressureCalculator;
    private SpatialGrid grid;
    private bool isRunning = false;

    // Ensuring a fixed time step
    private float accumulatedTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
        CalculatePositions();

        densities = new float[particleCount];
        nearDensities = new float[particleCount];

        velocities = new Vector2[particleCount];
        accelerations = new Vector2[particleCount];
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
        float mouseInfluence = (Input.GetMouseButton(0) ? 1 : 0) - (Input.GetMouseButton(1) ? 1 : 0);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        accumulatedTime += deltaTime;
        accumulatedTime = Mathf.Clamp(accumulatedTime, 0f, timestepSeconds * 6f);
        while (accumulatedTime >= timestepSeconds)
        {
            Simulate(timestepSeconds, mouseInfluence, mousePosition);
            accumulatedTime -= timestepSeconds;
        }
    }

    void Simulate(float deltaTime, float mouseInfluence, Vector2 mousePosition)
    {
        // Step ahead of time to stabilize simulation faster
        Parallel.For(0, particleCount, i =>
        {
            predictedPositions[i] = positions[i] + velocities[i] * predictStep;
            // Clamping positions
            predictedPositions[i].x = Mathf.Clamp(predictedPositions[i].x, -simulationBounds.x, simulationBounds.x);
            predictedPositions[i].y = Mathf.Clamp(predictedPositions[i].y, -simulationBounds.y, simulationBounds.y);
        });

        grid.UpdateSpatialGrid(predictedPositions);
        Parallel.For(0, particleCount, i =>
        {
            densities[i] = CalculateDensity(predictedPositions, i);
            if (pressureCalculator.nearPressureMult != 0f) nearDensities[i] = CalculateNearDensity(predictedPositions, i);
        });
        
        // Pressure
        Parallel.For(0, particleCount, i =>
        {
            Vector2 pressureForce = pressureCalculator.PressureGradientAtParticle(predictedPositions, i);
            Vector2 viscosityForce = pressureCalculator.ViscosityForceAtParticle(predictedPositions, i);

            accelerations[i] = pressureForce 
            + viscosityForce 
            + Vector2.down * gravity * densities[i];

            // Mouse
            if (mouseInfluence != 0f)
            {
                accelerations[i] += MouseForce(mousePosition, predictedPositions[i], mouseInfluence) * densities[i];
            }
        });

        // Updating positions
        Parallel.For(0, particleCount, i =>
        {
            velocities[i] += accelerations[i] / densities[i] * deltaTime;
            positions[i] += velocities[i] * deltaTime;
            ResolveCollision(i);
        });
    }

    Vector2 MouseForce(Vector2 mousePosition, Vector2 position, float mouseInfluence)
    {
        Vector2 difference = mousePosition - position;

        float distance = difference.magnitude;
        if (distance > mouseRadius || distance < 0.001f) return Vector2.zero;

        float influence = mouseForceStrength;
        return mouseInfluence * influence * difference.normalized;
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

    // Near density to prevent clustering
    public float NearSmoothingKernel(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        float thisFunctionVolume = Mathf.PI * Mathf.Pow(radius, 5) / 10f;
        return (radius - distance) * (radius - distance) * (radius - distance) / thisFunctionVolume; 
    }

    // Derivative of kernel function
    public float SmoothingKernelDerivative(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        return -2f * (radius - distance) / functionVolume; // Derivative
    }

     // Near density to prevent clustering
    public float NearSmoothingKernelDerivative(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        float thisFunctionVolume = -Mathf.PI * Mathf.Pow(radius, 4) / 2f;
        return -3f * (radius - distance) * (radius - distance) / thisFunctionVolume; 
    }

    // Second-order derivative of kernel function
    public float SmoothingKernelSecondDerivative(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        return 2f / functionVolume; // Second derivative
    }

    public float CalculateDensity(Vector2[] positions, int particleId)
    {
        float density = 0f;
        Vector2 position = positions[particleId];
        
        grid.ForeachNeighborParticle(positions[particleId], i =>
        {
            Vector2 particlePos = predictedPositions[i];
            float distance = Vector2.Distance(position, particlePos);
            density += SmoothingKernel(smoothingRadius, distance);
        });
        return density;
    }

    public float CalculateNearDensity(Vector2[] positions, int particleId)
    {
        float nearDensity = 0f;
        Vector2 position = positions[particleId];
        
        grid.ForeachNeighborParticle(positions[particleId], i =>
        {
            Vector2 particlePos = predictedPositions[i];
            float distance = Vector2.Distance(position, particlePos);
            nearDensity += NearSmoothingKernel(smoothingRadius, distance);
        });
        return nearDensity;
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
