using UnityEngine;

public interface Kernel
{
    public abstract string GetName();
    public abstract float SmoothingKernel(float sqrDistance);
    public abstract float KernelGradient(float sqrDistance);
    public abstract float KernelLaplacian(float sqrDistance);
}
