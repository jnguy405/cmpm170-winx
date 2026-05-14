using UnityEngine;

public class DustPile : MonoBehaviour
{
    [Header("Collection")]
    [SerializeField] InventoryManager inventory;
    [SerializeField] int itemId;

    [Header("Interaction")]
    [SerializeField] GameObject interactText;

    DustSpawner spawner;
    bool playerNearby;
    bool collected;

    void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<DustSpawner>();
    }

    public void SetSpawner(DustSpawner owner)
    {
        spawner = owner;
    }

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    void Update()
    {
        if (!playerNearby || collected)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            Collect();
    }

    public void Collect()
    {
        if (collected)
            return;

        collected = true;
        playerNearby = false;

        if (interactText != null)
            interactText.SetActive(false);

        if (inventory != null)
            inventory.AddItem(itemId);

        if (spawner != null)
            spawner.HandleCollected(gameObject);
        else
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player"))
            return;

        playerNearby = true;
        if (interactText != null)
            interactText.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        if (interactText != null)
            interactText.SetActive(false);
    }
}
