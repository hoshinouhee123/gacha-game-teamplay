using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRoster", menuName = "TurnSystem/Character Roster")]
public class CharacterRosterSO : ScriptableObject
{
    [Tooltip("편성 화면에서 선택할 수 있는 아군 BattleUnit SO 목록")]
    public BattleUnit[] characters;

    public BattleUnit FindById(string id)
    {
        if (characters == null || string.IsNullOrEmpty(id)) return null;

        foreach (BattleUnit character in characters)
        {
            if (character != null && character.characterId == id)
                return character;
        }

        return null;
    }
}
