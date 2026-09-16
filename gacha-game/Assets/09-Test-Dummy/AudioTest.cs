using UnityEngine;
using UnityEngine.UI;

public class AudioTest : MonoBehaviour
{
    public AudioClip testBGM;
    public Button testButton;

    void Start()
    {
        testButton.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        AudioManager.Instance.PlayBGM(testBGM);
        //AudioManager.Instance.PlaySFX(testSFX);
        //AudioManager.Instance.PlayVoice(testVoice);
    }
}
