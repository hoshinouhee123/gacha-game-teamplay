using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PartyFormationUI : MonoBehaviour
{
    [Header("아군 캐릭터 목록 SO")]
    [SerializeField] private CharacterRosterSO roster;

    [Header("편성 슬롯 5개: 전투 1~3, 교대 1~2 순서")]
    [SerializeField] private Button[] slotButtons = new Button[5];
    [SerializeField] private TMP_Text[] slotNameTexts = new TMP_Text[5];
    [SerializeField] private Image[] slotPortraits = new Image[5];

    [Header("캐릭터 선택 목록")]
    [SerializeField] private Transform characterListContent;
    [SerializeField] private PartyCharacterCardUI characterCardPrefab;

    [Header("추가 UI")]
    [SerializeField] private TMP_Text selectedSlotText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearSlotButton;

    private readonly BattleUnit[] members =
        new BattleUnit[PartyFormationStorage.TotalCount];
    private int selectedSlotIndex;

    private void Start()
    {
        if (roster == null || characterListContent == null ||
            characterCardPrefab == null || slotButtons.Length != 5 ||
            slotNameTexts.Length != 5 || slotPortraits.Length != 5)
        {
            Debug.LogError("[편성] Inspector 필수 참조 또는 슬롯 배열 크기를 확인하세요.");
            enabled = false;
            return;
        }

        if (PartyFormationStorage.TryLoad(roster, out BattleUnit[] loaded))
        {
            for (int i = 0; i < members.Length; i++) members[i] = loaded[i];
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => SelectSlot(index));
        }

        if (saveButton != null) saveButton.onClick.AddListener(SaveFormation);
        if (clearSlotButton != null) clearSlotButton.onClick.AddListener(ClearSelectedSlot);

        // 같은 SO 또는 같은 ID가 중복 등록되었다면 목록에 한 번만 표시
        var addedIds = new HashSet<string>();
        if (roster.characters != null)
        {
            foreach (BattleUnit character in roster.characters)
            {
                if (character == null || character.team != Team.Ally ||
                    string.IsNullOrWhiteSpace(character.characterId) ||
                    !addedIds.Add(character.characterId)) continue;

                PartyCharacterCardUI card = Instantiate(
                    characterCardPrefab, characterListContent);
                card.Bind(character, SelectCharacter);
            }
        }

        RefreshUI();
    }

    private string SlotLabel(int index)
    {
        return index < 3
            ? $"전투 {index + 1}"
            : $"교대 {index - 2}";
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= members.Length) return;
        selectedSlotIndex = index;
        RefreshUI();
    }

    public void SelectCharacter(BattleUnit character)
    {
        if (character == null) return;

        // 이미 배치된 캐릭터를 선택하면 기존 슬롯과 서로 교환
        int existingIndex = -1;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == character)
            {
                existingIndex = i;
                break;
            }
        }

        BattleUnit previous = members[selectedSlotIndex];
        members[selectedSlotIndex] = character;
        if (existingIndex >= 0 && existingIndex != selectedSlotIndex)
            members[existingIndex] = previous;

        if (messageText != null) messageText.text = "변경 사항을 저장해 주세요.";
        RefreshUI();
    }

    public void ClearSelectedSlot()
    {
        members[selectedSlotIndex] = null;
        if (messageText != null) messageText.text = "변경 사항을 저장해 주세요.";
        RefreshUI();
    }

    public void SaveFormation()
    {
        bool success = PartyFormationStorage.Save(roster, members);
        if (messageText != null)
            messageText.text = success
                ? "전투 3명 + 교대 2명 편성이 저장되었습니다!"
                : "서로 다른 아군 5명을 모두 배치해야 저장할 수 있어요.";
    }

    private void RefreshUI()
    {
        for (int i = 0; i < members.Length; i++)
        {
            BattleUnit member = members[i];
            if (slotNameTexts[i] != null)
                slotNameTexts[i].text = SlotLabel(i) + ": " +
                    (member != null ? member.unitName : "비어 있음");

            if (slotPortraits[i] != null)
            {
                slotPortraits[i].sprite = member != null ? member.characterSprite : null;
                slotPortraits[i].enabled = member != null && member.characterSprite != null;
            }
        }
        if (selectedSlotText != null)
            selectedSlotText.text = "선택 중: " + SlotLabel(selectedSlotIndex);
    }

    public void MainMenuBack()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
