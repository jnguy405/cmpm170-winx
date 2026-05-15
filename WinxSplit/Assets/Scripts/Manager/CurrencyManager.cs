using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Currency")]
    public int fairyDust = 0;

    [Header("UI")]
    public TextMeshProUGUI currencyText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddDust(int amount)
    {
        fairyDust += amount;

        UpdateUI();
    }

    public bool SpendDust(int amount)
    {
        if (fairyDust >= amount)
        {
            fairyDust -= amount;

            UpdateUI();

            return true;
        }

        return false;
    }

    void UpdateUI()
    {
        if (currencyText != null)
        {
            currencyText.text =
                "Fairy Dust: " + fairyDust;
        }
    }
}