
using UnityEngine;
using DG.Tweening;

public class MainMenuUIAnimationManager : MonoBehaviour
{
    [Header("위아래로 움직이는 이미지")]
    public RectTransform lightOrangeImage;

    [Header("스케일이 변하는 이미지")]
    public RectTransform whiteBarImage;

    [Header("애니메이션 설정")]
    public float moveDistance = 80f;
    public float moveDuration = 2f;
    public float scaleDuration = 1f;

    private Vector2 lightOrangeImagePos;

    private Tween moveTween;
    private Tween scaleTween;

    void Awake()
    {
        lightOrangeImagePos = lightOrangeImage.anchoredPosition;
    }

    void OnEnable()
    {
        // 1. 이미지 상하 이동 애니메이션
        lightOrangeImage.anchoredPosition =
            lightOrangeImagePos + Vector2.down * moveDistance;

        moveTween = lightOrangeImage
            .DOAnchorPosY(lightOrangeImagePos.y + moveDistance, moveDuration)
            .SetEase(Ease.InOutExpo)
            .SetLoops(-1, LoopType.Yoyo);

        // 2. 이미지 Y 스케일 애니메이션
        Vector3 originalScale = whiteBarImage.localScale;

        whiteBarImage.localScale = new Vector3(
            originalScale.x,
            0f,
            originalScale.z
        );

        scaleTween = whiteBarImage
            .DOScaleY(1f, scaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnDisable()
    {
        // 트윈 정리
        moveTween?.Kill();
        scaleTween?.Kill();
    }
}
