using UnityEngine;

// Spiky
public class DesbrunCustomLaplacianKernel : Kernel
{
    private float _smoothingRadius;
    private float _radiusSqr;
    private float _functionVolume;
    public DesbrunCustomLaplacianKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _radiusSqr = smoothingRadius * smoothingRadius;
        _functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 5) / 10f;
    }

    public string GetName() => "SpikyNonNegativeViscosity";
    
    public float SmoothingKernel(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return (_smoothingRadius - distance) * (_smoothingRadius - distance) * (_smoothingRadius - distance) / _functionVolume; // (r-d)^2 for steeper derivatives near 0
    }
    public float KernelGradient(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return -3f * (_smoothingRadius - distance) * (_smoothingRadius - distance) / _functionVolume; // Derivative
    }
    public float KernelLaplacian(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return 3f * (_smoothingRadius - distance) / _functionVolume; // Second derivative
    }
}
