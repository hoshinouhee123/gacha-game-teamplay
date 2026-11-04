using UnityEngine;

public enum SkillSpecialType
{
    Normal,             // 일반 스킬
    WarrantExecution,   // 1스킬: 영장 부여 (이속 1 고정)
    SurpriseRaid,       // 2스킬: 영장 소모 시 전체 100% 관통 피해
    PermissionToFire    // 3스킬: 5연타(50% x 5) + 처치 가능
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "TurnSystem/Skill Data")]
public class SkillDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    public int spCost = 0;
    public int spGain = 0;
    public float damageMultiplier = 1.0f;

    [Header("스킬 특수 기믹")]
    public SkillSpecialType specialType = SkillSpecialType.Normal;
    [Tooltip("다단 히트 횟수 (기본 1타)")]
    public int hitCount = 1;
}