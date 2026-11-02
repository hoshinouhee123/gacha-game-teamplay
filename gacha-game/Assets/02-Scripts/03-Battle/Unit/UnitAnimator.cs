using System.Collections;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private Sprite hitSprite;
    private Sprite attackSprite;
    private Unit unit;

    [Header("피격 연출 설정")]
    [SerializeField] private float hitDuration = 1f;
    private Coroutine hitCoroutine;

    [Header("타격 연출 설정")]
    [SerializeField] private float takeDamageDuration = 0.5f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // SO로부터 기본/피격 스프라이트를 주입받는 함수
    public void SetupSprites(Sprite defSprite, Sprite hSprite, Sprite aSprite)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        defaultSprite = defSprite;
        hitSprite = hSprite;
        attackSprite = aSprite;

        if (defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    // 피격 모션 실행
    public void PlayHit()
    {
        // 피격 스프라이트가 등록되어 있지 않으면 무시
        if (hitSprite == null) return;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }
        hitCoroutine = StartCoroutine(PlayHitAnimation());
    }

    private IEnumerator PlayHitAnimation()
    {
        if (spriteRenderer != null && hitSprite != null)
        {
            spriteRenderer.sprite = hitSprite;
            yield return new WaitForSeconds(hitDuration);

            spriteRenderer.sprite = defaultSprite;
        }
        hitCoroutine = null;
    }


    // 공격 애니메이션 코루틴
    public IEnumerator PlayAttackAnimation(string animName, System.Action onHit)
    {
        Vector3 originScale = transform.localScale;

        // transform.localScale = originScale * 1.15f;      //공격시 이미지 스케일 키우는 코드
        // yield return new WaitForSeconds(0.2f);


        

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