using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Benchmarker : MonoBehaviour
{
    [Header("Performance")]
    public float recalculationDelay = 0.25f;
    public float sampleWindow = 5f;
    [Header("UI")]
    public TextMeshProUGUI frameText;

    private Queue frameTimes = new Queue();
    private float timeSum;
    private float cooldown = 0f;

    void Start()
    {
        cooldown = recalculationDelay;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;

        frameTimes.Enqueue(dt);
        timeSum += dt;
        cooldown -= dt;

        while (timeSum > sampleWindow)
        {
            timeSum -= (float)frameTimes.Dequeue();
        }

        if (cooldown <= 0f)
        {
            cooldown = recalculationDelay;
            float fps = timeSum == 0 ? 0f : frameTimes.Count / timeSum;
            frameText.SetText($"FPS last {sampleWindow}s: {fps}");
        }
    }

}
