using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRoster", menuName = "TurnSystem/Character Roster")]
public class CharacterRosterSO : ScriptableObject
{
    [Tooltip("편성 화면에서 선택할 수 있는 아군 BattleUnit SO 목록")]
    public BattleUnit[] characters;

    [Header("새 게임 시작 시 1회 무료 지급 (선택)")]
    [Tooltip("최초 초기화 때 자동 지급할 캐릭터. 캐릭터 목록에도 등록해야 합니다.")]
    public BattleUnit[] starterCharacters;

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
