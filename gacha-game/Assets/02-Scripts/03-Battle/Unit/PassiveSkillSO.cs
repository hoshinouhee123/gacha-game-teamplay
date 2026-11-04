using UnityEngine;

public abstract class PassiveSkillSO : ScriptableObject
{
    public string passiveName;
    [TextArea] public string passiveDescription;

    // 전투 시작 시 초기화
    public virtual void OnBattleStart(Unit self) { }

    // 공격 시 처치 가능 여부 (false면 적 체력을 1 미만으로 깎지 못함)
    public virtual bool CanKillEnemy(Unit self, SkillDataSO skill) => true;

    // 피격 직전 확정 회피 여부 확인
    public virtual bool CheckForceDodge(Unit self, Unit attacker) => false;

    // 회피 성공 시 호출 (반사 딜 등)
    public virtual void OnDodgeSuccess(Unit self, Unit attacker, int incomingDamage) { }
}