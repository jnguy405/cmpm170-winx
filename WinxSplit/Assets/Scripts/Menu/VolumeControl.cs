using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer MainMixer;
    
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider windSlider;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private Slider birdsSlider;
    [SerializeField] private Slider cicadasSlider;
    [SerializeField] private Slider fireSlider;
    
    private void Start()
    {
        LoadVolumeSettings();
    }
    
    public void SetMasterVolume(float value)
    {
        float dB = ConvertToDecibels(value);
        MainMixer.SetFloat("MyExposedParam 1", dB);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }
    
    public void SetWindVolume(float value)
    {
        float dB = ConvertToDecibels(value);
        MainMixer.SetFloat("Wind", dB);
        PlayerPrefs.SetFloat("WindVolume", value);
    }
    
    public void SetWaterVolume(float value)
    {
        float dB = ConvertToDecibels(value);
        MainMixer.SetFloat("Water", dB);
        PlayerPrefs.SetFloat("WaterVolume", value);
    }
    
    public void SetBirdsVolume(float value)
    {
        float dB = ConvertToDecibels(value);
        MainMixer.SetFloat("Birds", dB);
        PlayerPrefs.SetFloat("BirdsVolume", value);
    }
    
    public void SetCicadasVolume(float value)
    {
        float dB = ConvertToDecibels(value);
        MainMixer.SetFloat("Cicadas", dB);
        PlayerPrefs.SetFloat("CicadasVolume", value);
    }
    
    public void SetFireVolume(float value)
    {
        float dB = ConvertToDecibels(value);
        MainMixer.SetFloat("Fire", dB);
        PlayerPrefs.SetFloat("FireVolume", value);
    }
    
    private float ConvertToDecibels(float value)
    {
        if (value <= 0.001f)
            return -80f;
        return Mathf.Log10(value) * 20f;
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
    
    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}
