using UnityEngine;

public class Density : MonoBehaviour
{

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float density = CalculateDensity(mousePos);
            Debug.Log($"Density at {mousePos}: {density}");
        }
    }

    

    // Calculate density by substituting density into field quantity equation
    public float CalculateDensity(Vector2 position)
    {
        float density = 0f;
        foreach (Vector2 particlePos in ParticleFluid.Instance.positions)
        {
            float distance = Vector2.Distance(position, particlePos);
            density += ParticleFluid.Instance.SmoothingKernel(ParticleFluid.Instance.smoothingRadius, distance);
        }
        return density;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, ParticleFluid.Instance.smoothingRadius);
    }
}
