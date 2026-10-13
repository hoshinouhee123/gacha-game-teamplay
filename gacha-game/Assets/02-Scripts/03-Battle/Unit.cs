using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Unit : MonoBehaviour
{
    [Header("데이터 에셋 (SO)")]
    public BattleUnit unitData;

    // 인게임에서 실시간으로 변하는 상태값들
    [Header("런타임 상태 (읽기 전용 확인용)")]
    public int maxHp;
    public int currentHp;
    public int currentSpeed; // 버프/디버프로 스피드가 변동될 수 있으므로 분리

    // ================= [ 전투 통계 데이터 ] =================
    [Header("전투 통계")]
    public int totalDamageDealt = 0; // 적에게 입힌 총 피해량
    public int totalDamageTaken = 0; // 받은 총 피해량
    public int totalHealing = 0;     // 총 회복량

    // 스타성 계산을 위한 누적 카운터
    [HideInInspector] public int skillUseCount = 0; // 스킬 사용 횟수
    [HideInInspector] public int hitCount = 0;      // 피격 횟수

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;

    // SO의 기본 정보를 편하게 읽어오는 프로퍼티
    public string UnitName => unitData != null ? unitData.unitName : name;
    public Team Team => unitData != null ? unitData.team : Team.Ally;
    public int BaseAttack => unitData != null ? unitData.attackPower : 10;
    public int Performance => unitData != null ? unitData.performance : 1;
    public bool IsAlive => currentHp > 0;

    private void Awake()
    {
        if (unitData != null)
        {
            Initialize(unitData);
        }
    }

    // 에디터에서 SO를 넣거나 바꿨을 때 '재생(Play)' 안 해도 즉시 스프라이트를 보여주는 함수
    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (unitData != null && unitData.characterSprite != null)
        {
            spriteRenderer.sprite = unitData.characterSprite;
        }
    }

    // 데이터 주입 함수 (동적으로 몬스터를 스폰할 때도 사용 가능)
    public void Initialize(BattleUnit data)
    {
        unitData = data;
        maxHp = data.maxHP;
        currentHp = maxHp;
        currentSpeed = data.speed;
        skillUseCount = 0;
        hitCount = 0;

        if (spriteRenderer != null && data.characterSprite != null)
        {
            spriteRenderer.sprite = data.characterSprite;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp = Mathf.Max(0, currentHp - damage);
        totalDamageTaken += damage; // 받은 피해량 누적 기록
        Debug.Log($"<color=orange>{UnitName}</color>이 {damage} 피해를 입음 (남은 HP: {currentHp}/{maxHp})");

        if (!IsAlive)
        {
            Debug.Log($"<color=red>{UnitName} 사망</color>");
        }
    }

    // 추후 속도 증가/감소 버프를 처리할 때
    public void ModifySpeed(int amount)
    {
        currentSpeed += amount;
        Debug.Log($"{UnitName}의 스피드 변경: {currentSpeed}");
    }

    // 나중에 회복 스킬을 쓸 때 호출할 함수
    public void Heal(int amount)
    {
        int prevHp = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        int actualHeal = currentHp - prevHp;

        totalHealing += actualHeal; // 회복량 누적 기록
        Debug.Log($"<color=green>{UnitName}</color>이(가) {actualHeal} 회복! (현재 HP: {currentHp}/{maxHp})");
    }
}