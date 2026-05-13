using UnityEngine;

public class IndividualButterfly : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float frequency = 5f;
    public float magnitude = 1.5f;

    [Header("Wing Flap Settings")]
    [Range(0.1f, 5.0f)]
    public float wingPower = 1.0f; // High value = Stronger flap
    [Range(0.0f, 1.0f)]
    public float wingMaxDown = 0.8f;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        ApplyShaderOffsets();
    }

    void ApplyShaderOffsets()
    {
        meshRenderer.GetPropertyBlock(propBlock);
        
        // This is what gives you the "Stronger" flap
        propBlock.SetFloat("_WingPower", wingPower);
        propBlock.SetFloat("_Wingmaxdown", wingMaxDown);
        
        // Randomize phase so they don't flap in sync
        propBlock.SetFloat("_Offset", Random.Range(0f, 10f));
        
        meshRenderer.SetPropertyBlock(propBlock);
    }

    void Update()
    {
        // Simple Perlin "Wander" logic
        //float nx = (Mathf.PerlinNoise(Time.time * frequency, seed.x) * 2 - 1) * magnitude;
        //float ny = (Mathf.PerlinNoise(Time.time * frequency, seed.y) * 2 - 1) * magnitude;
        //float nz = (Mathf.PerlinNoise(Time.time * frequency, seed.z) * 2 - 1) * magnitude;

        //Vector3 drift = new Vector3(nx, ny, nz);
        //transform.position += (transform.forward + drift) * moveSpeed * Time.deltaTime;

        // Smoothly rotate to face the move direction
        //if (drift != Vector3.zero)
        //{
            //Quaternion lookRot = Quaternion.LookRotation(transform.forward + drift);
            //transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 3f);
        //}
    }
}