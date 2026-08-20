using UnityEngine;

public class DefaultKernel : Kernel
{
    private float _smoothingRadius;
    private float _functionVolume;
    public DefaultKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _functionVolume = Mathf.PI * Mathf.Pow(smoothingRadius, 4) / 6f;
    }
    
    public float SmoothingKernel(float sqrDistance)
    {
        if (sqrDistance >= _smoothingRadius * _smoothingRadius)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return (_smoothingRadius - distance) * (_smoothingRadius - distance) / _functionVolume; // (r-d)^2 for steeper derivatives near 0
    }
    public float SmoothingKernelDerivative(float sqrDistance)
    {
        if (sqrDistance >= _smoothingRadius * _smoothingRadius)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return -2f * (_smoothingRadius - distance) / _functionVolume; // Derivative
    }
    public float SmoothingKernelSecondDerivative(float sqrDistance)
    {
        if (sqrDistance >= _smoothingRadius * _smoothingRadius)
            return 0f;

        return 2f / _functionVolume; // Second derivative
    }
}
