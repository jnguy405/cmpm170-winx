using UnityEngine;

public class ButterflySpawner : MonoBehaviour
{
    public ButterflyManager manager;
    public int myID; // Set this in the inspector to match the index of the system you want to spawn from

    void OnMouseDown()
    {
        manager.SpawnSpecificButterfly(myID);
    }
}