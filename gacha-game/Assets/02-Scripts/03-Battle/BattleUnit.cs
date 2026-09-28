using UnityEngine;

public enum Team
{
    Ally,
    Enemy
}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "TurnSystem/Unit Data")]
public class BattleUnit : ScriptableObject
{
    [Header("기본 캐릭터 정보")]
    public string unitName; //유닛 이름
    public Team team;

    [Header("기본 스탯")]
    public int maxHP = 100;       //최대 HP
    public int attackPower = 10; //공격력
    public int speed= 10;       //스피드 (턴 순서 용)

    [Header("비주얼 정보")]
    public Sprite sprite;
}
