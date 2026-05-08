using UnityEngine;

public class SwitchState : MonoBehaviour
{
    public GameObject firstObject;
    public GameObject secondObject;
    public AudioSource audioSource;
    public AudioClip switchClip;
    public void Switch()
    {
        firstObject.SetActive(!firstObject.activeSelf);
        secondObject.SetActive(!secondObject.activeSelf);
        if (switchClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchClip);
        }
    }
}
