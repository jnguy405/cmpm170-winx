using UnityEngine;

public class ButterflyManager : MonoBehaviour
{
    public ParticleSystem[] butterflySystems;

    public void SpawnRandomButterfly()
    {
        if (butterflySystems.Length == 0) return;

        int randomIndex = Random.Range(0, butterflySystems.Length);
        
        butterflySystems[randomIndex].Emit(1);
        Debug.Log("Randomly spawned from system: " + randomIndex);
    }

    public void SpawnSpecificButterfly(int index)
    {
        if (index >= 0 && index < butterflySystems.Length)
        {
            butterflySystems[index].Emit(1);
        }
    }
}