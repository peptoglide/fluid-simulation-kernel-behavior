using UnityEngine;
using System;

public class Gradient : MonoBehaviour
{
    [Header("Initial Configuration")]
    public int sidelength; 
    public float particleSpacing;

    public Vector2[] positions { get; private set; }
    public Vector2[] gradients { get; private set; }
    private bool hasCalculated = false;
    private int particleCount;

    void Start()
    {
        CalculatePositions();
    }

    // Update is called once per frame
    void Update()
    {
        if(!hasCalculated)
        {
            Debug.Log($"Calculating gradients time at {DateTime.Now.ToString("HH:mm:ss")}");
            gradients = new Vector2[particleCount];
            CalculateGradient();
            Debug.Log($"Finished gradients at {DateTime.Now.ToString("HH:mm:ss")}");
            hasCalculated = true;
        }
    }

    void CalculateGradient()
    {
        for (int i = 0; i < particleCount; i++)
        {
            Vector2 pos = positions[i];
            
            float d = 0.001f;
            float deltaX = FieldQuantityAt(pos + d * Vector2.right) - FieldQuantityAt(pos);
            float deltaY = FieldQuantityAt(pos + d * Vector2.up) - FieldQuantityAt(pos);

            Vector2 gradient = new Vector2(deltaX, deltaY) / d;
            gradients[i] = gradient;
        }
    }

    float FieldQuantityAt(Vector2 position)
    {
        float quantity = 0f;
        for (int i = 0; i < ParticleFluid.Instance.particleCount; i++)
        {
            float distance = Vector2.Distance(position, ParticleFluid.Instance.positions[i]);
            quantity += ParticleFluid.Instance.SmoothingKernel(ParticleFluid.Instance.smoothingRadius, distance)
            * ParticleFluid.Instance.mass
            * ParticleFluid.Instance.fieldQuantities[i]
            / ParticleFluid.Instance.CalculateDensity(ParticleFluid.Instance.positions[i]);
        }
        return quantity;
    }

    void CalculatePositions()
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

    void OnDrawGizmos()
    {
        if (!hasCalculated) return;
        for (int i = 0; i < particleCount; i++)
        {
            Vector2 pos = positions[i];
            Vector2 grad = gradients[i] / 2f; // Scale down for visualization

            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, pos + grad);
            Gizmos.DrawCube(pos + grad, Vector3.one * 0.02f); // Arrow head for direction information
        }
    }
}
