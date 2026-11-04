using System.Collections;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private Sprite hitSprite;
    private Sprite attackSprite;
    private Sprite dodgeSprite;

    [Header("피격 연출 설정")]
    [SerializeField] private float hitDuration = 1f;

    [Header("타격 연출 설정")]
    [SerializeField] private float takeDamageDuration = 0.5f;

    [Header("회피 연출 설정")]
    [SerializeField] private float dodgeDuration = 0.6f;

    // 피격과 회피가 동시에 실행되어 스프라이트가 꼬이는 것을 방지
    private Coroutine reactionCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // SO에서 기본/피격/공격/회피 스프라이트를 전달받음
    public void SetupSprites(
        Sprite defSprite,
        Sprite hSprite,
        Sprite aSprite,
        Sprite dSprite = null)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        defaultSprite = defSprite;
        hitSprite = hSprite;
        attackSprite = aSprite;
        dodgeSprite = dSprite;

        if (defaultSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = defaultSprite;
    }

    // 피격 모션 실행
    public void PlayHit()
    {
        if (hitSprite == null || spriteRenderer == null)
            return;

        PlayReaction(hitSprite, hitDuration);
    }

    // 회피 모션 실행
    public void PlayDodge()
    {
        if (dodgeSprite == null || spriteRenderer == null)
        {
            Debug.LogWarning($"[{name}] 회피 스프라이트가 등록되지 않았습니다.");
            return;
        }

        PlayReaction(dodgeSprite, dodgeDuration);
    }

    private void PlayReaction(Sprite reactionSprite, float duration)
    {
        if (reactionCoroutine != null)
            StopCoroutine(reactionCoroutine);

        reactionCoroutine = StartCoroutine(PlayReactionRoutine(reactionSprite, duration));
    }

    private IEnumerator PlayReactionRoutine(Sprite reactionSprite, float duration)
    {
        spriteRenderer.sprite = reactionSprite;
        yield return new WaitForSeconds(duration);

        if (spriteRenderer != null)
            spriteRenderer.sprite = defaultSprite;

        reactionCoroutine = null;
    }

    // 공격 애니메이션 코루틴
    public IEnumerator PlayAttackAnimation(string animName, System.Action onHit)
    {
        Vector3 originScale = transform.localScale;

        // 실제 타격 시점에 전달된 콜백 실행
        onHit?.Invoke();

        if (spriteRenderer != null && attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
            yield return new WaitForSeconds(takeDamageDuration);
            spriteRenderer.sprite = defaultSprite;
        }

        yield return new WaitForSeconds(0.3f);
        transform.localScale = originScale;
    }
}
