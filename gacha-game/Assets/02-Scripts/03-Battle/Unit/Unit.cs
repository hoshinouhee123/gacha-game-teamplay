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
    private UnitHealthUI healthUI;

    // SO의 기본 정보를 편하게 읽어오는 프로퍼티
    public string UnitName => unitData != null ? unitData.unitName : name;
    public Team Team => unitData != null ? unitData.team : Team.Ally;
    public int BaseAttack => unitData != null ? unitData.attackPower : 10;
    public int Performance => unitData != null ? unitData.performance : 1;
    public bool IsAlive => currentHp > 0;

    [HideInInspector] public Vector3 originPos;

    [Header("타겟팅 선택 강조용")]
    private bool isTargetable = false;

    private void Awake()
    {

        healthUI = GetComponentInChildren<UnitHealthUI>(); // 자식에서 UI 컴포넌트 자동 탐색

        // 1. 스프라이트 렌더러와 기본 색상/크기 안전하게 기억
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        originalColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;
        originalScale = (transform.localScale != Vector3.zero) ? transform.localScale : Vector3.one;

        originPos = transform.position; // 시작할 때 자기 원래 자리 기억

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

        if (healthUI != null) healthUI.UpdateHP(currentHp, maxHp);
    }

    public void TakeDamage(int damage)
    {
        currentHp = Mathf.Max(0, currentHp - damage);
        totalDamageTaken += damage; // 받은 피해량 누적 기록
        Debug.Log($"<color=orange>{UnitName}</color>이 {damage} 피해를 입음 (남은 HP: {currentHp}/{maxHp})");

        // 머리 위 체력바 깎고 데미지 숫자 띄우기
        if (healthUI != null)
        {
            healthUI.UpdateHP(currentHp, maxHp);
            healthUI.ShowDamage(damage);
        }

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

        // 머리 위 체력바 채우고 초록색 힐 숫자 띄우기!
        if (healthUI != null)
        {
            healthUI.UpdateHP(currentHp, maxHp);
            healthUI.ShowHeal(actualHeal);
        }
    }

    // 타겟팅 선택 모드 켜고 끄기
    public void SetTargetable(bool enable)
    {
        isTargetable = enable;
        if (!enable && spriteRenderer != null)
        {
            // 원래 색상으로 복구 
            if (originalColor.a == 0f) originalColor = Color.white;
            spriteRenderer.color = originalColor;
        }
    }

    // 마우스 올렸을 때: 색상만 붉은색으로 변경
    private void OnMouseEnter()
    {
        if (isTargetable && IsAlive && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    // 마우스 나갔을 때: 원래 색상으로 복구
    private void OnMouseExit()
    {
        if (isTargetable && IsAlive && spriteRenderer != null)
        {
            if (originalColor.a == 0f) originalColor = Color.white;
            spriteRenderer.color = originalColor;
        }
    }

    // 다른 아군 톤다운(어둡게) 처리
    public void SetDimmed(bool isDimmed)
    {
        if (spriteRenderer == null) return;

        if (isDimmed)
        {
            // 색상을 어두운 회색(밝기 약 25%)으로 낮춤
            spriteRenderer.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }
        else
        {
            // 원래 밝기로 복구
            if (originalColor.a == 0f) originalColor = Color.white;
            spriteRenderer.color = originalColor;
        }
    }
}