using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    private const string SaveKey = "QuestProgress_V2";
    private const int StandardRenownReward = 120;
    private const int RenownPerLevel = 40;
    private const int DrawTarget = 10;
    private const int StarDrawTarget = 5;
    private const int MaterialReward = 20;

    [Serializable]
    private class QuestProgress
    {
        public int nextStagePercent = 10;
        public List<string> clearedStageIds = new List<string>();

        public int lastAllyClears;
        public int fastBattleClears;
        public int damageClears;

        public int drawProgress;
        public int[] starDrawProgress = new int[3];
        public int[] characterLevelUps = new int[InventoryStorage.CharacterCount];

        public int pendingDrawRewards;
        public int pendingUpgradeRewards;
    }

    [Header("퀘스트 카드 본문 TMP")]
    [SerializeField] private TMP_Text stageQuestText;
    [SerializeField] private TMP_Text battleQuestText;
    [SerializeField] private TMP_Text gachaQuestText;
    [SerializeField] private TMP_Text upgradeQuestText;

    [Header("명성 수령 버튼")]
    [SerializeField] private Button gachaClaimButton;
    [SerializeField] private Button upgradeClaimButton;

    private static QuestProgress progress;
    private static event Action ProgressChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        progress = null;
        ProgressChanged = null;
    }

    private void OnEnable()
    {
        ProgressChanged += RefreshQuestText;

        if (TryGrantStarDrawRewards(GetProgress()))
            SaveProgressAndRefresh();
        else
            RefreshQuestText();
    }

    private void OnDisable()
    {
        ProgressChanged -= RefreshQuestText;
    }

    public static void RecordStageClear(string stageId, int stagePercent)
    {
        if (string.IsNullOrWhiteSpace(stageId) ||
            stagePercent < 0 || stagePercent > 100)
        {
            return;
        }

        QuestProgress current = GetProgress();

        if (current.nextStagePercent > 100 ||
            stagePercent < current.nextStagePercent ||
            current.clearedStageIds.Contains(stageId))
        {
            return;
        }

        CurrencyManager.AddRenown(StandardRenownReward);
        current.clearedStageIds.Add(stageId);
        current.nextStagePercent += 10;
        SaveProgressAndRefresh();
    }

    public static void RecordBattleClear(
        int survivingAllies,
        int turnsUsed,
        int damageDealt,
        int damageTarget)
    {
        bool lastAlly = survivingAllies == 1;
        bool fastClear = turnsUsed >= 1 && turnsUsed <= 5;
        bool damageClear = damageTarget > 0 && damageDealt >= damageTarget;

        int completedConditions = (lastAlly ? 1 : 0) +
            (fastClear ? 1 : 0) + (damageClear ? 1 : 0);

        if (completedConditions == 0)
            return;

        QuestProgress current = GetProgress();
        CurrencyManager.AddRenown(completedConditions * StandardRenownReward);

        if (lastAlly)
            current.lastAllyClears++;
        if (fastClear)
            current.fastBattleClears++;
        if (damageClear)
            current.damageClears++;

        SaveProgressAndRefresh();
    }

    public static void RecordGacha(int[] stars)
    {
        if (stars == null || stars.Length == 0)
            return;

        foreach (int star in stars)
        {
            if (star < 1 || star > 3)
                return;
        }

        QuestProgress current = GetProgress();
        long totalDraws = (long)current.drawProgress + stars.Length;

        current.pendingDrawRewards += (int)(totalDraws / DrawTarget);
        current.drawProgress = (int)(totalDraws % DrawTarget);

        foreach (int star in stars)
            current.starDrawProgress[star - 1]++;

        TryGrantStarDrawRewards(current);
        SaveProgressAndRefresh();
    }

    public static void RecordCharacterUpgrade(
        int characterId,
        int oldLevel,
        int newLevel)
    {
        if (characterId < 1 || characterId > InventoryStorage.CharacterCount ||
            oldLevel < 1 || newLevel <= oldLevel ||
            newLevel > InventoryStorage.MaxCharacterLevel)
        {
            return;
        }

        QuestProgress current = GetProgress();
        int levelsGained = newLevel - oldLevel;

        current.characterLevelUps[characterId - 1] += levelsGained;
        current.pendingUpgradeRewards += levelsGained;

        SaveProgressAndRefresh();
    }

    public void ClaimGachaRenown()
    {
        QuestProgress current = GetProgress();
        ClaimRenown(ref current.pendingDrawRewards, StandardRenownReward);
    }

    public void ClaimUpgradeRenown()
    {
        QuestProgress current = GetProgress();
        ClaimRenown(ref current.pendingUpgradeRewards, RenownPerLevel);
    }

    private static void ClaimRenown(ref int pendingRewards, int rewardPerClear)
    {
        if (pendingRewards <= 0)
            return;

        int rewardCount = Mathf.Min(pendingRewards, int.MaxValue / rewardPerClear);
        int reward = rewardCount * rewardPerClear;

        // 지급이 진행되는 동안 같은 보상이 중복 수령되지 않게 처리
        pendingRewards -= rewardCount;

        try
        {
            CurrencyManager.AddRenown(reward);
        }
        catch (Exception exception)
        {
            pendingRewards += rewardCount;
            Debug.LogError("퀘스트 명성 지급 실패: " + exception.Message);
            return;
        }

        SaveProgressAndRefresh();
    }

    private static bool TryGrantStarDrawRewards(QuestProgress current)
    {
        List<int> rewardStars = new List<int>(3);
        List<int> rewardAmounts = new List<int>(3);

        for (int i = 0; i < current.starDrawProgress.Length; i++)
        {
            int completedQuests = current.starDrawProgress[i] / StarDrawTarget;

            if (completedQuests <= 0)
                continue;

            rewardStars.Add(i + 1);
            rewardAmounts.Add(completedQuests * MaterialReward);
        }

        if (rewardStars.Count == 0 ||
            !InventoryStorage.AddRewards(rewardStars.ToArray(), rewardAmounts.ToArray()))
        {
            return false;
        }

        foreach (int star in rewardStars)
            current.starDrawProgress[star - 1] %= StarDrawTarget;

        return true;
    }

    private static QuestProgress GetProgress()
    {
        if (progress != null)
            return progress;

        string json = PlayerPrefs.GetString(SaveKey, "");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                progress = JsonUtility.FromJson<QuestProgress>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning("퀘스트 진행도 불러오기 실패: " + exception.Message);
            }
        }

        if (progress == null)
            progress = new QuestProgress();

        if (progress.clearedStageIds == null)
            progress.clearedStageIds = new List<string>();

        if (progress.starDrawProgress == null || progress.starDrawProgress.Length != 3)
            Array.Resize(ref progress.starDrawProgress, 3);

        if (progress.characterLevelUps == null ||
            progress.characterLevelUps.Length != InventoryStorage.CharacterCount)
        {
            Array.Resize(ref progress.characterLevelUps, InventoryStorage.CharacterCount);
        }

        progress.nextStagePercent = Mathf.Clamp(progress.nextStagePercent, 10, 110);
        return progress;
    }

    private static void SaveProgressAndRefresh()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(GetProgress()));
        PlayerPrefs.Save();
        ProgressChanged?.Invoke();
    }

    private void RefreshQuestText()
    {
        QuestProgress current = GetProgress();

        if (stageQuestText != null)
        {
            stageQuestText.text =
                "스타성 조건을 충족한 스테이지 클리어\n" +
                $"달성 보상 · 명성 {StandardRenownReward}";
        }

        if (battleQuestText != null)
        {
            battleQuestText.text =
                "전투 조건을 충족하고 승리\n" +
                $"조건당 보상 · 명성 {StandardRenownReward}";
        }

        if (gachaQuestText != null)
        {
            gachaQuestText.text =
                $"뽑기 퀘스트\n달성 보상 · 명성 {StandardRenownReward}";
        }

        if (upgradeQuestText != null)
        {
            upgradeQuestText.text =
                $"강화 퀘스트\n레벨 상승당 명성 {RenownPerLevel}";
        }

        RefreshClaimButton(
            gachaClaimButton,
            current.pendingDrawRewards,
            StandardRenownReward
        );

        RefreshClaimButton(
            upgradeClaimButton,
            current.pendingUpgradeRewards,
            RenownPerLevel
        );
    }

    private static void RefreshClaimButton(Button button, int pendingRewards, int rewardPerClear)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(pendingRewards > 0);

        if (pendingRewards <= 0)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            int rewardCount = Mathf.Min(pendingRewards, int.MaxValue / rewardPerClear);
            label.text = $"명성 {rewardCount * rewardPerClear} 수령";
        }
    }

    private static string FormatStarProgress(QuestProgress current, int star)
    {
        int count = current.starDrawProgress[star - 1];
        int pendingRewards = count / StarDrawTarget;
        string pendingText = pendingRewards > 0
            ? $" · 지급 대기 {pendingRewards}회"
            : "";

        return $"{star}성 등장 {count % StarDrawTarget}/{StarDrawTarget}회" +
               $" · 교보재 {MaterialReward}개{pendingText}";
    }
}