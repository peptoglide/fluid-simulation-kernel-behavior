using UnityEngine;

public class Pressure : MonoBehaviour
{
    public float targetDensity = 1f;
    public float pressureMult = 1f;

    private ParticleFluid simulator;

    void Start()
    {
        simulator = ParticleFluid.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float DensityToPressure(float density)
    {
        return pressureMult * (density - targetDensity);
    }

    public Vector2 PressureGradientAtParticle(int particleId)
    {
        Vector2 pressureGradient = Vector2.zero;
        for (int i = 0; i < simulator.particleCount; i++)
        {
            Vector2 particlePos = simulator.positions[i];
            Vector2 direction = (particlePos - simulator.positions[particleId]).normalized;

            float distance = Vector2.Distance(simulator.positions[particleId], particlePos);
            float kernelDerivative = simulator.SmoothingKernelDerivative(simulator.smoothingRadius, distance);

            float p_i = simulator.densities[particleId];
            float p_j = simulator.densities[i];

            pressureGradient += simulator.mass * (DensityToPressure(p_i) + DensityToPressure(p_j)) / 2f * kernelDerivative * direction / p_i;
        }
        return pressureGradient;
    }
}
