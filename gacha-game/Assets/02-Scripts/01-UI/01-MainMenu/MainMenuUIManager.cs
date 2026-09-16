using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{

    public Button stageButton;

    //일단은 임시로 스테이지 버튼 누르면 바로 씬 전환되도록 적용
    //나중에 로딩화면 같은거 구현할 예정
    public void ClickStageBT()
    {
        if (stageButton != null)
        {
            SceneManager.LoadScene("StageScene");
        }
    }
}
