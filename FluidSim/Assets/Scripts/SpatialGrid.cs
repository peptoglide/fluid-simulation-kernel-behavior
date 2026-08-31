using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{
    public int[] startLocations { get; private set; }
    public int[] gridParticleCount { get; private set; }
    private int[] currentCell;
    private int[] tempSortedArray;
    private ParticleFluid simulator;
    private float gridSize;

    public int width { get; private set; }
    public int height { get; private set; }
    private Vector2 gridBound;
    void Start()
    {
        simulator = ParticleFluid.Instance;
        gridSize = simulator.kernel.GetName() == "CubicSpline" ? 
        simulator.smoothingRadius * 2f :
        simulator.smoothingRadius;

        width = Mathf.CeilToInt(simulator.simulationBounds.x * 2f / gridSize);
        height = Mathf.CeilToInt(simulator.simulationBounds.y * 2f / gridSize);

        gridBound = new Vector2(width * gridSize, height * gridSize);
        startLocations = new int[width * height];
        gridParticleCount = new int[width * height];
        currentCell = new int[simulator.particleCount];
        
        tempSortedArray = new int[simulator.particleCount];
        Array.Copy(simulator.sortedParticles, tempSortedArray, simulator.particleCount);     
    }

    public Vector2Int GetGridCell(Vector2 position)
    {
        Vector2 normalizedPos = (position + gridBound / 2f) / gridSize;
        int x = Mathf.Clamp(Mathf.FloorToInt(normalizedPos.x), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(normalizedPos.y), 0, height - 1);
        return new Vector2Int(x, y);
    }

    public int GetGridId(Vector2 position)
    {
        Vector2Int gridCell = GetGridCell(position);
        return gridCell.y * width + gridCell.x;
    }  

    public void UpdateSpatialGrid(Vector2[] positions)
    {
        Parallel.For(0, simulator.particleCount, i =>
        {
            int gridId = GetGridId(positions[i]);
            currentCell[i] = gridId;
            Interlocked.Increment(ref gridParticleCount[gridId]);
        });

        int tail = 0;
        for (int i = 0; i < width * height; i++)
        {
            startLocations[i] = tail;
            tail += gridParticleCount[i];
            gridParticleCount[i] = 0;
        }

        // Sort particles based on their grid cell
        Array.Sort(simulator.sortedParticles, (a, b) => currentCell[a].CompareTo(currentCell[b]));
        // SortArrayByCell(positions);

        Debug.Assert(tail == simulator.particleCount, "Tail does not match particle count after sorting.");
    }

    void SortArrayByCell(Vector2[] positions)
    {
        // Use grid particle count temporarily
        Array.Copy(
            startLocations,
            gridParticleCount,
            width * height
        );

        for (int i = 0; i < simulator.particleCount; i++)
        {
            int particleId = simulator.sortedParticles[i];
            int cell = currentCell[particleId];

            int destination = gridParticleCount[cell]++;
            tempSortedArray[destination] = particleId;
        }

        (tempSortedArray, simulator.sortedParticles) = (simulator.sortedParticles, tempSortedArray);
        Array.Clear(
            gridParticleCount,
            0,
            width * height
        );
    }

    public void ForeachNeighborParticle(Vector2 position, Action<int> actionAgainstParticleId)
    {
        Vector2Int gridCell = GetGridCell(position);
        for (int x = gridCell.x - 1; x <= gridCell.x + 1; x++)
        {
            for (int y = gridCell.y - 1; y <= gridCell.y + 1; y++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                int gridId = y * width + x;
                int startIdx = startLocations[gridId];
                int endIdx = (gridId + 1 == width * height) ? simulator.particleCount : startLocations[gridId + 1];

                for (int i = startIdx; i < endIdx; i++)
                {
                    actionAgainstParticleId(simulator.sortedParticles[i]);
                }
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
