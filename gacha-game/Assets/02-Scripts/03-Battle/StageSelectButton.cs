using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StageSelectButton : MonoBehaviour
{
    [Header("이 버튼이 열어줄 스테이지 SO")]
    public StageDataSO stageData;

    [Header("스테이지 이름 텍스트")]
    public TextMeshProUGUI stageNameText;

    private void Start()
    {
        UpdateUI();

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnStageClicked);
        }
    }

    public void UpdateUI()
    {
        if (stageData == null) return;

        // 버튼에 스테이지 이름만 표기
        if (stageNameText != null)
            stageNameText.text = stageData.stageName;
    }

    private void OnStageClicked()
    {
        if (stageData == null) return;

        // 선택한 스테이지 SO 전달
        TurnManager.SelectedStageData = stageData;

        // 전투 씬(StageScene)으로 이동
        SceneManager.LoadScene("BattleScene");
    }
}