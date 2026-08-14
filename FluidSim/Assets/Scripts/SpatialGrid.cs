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
    private ParticleFluid simulator;
    private float gridSize;

    public int width { get; private set; }
    public int height { get; private set; }
    private Vector2 gridBound;
    void Start()
    {
        simulator = ParticleFluid.Instance;
        gridSize = simulator.smoothingRadius;

        width = Mathf.CeilToInt(simulator.simulationBounds.x * 2f / gridSize);
        height = Mathf.CeilToInt(simulator.simulationBounds.y * 2f / gridSize);

        gridBound = new Vector2(width * gridSize, height * gridSize);
        startLocations = new int[width * height];
        gridParticleCount = new int[width * height];
        currentCell = new int[simulator.particleCount];
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

        // Sort particles based on their grid cell
        Array.Sort(simulator.sortedParticles, (a, b) => currentCell[a].CompareTo(currentCell[b]));

        int tail = 0;
        for (int i = 0; i < width * height; i++)
        {
            startLocations[i] = tail;
            tail += gridParticleCount[i];
            gridParticleCount[i] = 0; // Reset for next update
        }

        Debug.Assert(tail == simulator.particleCount, "Tail does not match particle count after sorting.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
