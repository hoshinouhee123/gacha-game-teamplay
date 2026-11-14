
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

// 회전 방향
public enum RotationDirection
{
    Clockwise,        // 우회전
    CounterClockwise  // 좌회전
}

// 이미지별 회전 애니메이션 설정
[System.Serializable]
public class RotationImageSettings
{
    [Header("회전 이미지")]
    public RectTransform image;

    [Header("첫 번째 회전")]
    public RotationDirection firstDirection =
        RotationDirection.CounterClockwise;

    [Min(0f)]
    public float rotationAngle = 45f;

    [Min(0.01f)]
    public float firstDuration = 3f;

    public Ease firstEase = Ease.InOutSine;

    [Header("방향 전환 딜레이")]
    [Min(0f)]
    public float directionDelay = 0.5f;

    [Header("두 번째 회전 (반대 방향)")]
    [Min(0.01f)]
    public float secondDuration = 0.5f;

    public Ease secondEase = Ease.OutQuad;

    [Header("반복 설정")]
    [Min(0f)]
    public float startDelay = 0f;

    [Min(0f)]
    public float loopDelay = 1f;
}

public class MainMenuUIAnimationManager : MonoBehaviour
{
    [Header("상하 이동 이미지")]
    public RectTransform lightOrangeImage;

    public float moveDistance = 80f;
    public float moveDuration = 2f;

    [Header("스케일 변화 이미지")]
    public RectTransform scaleImage;

    public float scaleDuration = 1f;

    [Header("회전 이미지 목록")]
    public RotationImageSettings[] rotationImages;

    // 초기 상태
    private Vector2 originalMovePos;
    private Vector3 originalScale;
    private Quaternion[] originalRotations;

    // 트윈 관리
    private Tween moveTween;
    private Tween scaleTween;

    private readonly List<Sequence> rotationTweens =
        new List<Sequence>();

    void Awake()
    {
        if (lightOrangeImage != null)
        {
            originalMovePos =
                lightOrangeImage.anchoredPosition;
        }

        if (scaleImage != null)
        {
            originalScale = scaleImage.localScale;
        }

        // 회전 이미지의 초기 각도 저장
        originalRotations =
            new Quaternion[rotationImages.Length];

        for (int i = 0; i < rotationImages.Length; i++)
        {
            if (rotationImages[i].image != null)
            {
                originalRotations[i] =
                    rotationImages[i].image.localRotation;
            }
        }
    }

    void OnEnable()
    {
        StartMoveAnimation();
        StartScaleAnimation();
        StartRotationAnimations();
    }

    // ===========================
    // 상하 이동 애니메이션
    // ===========================
    void StartMoveAnimation()
    {
        if (lightOrangeImage == null) return;

        lightOrangeImage.anchoredPosition =
            originalMovePos +
            Vector2.down * moveDistance;

        moveTween = lightOrangeImage
            .DOAnchorPosY(
                originalMovePos.y + moveDistance,
                Mathf.Max(0.01f, moveDuration)
            )
            .SetEase(Ease.InOutExpo)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ===========================
    // 스케일 애니메이션
    // ===========================
    void StartScaleAnimation()
    {
        if (scaleImage == null) return;

        scaleImage.localScale = new Vector3(
            originalScale.x,
            0f,
            originalScale.z
        );

        scaleTween = scaleImage
            .DOScaleY(1f, Mathf.Max(0.01f, scaleDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ===========================
    // 회전 애니메이션
    // ===========================
    void StartRotationAnimations()
    {
        for (int i = 0; i < rotationImages.Length; i++)
        {
            RotationImageSettings settings =
                rotationImages[i];

            if (settings.image == null) continue;

            // 초기 회전 상태 복구
            settings.image.localRotation =
                originalRotations[i];

            // 회전 방향 결정
            float direction =
                settings.firstDirection ==
                RotationDirection.CounterClockwise
                ? 1f : -1f;

            float angle =
                Mathf.Abs(settings.rotationAngle) * direction;

            // 이미지별 시퀀스 생성
            Sequence sequence = DOTween.Sequence();

            // 첫 번째 회전
            sequence.Append(
                settings.image.DOLocalRotate(
                    new Vector3(0f, 0f, angle),
                    Mathf.Max(0.01f, settings.firstDuration),
                    RotateMode.LocalAxisAdd
                ).SetEase(settings.firstEase)
            );

            // 방향 전환 대기
            sequence.AppendInterval(
                Mathf.Max(0f, settings.directionDelay)
            );

            // 반대 방향으로 회전
            sequence.Append(
                settings.image.DOLocalRotate(
                    new Vector3(0f, 0f, -angle),
                    Mathf.Max(0.01f, settings.secondDuration),
                    RotateMode.LocalAxisAdd
                ).SetEase(settings.secondEase)
            );

            // 반복 전 대기
            sequence.AppendInterval(
                Mathf.Max(0f, settings.loopDelay)
            );

            // 처음 시작할 때만 대기
            sequence.SetDelay(
                Mathf.Max(0f, settings.startDelay),
                false
            );

            // 무한 반복
            sequence.SetLoops(-1, LoopType.Restart);

            // 리스트에 저장
            rotationTweens.Add(sequence);
        }
    }

    // ===========================
    // 트윈 정리
    // ===========================
    void OnDisable()
    {
        moveTween?.Kill();
        scaleTween?.Kill();

        foreach (Sequence sequence in rotationTweens)
        {
            sequence?.Kill();
        }

        rotationTweens.Clear();

        // 초기 상태 복구
        if (lightOrangeImage != null)
        {
            lightOrangeImage.anchoredPosition =
                originalMovePos;
        }

        if (scaleImage != null)
        {
            scaleImage.localScale = originalScale;
        }

        if (originalRotations != null)
        {
            for (int i = 0; i < rotationImages.Length; i++)
            {
                if (rotationImages[i].image != null)
                {
                    rotationImages[i].image.localRotation =
                        originalRotations[i];
                }
            }
        }
    }
}
