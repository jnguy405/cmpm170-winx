using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("UI Panel")]
    [SerializeField] private GameObject volumePanel;
    
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider windSlider;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private Slider birdsSlider;
    [SerializeField] private Slider cicadasSlider;
    [SerializeField] private Slider fireSlider;
    
    private void Start()
    {
        if (volumePanel != null)
            volumePanel.SetActive(false);
        
        LoadVolumeSettings();
        
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (windSlider != null)
            windSlider.onValueChanged.AddListener(SetWindVolume);
        if (waterSlider != null)
            waterSlider.onValueChanged.AddListener(SetWaterVolume);
        if (birdsSlider != null)
            birdsSlider.onValueChanged.AddListener(SetBirdsVolume);
        if (cicadasSlider != null)
            cicadasSlider.onValueChanged.AddListener(SetCicadasVolume);
        if (fireSlider != null)
            fireSlider.onValueChanged.AddListener(SetFireVolume);
    }
    
    public void OpenVolumePanel()
    {
        if (volumePanel != null)
            volumePanel.SetActive(true);
    }
    
    public void CloseVolumePanel()
    {
        if (volumePanel != null)
            volumePanel.SetActive(false);
        SaveVolumeSettings();
    }
    
    public void SetMasterVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat("MasterVolume", dB);
    }
    
    public void SetWindVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat("WindVolume", dB);
    }
    
    public void SetWaterVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat("WaterVolume", dB);
    }
    
    public void SetBirdsVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat("BirdsVolume", dB);
    }
    
    public void SetCicadasVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat("CicadasVolume", dB);
    }
    
    public void SetFireVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        audioMixer.SetFloat("FireVolume", dB);
    }
    
    private void LoadVolumeSettings()
    {
        if (masterSlider != null)
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        if (windSlider != null)
            windSlider.value = PlayerPrefs.GetFloat("WindVolume", 0.7f);
        if (waterSlider != null)
            waterSlider.value = PlayerPrefs.GetFloat("WaterVolume", 0.8f);
        if (birdsSlider != null)
            birdsSlider.value = PlayerPrefs.GetFloat("BirdsVolume", 0.6f);
        if (cicadasSlider != null)
            cicadasSlider.value = PlayerPrefs.GetFloat("CicadasVolume", 0.5f);
        if (fireSlider != null)
            fireSlider.value = PlayerPrefs.GetFloat("FireVolume", 0.65f);
    }
    
    private void SaveVolumeSettings()
    {
        if (masterSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
        if (windSlider != null)
            PlayerPrefs.SetFloat("WindVolume", windSlider.value);
        if (waterSlider != null)
            PlayerPrefs.SetFloat("WaterVolume", waterSlider.value);
        if (birdsSlider != null)
            PlayerPrefs.SetFloat("BirdsVolume", birdsSlider.value);
        if (cicadasSlider != null)
            PlayerPrefs.SetFloat("CicadasVolume", cicadasSlider.value);
        if (fireSlider != null)
            PlayerPrefs.SetFloat("FireVolume", fireSlider.value);
        
        PlayerPrefs.Save();
    }
}
