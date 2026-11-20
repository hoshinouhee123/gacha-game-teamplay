using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUpgradeUI : MonoBehaviour
{
    [Serializable]
    private class MaterialSlot
    {
        public GameObject slotRoot;
        public Button addButton;
        public TMP_Text ownedText;
        public Button removeButton;
        public TMP_Text selectedText;
    }

    [Header("캐릭터 이름")]
    [SerializeField]
    private string[] characterNames =
    {
        "캐릭터 1", "캐릭터 2", "캐릭터 3", "캐릭터 4", "캐릭터 5"
    };

    [Header("강화 패널")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TMP_Text characterText;
    [SerializeField] private TMP_Text materialsText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button upgradeButton;

    [Header("등급별 재료 칸")]
    [SerializeField] private MaterialSlot oneStarSlot = new MaterialSlot();
    [SerializeField] private MaterialSlot twoStarSlot = new MaterialSlot();
    [SerializeField] private MaterialSlot threeStarSlot = new MaterialSlot();

    private readonly int[] selectedAmounts = new int[3];
    private int selectedCharacterId;

    private void Awake()
    {
        ClosePanel();
    }

    private void OnEnable()
    {
        InventoryStorage.OnInventoryChanged += RefreshPanel;
        RefreshPanel();
    }

    private void OnDisable()
    {
        InventoryStorage.OnInventoryChanged -= RefreshPanel;
    }

    public void OpenCharacter(int characterId)
    {
        if (characterId < 1 || characterId > InventoryStorage.CharacterCount)
            return;

        selectedCharacterId = characterId;
        Array.Clear(selectedAmounts, 0, selectedAmounts.Length);
        SetMessage("");

        RefreshPanel();

        if (upgradePanel != null)
            upgradePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        selectedCharacterId = 0;
        Array.Clear(selectedAmounts, 0, selectedAmounts.Length);

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    public void AddMaterial(int star)
    {
        if (selectedCharacterId == 0 || star < 1 || star > 3)
            return;

        int index = star - 1;
        int owned = InventoryStorage.GetItemCount(star);
        int remaining = InventoryStorage.GetRemainingMaterialsToMaxLevel(
            selectedCharacterId
        );

        if (selectedAmounts[index] >= owned || GetTotalSelected() >= remaining)
            return;

        selectedAmounts[index]++;
        SetMessage("");
        RefreshPanel();
    }

    public void RemoveMaterial(int star)
    {
        if (selectedCharacterId == 0 || star < 1 || star > 3)
            return;

        int index = star - 1;

        if (selectedAmounts[index] == 0)
            return;

        selectedAmounts[index]--;
        SetMessage("");
        RefreshPanel();
    }

    public void UpgradeSelectedCharacter()
    {
        if (selectedCharacterId == 0 || GetTotalSelected() == 0)
            return;

        if (!InventoryStorage.TryUpgradeWithMaterials(
                selectedCharacterId, selectedAmounts))
        {
            RefreshPanel();
            SetMessage("강화에 실패했습니다. 재료 수량과 Console을 확인해 주세요.");
            return;
        }

        Array.Clear(selectedAmounts, 0, selectedAmounts.Length);
        RefreshPanel();
        SetMessage("강화 완료!");
    }

    private void RefreshPanel()
    {
        if (selectedCharacterId == 0)
            return;

        int oneStarOwned = Mathf.Max(0, InventoryStorage.GetItemCount(1));
        int twoStarOwned = Mathf.Max(0, InventoryStorage.GetItemCount(2));
        int threeStarOwned = Mathf.Max(0, InventoryStorage.GetItemCount(3));

        selectedAmounts[0] = Mathf.Min(selectedAmounts[0], oneStarOwned);
        selectedAmounts[1] = Mathf.Min(selectedAmounts[1], twoStarOwned);
        selectedAmounts[2] = Mathf.Min(selectedAmounts[2], threeStarOwned);

        int totalSelected = GetTotalSelected();
        int remaining = InventoryStorage.GetRemainingMaterialsToMaxLevel(
            selectedCharacterId
        );
        bool canAddMore = totalSelected < remaining;

        RefreshSlot(oneStarSlot, oneStarOwned, selectedAmounts[0], canAddMore);
        RefreshSlot(twoStarSlot, twoStarOwned, selectedAmounts[1], canAddMore);
        RefreshSlot(threeStarSlot, threeStarOwned, selectedAmounts[2], canAddMore);

        int level = InventoryStorage.GetCharacterLevel(selectedCharacterId);
        int experience = InventoryStorage.GetCharacterExperience(selectedCharacterId);

        InventoryStorage.GetMaterialUpgradePreview(
            selectedCharacterId,
            selectedAmounts,
            out long gainedExperience,
            out int nextLevel,
            out int nextExperience
        );

        if (characterText != null)
        {
            characterText.text =
                $"{GetCharacterName()}\n현재 {FormatLevelProgress(level, experience)}";
        }

        if (materialsText != null)
        {
            materialsText.text =
                $"사용 예정 교보재 {totalSelected}개\n" +
                $"강화 후 {FormatLevelProgress(nextLevel, nextExperience)}";
        }

        if (upgradeButton != null)
            upgradeButton.interactable = gainedExperience > 0;
    }

    private void RefreshSlot(
        MaterialSlot slot,
        int owned,
        int selected,
        bool canAddMore)
    {
        if (slot.slotRoot != null)
            slot.slotRoot.SetActive(owned > 0);

        if (slot.ownedText != null)
            slot.ownedText.text = $"보유 {owned}개";

        if (slot.selectedText != null)
            slot.selectedText.text = $"담은 재료 {selected}개";

        if (slot.addButton != null)
            slot.addButton.interactable = canAddMore && selected < owned;

        if (slot.removeButton != null)
            slot.removeButton.interactable = selected > 0;
    }

    private int GetTotalSelected()
    {
        return selectedAmounts[0] + selectedAmounts[1] + selectedAmounts[2];
    }

    private string FormatLevelProgress(int level, int experience)
    {
        int required = InventoryStorage.GetRequiredMaterials(level);

        if (required == 0)
            return $"Lv. {level} · 최대 레벨";

        int progress = experience / InventoryStorage.ExperiencePerMaterial;
        return $"Lv. {level} · 교보재 {progress} / {required}개";
    }

    private string GetCharacterName()
    {
        int index = selectedCharacterId - 1;

        if (characterNames != null && index >= 0 &&
            index < characterNames.Length &&
            !string.IsNullOrWhiteSpace(characterNames[index]))
        {
            return characterNames[index];
        }

        return $"캐릭터 {selectedCharacterId}";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}