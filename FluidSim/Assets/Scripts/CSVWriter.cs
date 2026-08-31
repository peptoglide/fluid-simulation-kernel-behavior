using System;
using System.IO;
using System.Text;
using UnityEngine;

public class CSVWriter : MonoBehaviour
{
    public string csvName;
    private string _filePath;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(Application.dataPath);
        _filePath = Path.Combine(Application.dataPath, csvName + ".csv");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Write template CSV file")]
    void WriteTemplate()
    {
        _filePath = Path.Combine(Application.dataPath, csvName + ".csv");
        string[] columns =
        {
            "kernel",
            "msPerStep",
            "settleSteps",
            "settleDensitySTD",
            "settleMaxDensityError",
            "settleMeanDensityError",
            "maxVelocity"
        };

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns));
        
        try
        {
            File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"File successfully written to {_filePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to write CSV file: {e.Message}");
        }
    }

    /// <summary>
    /// Update CSV file with metrics
    /// </summary>
    /// <param name="metrics">Metrics to be recorded. Include kernel name as first element</param>
    public void UpdateKernel(string[] metrics)
    {
        if (!File.Exists(_filePath))
        {
            WriteTemplate();
            Debug.Log("CSV file doesn't exist, wrote template csv");
        }

        string[] lines = File.ReadAllLines(_filePath);
        string kernelName = metrics[0];
        bool updated = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string currentKernel = lines[i].Split(',')[0];
            if (currentKernel == kernelName)
            {
                // Change this line
                lines[i] = string.Join(',', metrics);
                updated = true;
                break;
            }
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++) sb.AppendLine(lines[i]);
        
        // Add new entry if doesn't exist
        if (!updated) sb.AppendLine(string.Join(',', metrics));

        try
        {
            File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"File successfully updated {_filePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to write to CSV file: {e.Message}");
        }
    }
}
