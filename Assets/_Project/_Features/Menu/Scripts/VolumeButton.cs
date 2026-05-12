using UnityEngine;
using UnityEngine.UI;

public class VolumeButton : MonoBehaviour
{
    public enum ButtonType
    {
        Mute,
        FullUnmute
    }
    public ButtonType type;

    [SerializeField] private Slider slider;

    public void OnClick()
    {
        if (type == ButtonType.Mute)
            MuteVolume();
        else
            FullUnmuteVolume();
    }

    private void MuteVolume()
    {
        slider.value = 0;
        AudioListener.volume = 0;
    }

    private void FullUnmuteVolume()
    {
        slider.value = 1;
        AudioListener.volume = 1;
    }
}
