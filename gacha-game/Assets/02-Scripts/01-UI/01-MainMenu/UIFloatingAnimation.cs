
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIFloatingAnimation : MonoBehaviour
{
    [Header("움직임 거리")]
    [Min(0f)] public float moveX = 5f;
    [Min(0f)] public float moveY = 10f;

    [Header("움직임 주기 (초)")]
    [Min(0.1f)] public float durationX = 5f;
    [Min(0.1f)] public float durationY = 3.5f;

    [Header("기타 설정")]
    public bool ignoreTimeScale = true;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private float elapsedTime;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        originalPosition = rectTransform.anchoredPosition;
        elapsedTime = 0f;
    }

    private void LateUpdate()
    {
        float deltaTime = ignoreTimeScale
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        elapsedTime += deltaTime;

        // 좌우로 천천히 움직임
        float offsetX = Mathf.Sin(
            elapsedTime * Mathf.PI * 2f / Mathf.Max(0.1f, durationX)
        ) * moveX;

        // 위아래로 천천히 움직임
        float offsetY = Mathf.Sin(
            elapsedTime * Mathf.PI * 2f / Mathf.Max(0.1f, durationY)
        ) * moveY;

        rectTransform.anchoredPosition =
            originalPosition + new Vector2(offsetX, offsetY);
    }

    private void OnDisable()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = originalPosition;
    }
}
