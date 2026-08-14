using UnityEngine;

public class Pressure : MonoBehaviour
{
    public float targetDensity = 1f;
    public float pressureMult = 1f;
    public float viscosity = 0.25f;

    private ParticleFluid simulator;
    private SpatialGrid grid;

    void Start()
    {
        simulator = ParticleFluid.Instance;
        grid = GetComponent<SpatialGrid>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float DensityToPressure(float density)
    {
        return pressureMult * (density - targetDensity);
    }

    
    // Lowkey forgot to spatial grid this function
    public Vector2 PressureGradientAtParticle(Vector2[] positions, int particleId)
    {
        Vector2 pressureGradient = Vector2.zero;

        // Only look at particles within a 3x3 square
        Vector2 position = positions[particleId];
        grid.ForeachNeighborParticle(position, i =>
        {
            Vector2 particlePos = positions[i];
            float distance = Vector2.Distance(position, particlePos);

            if (distance == 0f)
                return; // Skip self
            
            Vector2 direction = (particlePos - position) / distance; // Normalize direction but do this to save 
            float kernelDerivative = simulator.SmoothingKernelDerivative(simulator.smoothingRadius, distance);

            float p_i = simulator.densities[particleId];
            float p_j = simulator.densities[i];
            pressureGradient += simulator.mass * (DensityToPressure(p_i) + DensityToPressure(p_j)) / 2f * kernelDerivative * direction / p_i;
        });
        return pressureGradient;
    }
    

    public Vector2 ViscosityForceAtParticle(Vector2[] positions, int particleId)
    {
        Vector2 viscosityForce = Vector2.zero;

        // Only look at particles within a 3x3 square
        Vector2 position = positions[particleId];
        grid.ForeachNeighborParticle(position, i =>
        {
            Vector2 particlePos = positions[i];
            float distance = Vector2.Distance(position, particlePos);

            if (distance == 0f)
                return; // Skip self
            
            Vector2 direction = (particlePos - position) / distance; // Normalize direction but do this to save 
            float kernelSecondDerivative = simulator.SmoothingKernelSecondDerivative(simulator.smoothingRadius, distance);

            Vector2 v_i = simulator.velocities[particleId];
            Vector2 v_j = simulator.velocities[i];
            viscosityForce += simulator.mass * (v_j - v_i) * kernelSecondDerivative / simulator.densities[i];
        });
        return viscosityForce * viscosity;
    }
}
