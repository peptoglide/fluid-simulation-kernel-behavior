using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

class CooldownActions
{
    private float _cooldown;
    private float _currentCooldown;
    private Action _action;
    public CooldownActions(float cooldown, Action action)
    {
        _cooldown = cooldown;
        _currentCooldown = _cooldown;
        _action = action;
    }

    public void Tick(float deltaTime)
    {
        _currentCooldown -= deltaTime;

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
    public float recalculationDelay = 0.25f;
    public float sampleWindow = 5f;
    [Header("Fluid Behavior")]
    public float velocityRecalc = 0.5f;
    [Header("UI")]
    public TextMeshProUGUI frameText;
    public TextMeshProUGUI velocityText;
    
    private ParticleFluid fluid;
    private Queue frameTimes = new Queue();
    private List<CooldownActions> cooldownActions;
    private float timeSum;

    void Start()
    {
        cooldownActions = new()
        {
            new(recalculationDelay, CalculateFPS),
            new(velocityRecalc, CalculateTotalVelocity)
        };
        fluid = GetComponent<ParticleFluid>();
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;

        frameTimes.Enqueue(dt);
        timeSum += dt;

        while (timeSum > sampleWindow)
        {
            timeSum -= (float)frameTimes.Dequeue();
        }

        // Ticking timers
        for (int i = 0; i < cooldownActions.Count; i++)
        {
            cooldownActions[i].Tick(dt);
        }
    }

    void CalculateFPS()
    {
        float fps = timeSum == 0 ? 0f : frameTimes.Count / timeSum;
        frameText.SetText($"FPS last {sampleWindow}s: {fps}");
    }

    void CalculateTotalVelocity()
    {
        float totalVelocity = 0f;
        Parallel.For(0, fluid.particleCount, i =>
        {
            totalVelocity += fluid.velocities[i].magnitude;
        });
        velocityText.SetText($"Total & Avg velocity: {totalVelocity:F2} & {totalVelocity/fluid.particleCount:F2}");
    }
}
