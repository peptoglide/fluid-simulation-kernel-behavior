using UnityEngine;

// Spiky but ^2
public class DefaultKernel : Kernel
{
    private float _smoothingRadius;
    private float _radiusSqr;
    private float _functionVolume;
    public DefaultKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _radiusSqr = smoothingRadius * smoothingRadius;
        _functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 4) / 6f;
    }

    public string GetName() => "SpikyBut^2";
    
    public float SmoothingKernel(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return (_smoothingRadius - distance) * (_smoothingRadius - distance) / _functionVolume; // (r-d)^2 for steeper derivatives near 0
    }
    public float KernelGradient(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return -2f * (_smoothingRadius - distance) / _functionVolume; // Derivative
    }
    public float KernelLaplacian(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return 2f / _functionVolume + KernelGradient(sqrDistance) / distance; // Second derivative
    }
}
