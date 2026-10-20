using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    public Button resultCloseBT;

    public void resoultCloseBTClick()
    {
        if (resultCloseBT != null)
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
