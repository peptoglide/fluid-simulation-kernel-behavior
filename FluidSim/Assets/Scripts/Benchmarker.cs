using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;

class CooldownActions
{
    private int _cooldown;
    private int _currentCooldown;
    private Action _action;
    public CooldownActions(int cooldown, Action action)
    {
        _cooldown = cooldown;
        _currentCooldown = _cooldown;
        _action = action;
    }

    public void Tick(int tickCount)
    {
        _currentCooldown -= tickCount;

        if (_currentCooldown <= 0f)
        {
            _action?.Invoke();
            _currentCooldown = _cooldown;
        }
    }
}

public class Benchmarker : MonoBehaviour
{
    [Header("Performance")]
    public int recalculationDelay = 15;
    public float sampleWindow = 5f;
    public int stepRecalc = 15;
    public float stepSampleWindow = 2f;
    [Header("Fluid Behavior")]
    public int velocityRecalc = 6;
    public int stabilityChecks = 5;
    public float stabilityThreshold = 1000f;
    public int densityRecalc = 12;
    [Header("UI")]
    public TextMeshProUGUI frameText;
    public TextMeshProUGUI velocityText;
    public TextMeshProUGUI stabilityText;
    public TextMeshProUGUI densityErrorText;
    public TextMeshProUGUI timePerStepText;

    [Header("Stable UI")]
    public TextMeshProUGUI stableStepAverage;
    public TextMeshProUGUI stableDensitySTD;
    public TextMeshProUGUI stableMaxDensityError;
    public TextMeshProUGUI stableMeanDensityError;
    public TextMeshProUGUI stableMaxVelocity; 
    
    private ParticleFluid fluid;
    private CSVWriter writer;
    private Queue frameTimes = new Queue();
    private Queue stepTimes = new Queue();
    private List<CooldownActions> cooldownActions;
    private float timeSumFPS;
    private float timeSumSteps;
    private float timeElapsed;
    private int stableCount = 0;
    private float maxVelocity = 0f;
    private int simulationStepsTotal = 0;

    void Start()
    {
        cooldownActions = new()
        {
            new(recalculationDelay, CalculateFPS),
            new(velocityRecalc, CalculateTotalVelocity),
            new(densityRecalc, CalculateDensityError),
            new(stepRecalc, CalculateAvgSteptime)
        };
        fluid = GetComponent<ParticleFluid>();
        writer = GetComponent<CSVWriter>();

        fluid.onFinishSimulationStep += AddStepIntoQueue;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;

        frameTimes.Enqueue(dt);
        timeSumFPS += dt;
        timeElapsed += dt;

        while (timeSumFPS > sampleWindow)
        {
            timeSumFPS -= (float)frameTimes.Dequeue();
        }

        // Ticking timers
        for (int i = 0; i < cooldownActions.Count; i++)
        {
            cooldownActions[i].Tick(1);
        }
    }

    void CalculateFPS()
    {
        float fps = timeSumFPS == 0 ? 0f : frameTimes.Count / timeSumFPS;
        frameText.SetText($"FPS last {sampleWindow}s: {fps}");
    }

    void CalculateAvgSteptime()
    {
        float avgSteps = timeSumSteps == 0 ? 0f : timeSumSteps / stepTimes.Count;
        timePerStepText.SetText($"Avg step last {stepSampleWindow}s: {avgSteps}");
    }

    void CalculateTotalVelocity()
    {
        float totalVelocity = 0f;
        float rms = 0f;

        for (int i = 0; i < fluid.particleCount; i++)
        {
            float mag = fluid.velocities[i].magnitude;
            maxVelocity = Mathf.Max(maxVelocity, mag);
            totalVelocity += mag;
            rms += mag * mag; // sqrMagnitude
        }
        rms = Mathf.Sqrt(rms / fluid.particleCount);

        if (rms <= stabilityThreshold) stableCount++;
        else stableCount = Mathf.Min(stableCount, 0);

        if (stableCount >= stabilityChecks)
        {
            stabilityText.SetText($"Stable after {simulationStepsTotal} steps");
            CalculateStabilityMetrics();
            stableCount = -9999;
        }

        velocityText.SetText($"Total & RMS velocity: {totalVelocity:F2} & {rms:F2}");
    }

    void CalculateDensityError()
    {
        float totalDensityError = 0f;

        for (int i = 0; i < fluid.particleCount; i++)
        {
            totalDensityError += Mathf.Abs(fluid.densities[i] - fluid.pressureCalculator.targetDensity);
        }
        densityErrorText.SetText($"Avg density error: {totalDensityError / fluid.pressureCalculator.targetDensity / fluid.particleCount:F2}");
    }

    void AddStepIntoQueue(float stepTime)
    {
        timeSumSteps += stepTime;
        stepTimes.Enqueue(stepTime);

        while (timeSumSteps > stepSampleWindow)
        {
            timeSumSteps -= (float)stepTimes.Dequeue();
        }
        simulationStepsTotal++;
    }

    public float RelativeDensityStd(float[] densities)
    {
        float sum = 0f;
        for (int i = 0; i < densities.Length; i++) sum += densities[i];

        float mean = sum / densities.Length;

        float squaredDifferenceSum = 0f;
        for (int i = 0; i < densities.Length; i++)
        {
            squaredDifferenceSum += (densities[i] - mean) * (densities[i] - mean);
        }

        float std = Mathf.Sqrt(squaredDifferenceSum / densities.Length);
        return std / mean;
    }

    void CalculateStabilityMetrics()
    {
        float avgSteps = timeSumSteps == 0 ? 0f : timeSumSteps / stepTimes.Count;
        float densitySTD = RelativeDensityStd(fluid.densities);

        float maxDensityError = 0f;
        float totalDensityError = 0f;

        for (int i = 0; i < fluid.particleCount; i++)
        {
            maxDensityError = Mathf.Max(maxDensityError, Mathf.Abs(fluid.densities[i] - fluid.pressureCalculator.targetDensity));
            totalDensityError += Mathf.Abs(fluid.densities[i] - fluid.pressureCalculator.targetDensity);
        }
        float maxRelativeDensityError = maxDensityError / fluid.pressureCalculator.targetDensity;
        float meanRelativeDensityError = totalDensityError / fluid.pressureCalculator.targetDensity / fluid.particleCount;
        float maxVelocityOverSimulation = maxVelocity;

        stableStepAverage.SetText($"Settle avg step over {stepSampleWindow}s: {avgSteps:F5}");
        stableDensitySTD.SetText($"Density STD: {densitySTD * 100:F3}%");
        stableMaxDensityError.SetText($"Max density error: {maxRelativeDensityError:F3}");
        stableMeanDensityError.SetText($"Mean density error: {meanRelativeDensityError:F3}");
        stableMaxVelocity.SetText($"Max velocity over simulation: {maxVelocityOverSimulation:F3}");

        string[] metrics =
        {
            fluid.kernel.GetName(),
            avgSteps.ToString("F5"),
            simulationStepsTotal.ToString(),
            (densitySTD * 100).ToString("F5"),
            maxRelativeDensityError.ToString("F5"),
            meanRelativeDensityError.ToString("F5"),
            maxVelocityOverSimulation.ToString("F5")
        };
        writer.UpdateKernel(metrics);
    }
}
