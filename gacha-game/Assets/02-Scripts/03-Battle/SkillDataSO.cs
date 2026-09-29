using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "TurnSystem/Skill Data")]
public class SkillDataSO : ScriptableObject
{
    [Header("스킬 정보")]
    public string skillName;
    [TextArea] public string description;

    [Header("스펙")]
    [Tooltip("1.0 = 공격력의 100%, 1.5 = 150%, 2.0 = 200%")]
    public float damageMultiplier = 1.0f;

    [Header("SP 설정")]
    public int spCost = 0; // 소모량 (스킬2: 2, 스킬3: 4)
    public int spGain = 0; // 회복량 (스킬1: 1)
}