
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

    private int starPoint;
    private bool isCompleted;
    private int grantedReward;
    private Coroutine messageCoroutine;

    private const string StarKey = "StarPoint";
    private const string QuestKey = "QuestCompleted";
    private const string RewardKey = "QuestGrantedReward";

    void Start()
    {
        starPoint = PlayerPrefs.GetInt(StarKey, 0);
        isCompleted = PlayerPrefs.GetInt(QuestKey, 0) == 1;

        // 이전 버전에서 완료한 퀘스트도 되돌릴 수 있도록 처리
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
        if (isCompleted) return;

        isCompleted = true;
        grantedReward = rewardStarPoint;
        starPoint += grantedReward;

        PlayerPrefs.SetInt(StarKey, starPoint);
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
        // 완료하지 않은 퀘스트는 보상을 차감하지 않음
        if (isCompleted)
        {
            starPoint = Mathf.Max(0, starPoint - grantedReward);
        }

        isCompleted = false;
        grantedReward = 0;

        PlayerPrefs.SetInt(StarKey, starPoint);
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
        starPointText.text = "스타성 : " + starPoint;
    }

    public int GetStarPoint()
    {
        return starPoint;
    }

    public bool SpendStarPoint(int amount)
    {
        if (amount < 0 || starPoint < amount)
            return false;

        starPoint -= amount;

        PlayerPrefs.SetInt(StarKey, starPoint);
        PlayerPrefs.Save();

        UpdateStarUI();
        return true;
    }
}
