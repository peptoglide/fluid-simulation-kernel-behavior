using UnityEngine;

// CubicSpline
public class CubicSplineKernel : Kernel
{
    private float _smoothingRadius;
    private float _radiusSqr;
    private float _functionVolume;
    public CubicSplineKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _radiusSqr = smoothingRadius * smoothingRadius;
        _functionVolume = 7f * Mathf.PI * Mathf.Pow(smoothingRadius, 2) / 10f;
    }

    public string GetName() => "CubicSpline";
    
    public float SmoothingKernel(float sqrDistance)
    {
        float q = Mathf.Sqrt(sqrDistance) / _smoothingRadius;

        if (q >= 2) return 0;
        if (q >= 1)
        {
            return (2f-q)*(2f-q)*(2f-q) / 4f / _functionVolume;
        }

        return (1 - 3f/2f*q*q + 3f/4f*q*q*q) / _functionVolume;
    }
    public float KernelGradient(float sqrDistance)
    {
        float q = Mathf.Sqrt(sqrDistance) / _smoothingRadius;

        if (q >= 2) return 0;
        if (q >= 1)
        {
            return -3f/4f * (2f-q)*(2f-q) / _smoothingRadius / _functionVolume;
        }

        return (-3f*q + 9f/4f*q*q) / _smoothingRadius / _functionVolume;
    }
    public float KernelLaplacian(float sqrDistance)
    {
        float distance = Mathf.Sqrt(sqrDistance);
        float q = distance / _smoothingRadius;

        if (q >= 2) return 0;
        if (q >= 1)
        {
            return 3f/2f * (2f-q) / _smoothingRadius / _smoothingRadius / _functionVolume + 
            KernelGradient(sqrDistance) / distance;
        }

        return (-3f + 9f/2f*q) / _smoothingRadius / _smoothingRadius / _functionVolume + 
            KernelGradient(sqrDistance) / distance;
    }
}
