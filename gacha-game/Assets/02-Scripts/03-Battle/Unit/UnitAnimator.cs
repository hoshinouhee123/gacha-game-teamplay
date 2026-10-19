using System.Collections;
using UnityEngine;
// using Spine.Unity; // ★ 나중에 스파인 임포트 후 주석 해제

public class UnitAnimator : MonoBehaviour
{
    // private SkeletonAnimation skeletonAnim; // ★ 나중에 스파인 컴포넌트 연결용

    private void Awake()
    {
        // skeletonAnim = GetComponent<SkeletonAnimation>();
    }

    // 공격 애니메이션 실행 코루틴
    // onHit: 실제로 데미지가 들어가는 순간(타격 프레임)에 실행할 콜백
    public IEnumerator PlayAttackAnimation(string animName, System.Action onHit)
    {
        // ================================================================
        // 1. [임시] 스파인이 없을 때: 0.4초간 공격하는 척 (살짝 커졌다 줄어듦)
        // ================================================================
        Vector3 originScale = transform.localScale;

        // 칼 휘두르는 준비 동작 (0.15초)
        transform.localScale = originScale * 1.15f;
        yield return new WaitForSeconds(0.15f);

        // ★ 타격 시점! (적에게 데미지 전달)
        onHit?.Invoke();

        // 타격 후 딜레이 (0.25초)
        yield return new WaitForSeconds(0.25f);
        transform.localScale = originScale;

        /* ================================================================
        // 2. [추후 교체용] 스파인이 들어왔을 때 사용할 실제 코드:
        // ================================================================
        var track = skeletonAnim.AnimationState.SetAnimation(0, animName, false);
        
        // 타격 이벤트가 스파인 애니메이션 타임라인에 심어져 있다면:
        // skeletonAnim.AnimationState.Event += (entry, e) => { if(e.Data.Name == "Hit") onHit?.Invoke(); };

        // 혹은 애니메이션 중간 시점에 onHit 호출:
        yield return new WaitForSeconds(track.Animation.Duration * 0.4f);
        onHit?.Invoke();
        yield return new WaitForSeconds(track.Animation.Duration * 0.6f);
        */
    }

    // 피격 모션 (맞았을 때 붉은색 깜빡임)
    public IEnumerator PlayHitAnimation()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color orig = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = orig;
        }
    }
}