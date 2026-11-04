
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestManager : MonoBehaviour
{
    [Header("UI")]
    public Button questButton;
    public Button resetQuestButton;
    public TMP_Text questCompleteText;
    public TMP_Text starPointText;

    [Header("Reward")]
    public int rewardStarPoint = 100;

    [Header("Settings")]
    public float completeMessageDuration = 3f;

    private bool isCompleted;
    private int grantedReward;
    private Coroutine messageCoroutine;

    private const string QuestKey = "QuestCompleted";
    private const string RewardKey = "QuestGrantedReward";

    void Start()
    {
        isCompleted = PlayerPrefs.GetInt(QuestKey, 0) == 1;

        grantedReward = PlayerPrefs.GetInt(
            RewardKey,
            isCompleted ? rewardStarPoint : 0
        );

        questButton.onClick.AddListener(CompleteQuest);
        resetQuestButton.onClick.AddListener(ResetQuest);

        questButton.gameObject.SetActive(!isCompleted);
        questCompleteText.gameObject.SetActive(false);

        UpdateStarUI();
    }

    public void CompleteQuest()
    {
        if (isCompleted)
            return;

        isCompleted = true;
        grantedReward = rewardStarPoint;

        // CurrencyManager에 명성 지급
        CurrencyManager.AddRenown(grantedReward);

        PlayerPrefs.SetInt(QuestKey, 1);
        PlayerPrefs.SetInt(RewardKey, grantedReward);
        PlayerPrefs.Save();

        questButton.gameObject.SetActive(false);
        UpdateStarUI();

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowCompleteMessage());
    }

    IEnumerator ShowCompleteMessage()
    {
        questCompleteText.text = "퀘스트 완료!";
        questCompleteText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(
            completeMessageDuration
        );

        questCompleteText.gameObject.SetActive(false);
        messageCoroutine = null;
    }

    public void ResetQuest()
    {
        // 퀘스트 완료 상태일 때만 보상 회수
        if (isCompleted)
        {
            int currentRenown = CurrencyManager.GetRenown();

            int removeAmount = Mathf.Min(
                currentRenown,
                grantedReward
            );

            CurrencyManager.AddRenown(-removeAmount);
        }

        isCompleted = false;
        grantedReward = 0;

        PlayerPrefs.SetInt(QuestKey, 0);
        PlayerPrefs.SetInt(RewardKey, 0);
        PlayerPrefs.Save();

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        questCompleteText.gameObject.SetActive(false);
        questButton.gameObject.SetActive(true);

        UpdateStarUI();
    }

    void UpdateStarUI()
    {
        if (starPointText != null)
        {
            starPointText.text =
                "스타성 : " + CurrencyManager.GetRenown();
        }
    }

    // 현재 명성 확인
    public int GetStarPoint()
    {
        return CurrencyManager.GetRenown();
    }

    // 명성 사용
    public bool SpendStarPoint(int amount)
    {
        if (amount < 0)
            return false;

        if (CurrencyManager.GetRenown() < amount)
            return false;

        CurrencyManager.AddRenown(-amount);

        UpdateStarUI();
        return true;
    }

    void OnEnable()
    {
        if (starPointText != null)
            UpdateStarUI();
    }
}
