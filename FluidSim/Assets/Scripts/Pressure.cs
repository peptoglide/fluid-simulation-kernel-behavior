using UnityEngine;

public class Pressure : MonoBehaviour
{
    public float targetDensity = 1f;
    public float pressureMult = 1f;
    public float nearPressureMult = 1f;
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

    public (float, float) DensityToPressure(float density, float nearDensity)
    {
        float densityDifference = density - targetDensity;

        float pressure = pressureMult * densityDifference;
        float nearPressure = nearDensity * nearPressureMult;
        return (pressure, nearPressure);
    }

    
    // Lowkey forgot to spatial grid this function
    public Vector2 PressureGradientAtParticle(Vector2[] positions, int particleId)
    {
        Vector2 pressureGradient = Vector2.zero;
        Vector2 nearPressureGradient = Vector2.zero;

        // Only look at particles within a 3x3 square
        Vector2 position = positions[particleId];
        grid.ForeachNeighborParticle(position, i =>
        {
            Vector2 particlePos = positions[i];
            Vector2 direction = particlePos - position;
            float sqrDistance = direction.sqrMagnitude;

            if (sqrDistance == 0f)
                return; // Skip self
            
            Vector2 directionNormalized = direction.normalized; 

            float kernelDerivative = simulator.SmoothingKernelDerivative(sqrDistance);
            float nearDerivative = simulator.NearSmoothingKernelDerivative(simulator.smoothingRadius, sqrDistance);

            (float thisPressure, float thisNearPressure) = DensityToPressure(simulator.densities[particleId],
            simulator.nearDensities[particleId]);

            (float otherPressure, float otherNearPressure) = DensityToPressure(simulator.densities[i],
            simulator.nearDensities[i]);

            float sharedPressure = (thisPressure + otherPressure) / 2f;
            float sharedNearPressure = (thisNearPressure + otherNearPressure) / 2f;

            pressureGradient += simulator.mass * sharedPressure * kernelDerivative * directionNormalized / simulator.densities[i];
            // Near density
            pressureGradient += simulator.mass * sharedNearPressure * nearDerivative * directionNormalized / simulator.densities[i];
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
            float sqrDistance = (particlePos - position).sqrMagnitude;

            if (sqrDistance == 0f)
                return; // Skip self
            
            float kernelSecondDerivative = simulator.SmoothingKernelSecondDerivative(sqrDistance);

            Vector2 v_i = simulator.velocities[particleId];
            Vector2 v_j = simulator.velocities[i];
            viscosityForce += simulator.mass * (v_j - v_i) * kernelSecondDerivative / simulator.densities[i];
        });
        return viscosityForce * viscosity;
    }
}
