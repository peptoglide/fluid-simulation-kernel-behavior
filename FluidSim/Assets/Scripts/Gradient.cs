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
    private ParticleFluid simulator;

    void Start()
    {
        simulator = ParticleFluid.Instance;
        CalculatePositions();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public Vector2 GradientAt(Vector2 position)
    {
        Vector2 gradient = Vector2.zero;
        for (int i = 0; i < simulator.particleCount; i++)
        {
            Vector2 particlePos = simulator.positions[i];
            Vector2 direction = (particlePos - position).normalized;

            float distance = Vector2.Distance(position, particlePos);
            float kernelDerivative = simulator.SmoothingKernelDerivative(simulator.smoothingRadius, distance);

            float density = simulator.densities[i];

            gradient -= simulator.mass * simulator.fieldQuantities[i] * kernelDerivative * direction / density;
        }
        return gradient;
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
