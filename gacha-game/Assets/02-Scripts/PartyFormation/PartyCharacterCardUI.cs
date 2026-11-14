using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyCharacterCardUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;

    public void Bind(BattleUnit character, Action<BattleUnit> onClick)
    {
        if (nameText != null) nameText.text = character.unitName;
        if (statsText != null)
            statsText.text = $"HP {character.maxHP} / ATK {character.attackPower} / SPD {character.speed}";

        if (portrait != null)
        {
            portrait.sprite = character.characterSprite;
            portrait.enabled = character.characterSprite != null;
        }

        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(character));
        }
    }
}
