using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

// Poly6
public class MixedKernel : Kernel
{
    private float _smoothingRadius;
    private float _radiusSqr;
    private float _functionVolumePoly6;
    private float _functionVolumeSpiky;
    public MixedKernel(float smoothingRadius)
    {
        _smoothingRadius = smoothingRadius;
        _radiusSqr = smoothingRadius * smoothingRadius;

        _functionVolumePoly6 = Mathf.PI * Mathf.Pow(smoothingRadius, 8) / 4f;
        _functionVolumeSpiky = Mathf.PI * Mathf.Pow(smoothingRadius, 5) / 10f;
    }

    public string GetName() => "Mixed";
    
    public float SmoothingKernel(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float sqrDifference = _radiusSqr - sqrDistance;
        return sqrDifference * sqrDifference * sqrDifference / _functionVolumePoly6;
    }
    public float KernelGradient(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return -3f * (_smoothingRadius - distance) * (_smoothingRadius - distance) / _functionVolumeSpiky; // Derivative
    }
    public float KernelLaplacian(float sqrDistance)
    {
        if (sqrDistance >= _radiusSqr)
            return 0f;

        float distance = Mathf.Sqrt(sqrDistance);
        return 3f * (_smoothingRadius - distance) / _functionVolumeSpiky; // Second derivative
    }
}
