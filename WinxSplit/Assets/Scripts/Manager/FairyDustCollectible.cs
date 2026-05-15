using UnityEngine;

public class FairyDustCollectible : MonoBehaviour
{
    public int dustAmount = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CurrencyManager.Instance.AddDust(dustAmount);

            Destroy(gameObject);
        }
    }
}