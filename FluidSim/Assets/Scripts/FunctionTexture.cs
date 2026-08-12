using UnityEngine;
using UnityEngine.UI;

public class FunctionTexture : MonoBehaviour
{
    public ComputeShader computeShader;
    public RawImage display;

    public int width = 512;
    public int height = 512;

    public float scale = 10f;

    private RenderTexture texture;

    private int kernel;

    void Start()
    {
        kernel = computeShader.FindKernel("CSMain");

        texture = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGBFloat
        );

        texture.enableRandomWrite = true;
        texture.Create();

        display.texture = texture;

        Generate();
    }

    void Generate()
    {
        computeShader.SetInt("Width", width);
        computeShader.SetInt("Height", height);
        computeShader.SetFloat("Scale", scale);

        computeShader.SetTexture(
            kernel,
            "Result",
            texture
        );

        int groupsX = Mathf.CeilToInt(width / 8f);
        int groupsY = Mathf.CeilToInt(height / 8f);

        computeShader.Dispatch(
            kernel,
            groupsX,
            groupsY,
            1
        );
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
        }
    }
}