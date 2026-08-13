using UnityEngine;
using System;
using System.Threading.Tasks;

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
    [Header("Rendering")]
    public Mesh circle;

    public Vector2[] positions { get; private set; }
    public float[] fieldQuantities { get; private set; }
    public float[] densities { get; private set; }
    public int particleCount { get; private set; }
    public float functionVolume { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
        CalculatePositions();

        densities = new float[particleCount];
        fieldQuantities = new float[particleCount];
        UpdateDensities();
        SetQuantities();

        functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 8) / 4f;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDensities();
    }

    void UpdateDensities()
    {
        Parallel.For(0, particleCount, i =>
        {
            densities[i] = CalculateDensity(positions[i]);
        });
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

    void SetQuantities()
    {
        // Use random function for testing
        for (int i = 0; i < particleCount; i++)
        {
            fieldQuantities[i] = Mathf.Sin(positions[i].x) * Mathf.Cos(positions[i].y);
        }
    }

    // Divide by volume to ensure normalized kernel i.e Integral all = 1
    public float SmoothingKernel(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        float sqrDistance = radius * radius - distance * distance;
        return sqrDistance * sqrDistance * sqrDistance / functionVolume; // (r^2 - d^2)^3 for a smooth top
    }

    // Derivative of kernel function
    public float SmoothingKernelDerivative(float radius, float distance)
    {
        if (distance >= radius)
            return 0f;

        float sqrDistance = radius * radius - distance * distance;
        return -6f * distance * sqrDistance * sqrDistance / functionVolume; // Derivative of (r^2 - d^2)^3 / volume
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
        if (sidelength >= 75) return; // Avoid drawing too many particles in the editor

        CalculatePositions();

        fieldQuantities = new float[particleCount];
        SetQuantities();
        for (int i = 0; i < particleCount; i++)
        {
            Gizmos.color = new Color(0.3f, 0.3f, 0.9f) * fieldQuantities[i];
            Gizmos.DrawSphere(positions[i], 0.05f);
        }
    }
}
