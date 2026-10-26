using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public void ClickBackButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
