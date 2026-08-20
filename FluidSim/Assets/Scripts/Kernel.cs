using UnityEngine;

public interface Kernel
{
    public abstract float SmoothingKernel(float sqrDistance);
    public abstract float SmoothingKernelDerivative(float sqrDistance);
    public abstract float SmoothingKernelSecondDerivative(float sqrDistance);
}
