using UnityEngine;

// CubicSpline
public class WendlandKernel : Kernel
{
    private float _smoothingRadius;
    private float _radiusSqr;
    private float _functionVolume;
    public WendlandKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _radiusSqr = smoothingRadius * smoothingRadius;
        _functionVolume = 4f * Mathf.PI * Mathf.Pow(smoothingRadius, 2) / 7f;
    }
    
    public float SmoothingKernel(float sqrDistance)
    {
        float q = Mathf.Sqrt(sqrDistance) / _smoothingRadius;

        if (q > 1) return 0;
        return Mathf.Pow(1f-q, 4f) * (1 + 4*q) / _functionVolume;
    }
    public float KernelGradient(float sqrDistance)
    {
        float q = Mathf.Sqrt(sqrDistance) / _smoothingRadius;

        if (q > 1) return 0;

        return -20f * q * (1-q)*(1-q)*(1-q) / _smoothingRadius / _functionVolume;
    }
    public float KernelLaplacian(float sqrDistance)
    {
        float distance = Mathf.Sqrt(sqrDistance);
        float q = distance / _smoothingRadius;

        if (q > 1) return 0;

        return -20f * (1-q)*(1-q) * (1-4*q) / _smoothingRadius / _smoothingRadius / _functionVolume + 
            KernelGradient(sqrDistance) / distance;
    }
}
