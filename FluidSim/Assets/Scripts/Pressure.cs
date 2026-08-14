using UnityEngine;

public class Pressure : MonoBehaviour
{
    public float targetDensity = 1f;
    public float pressureMult = 1f;

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
        Vector2Int gridCell = grid.GetGridCell(position);
        for (int x = gridCell.x - 1; x <= gridCell.x + 1; x++)
        {
            for (int y = gridCell.y - 1; y <= gridCell.y + 1; y++)
            {
                if (x < 0 || x >= grid.width || y < 0 || y >= grid.height)
                    continue;

                int gridId = y * grid.width + x;
                int startIdx = grid.startLocations[gridId];
                int endIdx = (gridId + 1 == grid.width * grid.height) ? simulator.particleCount : grid.startLocations[gridId + 1];

                for (int i = startIdx; i < endIdx; i++)
                {
                    Vector2 particlePos = positions[simulator.sortedParticles[i]];
                    float distance = Vector2.Distance(position, particlePos);

                    if (distance == 0f)
                        continue; // Skip self
                    
                    Vector2 direction = (particlePos - position) / distance; // Normalize direction but do this to save 
                    float kernelDerivative = simulator.SmoothingKernelDerivative(simulator.smoothingRadius, distance);

                    float p_i = simulator.densities[particleId];
                    float p_j = simulator.densities[simulator.sortedParticles[i]];

                    pressureGradient += simulator.mass * (DensityToPressure(p_i) + DensityToPressure(p_j)) / 2f * kernelDerivative * direction / p_i;
                }
            }
        }
        return pressureGradient;
    }
    

    public Vector2 AAPressureGradientAtParticle(Vector2[] positions, int particleId)
    {
        Vector2 pressureGradient = Vector2.zero;
        for (int i = 0; i < simulator.particleCount; i++)
        {
            Vector2 particlePos = positions[i];
            Vector2 direction = (particlePos - positions[particleId]).normalized;

            float distance = Vector2.Distance(positions[particleId], particlePos);
            float kernelDerivative = simulator.SmoothingKernelDerivative(simulator.smoothingRadius, distance);

            float p_i = simulator.densities[particleId];
            float p_j = simulator.densities[i];

            pressureGradient += simulator.mass * (DensityToPressure(p_i) + DensityToPressure(p_j)) / 2f * kernelDerivative * direction / p_i;
        }
        return pressureGradient;
    }
}
