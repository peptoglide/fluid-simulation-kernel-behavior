using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

// Poly6
public class FirstKernel : Kernel
{
    private float _smoothingRadius;
    private float _radiusSqr;
    private float _functionVolume;
    public FirstKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _radiusSqr = smoothingRadius * smoothingRadius;
        _functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 8) / 4f;
    }

    public string GetName() => "Poly6";
    
    public float SmoothingKernel(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float sqrDifference = _smoothingRadius - sqrDistance;
        return sqrDifference * sqrDifference * sqrDifference / _functionVolume;
    }
    public float KernelGradient(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float sqrDifference = _smoothingRadius - sqrDistance;
        float distance = Mathf.Sqrt(sqrDistance);
        return -6f * sqrDifference * sqrDifference * distance / _functionVolume; // Derivative
    }
    public float KernelLaplacian(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        float sqrDifference = _smoothingRadius - sqrDistance;

        return (-6f * sqrDifference * sqrDifference + 24f * sqrDifference * sqrDistance) / _functionVolume
        + KernelGradient(sqrDistance) / distance; // Laplacian
    }
}
