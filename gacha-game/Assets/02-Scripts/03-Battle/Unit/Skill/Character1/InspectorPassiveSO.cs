using UnityEngine;

[CreateAssetMenu(fileName = "Character1Passive", menuName = "TurnSystem/Passive")]
public class InspectorPassiveSO : PassiveSkillSO
{
    [Header("처치 제한 규칙")]
    [Tooltip("체크 시 일반 스킬로 적을 처치할 수 없음 (HP 1 남김)")]
    public bool cannotKillEnemies = true;
    public SkillSpecialType killAllowedSkillType = SkillSpecialType.PermissionToFire; // 3스킬만 처치 가능

    [Header("첫 공격 회피 & 반사")]
    public bool guaranteedFirstDodge = true;
    [Range(0f, 1f)] public float reflectRatio = 0.2f; // 회피 시 20% 반사

    public override void OnBattleStart(Unit self)
    {
        self.hasUsedFirstDodge = false;
    }

    public override bool CanKillEnemy(Unit self, SkillDataSO skill)
    {
        if (!cannotKillEnemies) return true;
        // 3스킬(PermissionToFire)일 때만 처치 허용
        return skill != null && skill.specialType == killAllowedSkillType;
    }

    public override bool CheckForceDodge(Unit self, Unit attacker)
    {
        if (guaranteedFirstDodge && !self.hasUsedFirstDodge)
        {
            self.hasUsedFirstDodge = true;
            return true; // 첫 피격 확정 회피
        }
        return false;
    }

    public override void OnDodgeSuccess(Unit self, Unit attacker, int incomingDamage)
    {
        if (reflectRatio <= 0f || attacker == null) return;

        int reflectDamage = Mathf.RoundToInt(incomingDamage * reflectRatio);
        if (reflectDamage > 0)
        {
            Debug.Log($"<color=#F43F5E>[반사] {self.UnitName}이(가) {attacker.UnitName}에게 {reflectDamage} 반사 피해!</color>");
            attacker.TakeDamage(reflectDamage, true);
        }
    }
}