using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(UnitAnimator))] // 애니메이터 필수 요구
public class Unit : MonoBehaviour
{
    [Header("데이터 에셋 (SO)")]
    public BattleUnit unitData;

    [Header("런타임 상태 (읽기 전용 확인용)")]
    public int maxHp;
    public int currentHp;
    public int currentSpeed;

    [Header("전투 통계")]
    public int totalDamageDealt = 0;
    public int totalDamageTaken = 0;
    public int totalHealing = 0;

    [HideInInspector] public int skillUseCount = 0;
    [HideInInspector] public int hitCount = 0;

    private SpriteRenderer spriteRenderer;
    private UnitAnimator unitAnimator; // 애니메이터 컴포넌트 캐싱
    private Color originalColor;
    private Vector3 originalScale;
    private UnitHealthUI healthUI;

    public string UnitName => unitData != null ? unitData.unitName : name;
    public Team Team => unitData != null ? unitData.team : Team.Ally;
    public int BaseAttack => unitData != null ? unitData.attackPower : 10;
    public int Performance => unitData != null ? unitData.performance : 1;

    public bool IsAlive => currentHp > 0 && !isDead; // 사망 여부 반영

    [HideInInspector] public Vector3 originPos;

    [Header("타겟팅 선택 강조용")]
    private bool isTargetable = false;

    private bool isDead = false; // 중복 사망 방지용 플래그

    private void Awake()
    {
        healthUI = GetComponentInChildren<UnitHealthUI>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        unitAnimator = GetComponent<UnitAnimator>(); // 애니메이터 가져오기

        originalColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;
        originalScale = (transform.localScale != Vector3.zero) ? transform.localScale : Vector3.one;
        originPos = transform.position;

        if (unitData != null)
        {
            Initialize(unitData);
        }
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (unitData != null && unitData.characterSprite != null)
        {
            spriteRenderer.sprite = unitData.characterSprite;
        }
    }

    public void Initialize(BattleUnit data)
    {
        unitData = data;
        maxHp = data.maxHP;
        currentHp = maxHp;
        currentSpeed = data.speed;
        skillUseCount = 0;
        hitCount = 0;

        // SO에 있는 기본 스프라이트와 피격 스프라이트를 애니메이터에 주입
        if (unitAnimator == null) unitAnimator = GetComponent<UnitAnimator>();
        if (unitAnimator != null)
        {
            unitAnimator.SetupSprites(data.characterSprite, data.hitSprite, data.attackSprite);
        }

        if (healthUI != null) healthUI.UpdateHP(currentHp, maxHp);
    }

    public void TakeDamage(int damage)
    {
        currentHp = Mathf.Max(0, currentHp - damage);
        totalDamageTaken += damage;
        hitCount++; // 피격 횟수 카운트

        // 데미지를 입었을 때 피격 스프라이트 모션 실행
        if (unitAnimator != null)
        {
            unitAnimator.PlayHit();
        }

        Debug.Log($"<color=orange>{UnitName}</color>이 {damage} 피해를 입음 (남은 HP: {currentHp}/{maxHp})");

        if (healthUI != null)
        {
            healthUI.UpdateHP(currentHp, maxHp);
            healthUI.ShowDamage(damage);
        }

        if (!IsAlive)
        {
            Debug.Log($"<color=red>{UnitName} 사망</color>");
        }

        // 사망 처리
        if (currentHp <= 0 && !isDead)
        {
            Die();
        }
    }
    // 사망 처리 함수
    public void Die()
    {
        isDead = true;
        SetTargetable(false);
        Debug.Log($"<color=red>{UnitName} 사망</color>");

        // 1. 머리 위 체력바 즉시 숨김
        if (healthUI != null)
        {
            healthUI.gameObject.SetActive(false);
        }

        // 2. 마우스 클릭 타겟팅에 걸리지 않도록 콜라이더 비활성화
        Collider col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;

        // 3. 서서히 페이드아웃 후 비활성화
        StartCoroutine(FadeOutAndDisableRoutine());
    }

    private IEnumerator FadeOutAndDisableRoutine()
    {
        // 타격 리액션(피격 스프라이트)을 잠시 보여준 뒤 사라지기 시작
        yield return new WaitForSeconds(0.35f);

        // UnitAnimator가 스프라이트를 다시 기본으로 되돌리지 못하도록 중지
        if (unitAnimator != null)
        {
            unitAnimator.StopAllCoroutines();
        }

        float fadeDuration = 0.5f; // 0.5초 동안 서서히 사라짐
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        // 완전히 투명해진 후 게임오브젝트 끄기
        gameObject.SetActive(false);
    }


    public void ModifySpeed(int amount)
    {
        currentSpeed += amount;
        Debug.Log($"{UnitName}의 스피드 변경: {currentSpeed}");
    }

    public void Heal(int amount)
    {
        int prevHp = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        int actualHeal = currentHp - prevHp;

        totalHealing += actualHeal;
        Debug.Log($"<color=green>{UnitName}</color>이(가) {actualHeal} 회복! (현재 HP: {currentHp}/{maxHp})");

        if (healthUI != null)
        {
            healthUI.UpdateHP(currentHp, maxHp);
            healthUI.ShowHeal(actualHeal);
        }
    }

    public void SetTargetable(bool enable)
    {
        isTargetable = enable;
        if (!enable && spriteRenderer != null)
        {
            if (originalColor.a == 0f) originalColor = Color.white;
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseEnter()
    {
        if (isTargetable && IsAlive && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    private void OnMouseExit()
    {
        if (isTargetable && IsAlive && spriteRenderer != null)
        {
            if (originalColor.a == 0f) originalColor = Color.white;
            spriteRenderer.color = originalColor;
        }
    }

    public void SetDimmed(bool isDimmed)
    {
        if (spriteRenderer == null) return;

        if (isDimmed)
        {
            spriteRenderer.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }
        else
        {
            if (originalColor.a == 0f) originalColor = Color.white;
            spriteRenderer.color = originalColor;
        }
    }
}