using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(UnitAnimator))]
public class Unit : MonoBehaviour
{
    [Header("데이터 에셋 (SO)")]
    public BattleUnit unitData;

    [Header("런타임 상태 (읽기 전용 확인용)")]
    public int maxHp;
    public int currentHp;
    public int currentSpeed;

    [Header("기믹 런타임 상태")]
    public bool hasWarrant = false;         // 영장 디버프 여부
    public int originalSpeed;               // 원래 속도 복구용
    [HideInInspector] public bool hasUsedFirstDodge = false; // 첫 확정 회피 사용 여부

    [Header("전투 통계")]
    public int totalDamageDealt = 0;
    public int totalDamageTaken = 0;
    public int totalHealing = 0;

    [HideInInspector] public int skillUseCount = 0;
    [HideInInspector] public int hitCount = 0;

    private SpriteRenderer spriteRenderer;
    private UnitAnimator unitAnimator;
    private Color originalColor;
    private Vector3 originalScale;
    private UnitHealthUI healthUI;

    public string UnitName => unitData != null ? unitData.unitName : name;
    public Team Team => unitData != null ? unitData.team : Team.Ally;
    public int BaseAttack => unitData != null ? unitData.attackPower : 10;
    public int Performance => unitData != null ? unitData.performance : 1;
    public PassiveSkillSO Passive => unitData != null ? unitData.passiveSkill : null;

    public bool IsAlive => currentHp > 0 && !isDead;

    [HideInInspector] public Vector3 originPos;
    private bool isTargetable = false;
    private bool isDead = false;

    private void Awake()
    {
        healthUI = GetComponentInChildren<UnitHealthUI>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        unitAnimator = GetComponent<UnitAnimator>();

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
        originalSpeed = data.speed;
        skillUseCount = 0;
        hitCount = 0;
        isDead = false;
        hasWarrant = false;
        hasUsedFirstDodge = false;

        // 패시브 전투 시작 호출
        if (Passive != null) Passive.OnBattleStart(this);

        if (unitAnimator == null) unitAnimator = GetComponent<UnitAnimator>();
        if (unitAnimator != null)
        {
            unitAnimator.SetupSprites(
                data.characterSprite,
                data.hitSprite,
                data.attackSprite,
                data.dodgeSprite
            );
        }

        if (healthUI != null) healthUI.UpdateHP(currentHp, maxHp);
    }

    // 영장 부여 (이속 1 고정)
    public void ApplyWarrant()
    {
        if (hasWarrant) return;
        hasWarrant = true;
        originalSpeed = currentSpeed;
        currentSpeed = 1;
        Debug.Log($"<color=#E11D48>[영장 집행] {UnitName}의 이속이 1로 고정되었습니다.</color>");
    }

    // 영장 해제
    public void RemoveWarrant()
    {
        if (!hasWarrant) return;
        hasWarrant = false;
        currentSpeed = originalSpeed;
        Debug.Log($"<color=#38BDF8>[영장 해제] {UnitName}의 영장이 제거되었습니다. (이속 {currentSpeed} 복구)</color>");
    }

    // canKill이 false면 HP가 1 밑으로 내려가지 않음
    public void TakeDamage(int damage, bool canKill = true)
    {
        if (isDead) return;

        int minHpLimit = canKill ? 0 : 1;
        currentHp = Mathf.Max(minHpLimit, currentHp - damage);
        totalDamageTaken += damage;
        hitCount++;

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

        if (currentHp <= 0 && !isDead)
        {
            Die();
        }
    }

    public void Die()
    {
        isDead = true;
        SetTargetable(false);
        Debug.Log($"<color=red>{UnitName} 사망</color>");

        if (healthUI != null) healthUI.gameObject.SetActive(false);

        Collider col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;

        StartCoroutine(FadeOutAndDisableRoutine());
    }

    private IEnumerator FadeOutAndDisableRoutine()
    {
        yield return new WaitForSeconds(0.35f);

        if (unitAnimator != null) unitAnimator.StopAllCoroutines();

        float fadeDuration = 0.5f;
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

        gameObject.SetActive(false);
    }

    public void ModifySpeed(int amount)
    {
        currentSpeed += amount;
        if (!hasWarrant) originalSpeed = currentSpeed;
        Debug.Log($"{UnitName}의 스피드 변경: {currentSpeed}");
    }

    public void Heal(int amount)
    {
        int prevHp = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        int actualHeal = currentHp - prevHp;

        totalHealing += actualHeal;

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