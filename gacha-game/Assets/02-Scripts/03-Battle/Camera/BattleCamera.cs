using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BattleCamera : MonoBehaviour
{
    public static BattleCamera Instance;

    private Camera cam;
    private Vector3 defaultPos;
    private Quaternion defaultRot;
    private float defaultDistance;
    private float defaultSizeOrFov;

    [Header("3D 추적 & 줌 설정")]
    [Tooltip("캐릭터가 대시할 때 카메라가 따라오는 부드러움 (높을수록 무게감 있게 뒤따라옴)")]
    public float followSmoothTime = 0.22f;

    [Tooltip("기본 줌인 배율")]
    public float defaultZoomRatio = 0.78f;

    [Header("리버스 1999 스타일 3D 틸트 각도")]
    public float cinematicYawTilt = 6.0f;    // Y축 회전 각도 (약 6도 틀어서 3D 깊이감 생성)
    public float cinematicPitchTilt = -2.0f; // X축 살짝 낮춤

    private Transform targetTransform = null;
    private float currentZoomRatio = 1.0f;
    private Vector3 currentVelocity = Vector3.zero;
    private bool isResetting = false;
    private Vector3 shakeOffset = Vector3.zero;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();

        defaultPos = transform.position;
        defaultRot = transform.rotation;
        defaultSizeOrFov = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;

        // 카메라가 바닥(Y=0)을 바라보는 기본 3D 거리 계산
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        Ray ray = new Ray(transform.position, transform.forward);
        if (ground.Raycast(ray, out float enter)) defaultDistance = enter;
        else defaultDistance = 10f;
    }

    private void LateUpdate()
    {
        if (targetTransform != null && !isResetting)
        {
            float targetDist = defaultDistance * currentZoomRatio;
            Vector3 targetCamPos = targetTransform.position - (transform.forward * targetDist);

            // 부드러운 3D 감속 추적 (관성 효과)
            transform.position = Vector3.SmoothDamp(transform.position, targetCamPos, ref currentVelocity, followSmoothTime);
        }

        if (shakeOffset != Vector3.zero)
        {
            transform.position += shakeOffset;
        }
    }

    // 캐릭터 포커스 및 3D 줌인 시작
    public void StartFollowing(Transform target, float zoomRatio = -1f)
    {
        isResetting = false;
        targetTransform = target;
        currentZoomRatio = (zoomRatio > 0f) ? zoomRatio : defaultZoomRatio;

        StopAllCoroutines();
        // 0.4초 동안 부드럽게 줌인
        StartCoroutine(AdjustFovRoutine(defaultSizeOrFov * currentZoomRatio, 0.4f));
    }

    // 전체 전장 시점으로 부드럽게 복구
    public void ResetCamera(float duration = 0.45f)
    {
        targetTransform = null;
        isResetting = true;
        StopAllCoroutines();
        StartCoroutine(ResetCameraRoutine(duration));
    }

    private IEnumerator ResetCameraRoutine(float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation; // 시작 회전 각도 기억
        float startVal = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-In-Out Cubic 곡선 (부드러운 출발 및 정지)
            float ease = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            transform.position = Vector3.Lerp(startPos, defaultPos, ease);
            transform.rotation = Quaternion.Slerp(startRot, defaultRot, ease);      // 각도를 원래 defaultRot로 부드럽게 복구
            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(startVal, defaultSizeOrFov, ease);
            else cam.fieldOfView = Mathf.Lerp(startVal, defaultSizeOrFov, ease);

            yield return null;
        }

        transform.position = defaultPos;
        transform.rotation = defaultRot; // 오차 방지용으로 원래 각도 고정 
        isResetting = false;
    }

    private IEnumerator AdjustFovRoutine(float targetVal, float duration)
    {
        float startVal = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(startVal, targetVal, ease);
            else cam.fieldOfView = Mathf.Lerp(startVal, targetVal, ease);

            yield return null;
        }
    }

    public void Shake(float intensity = 0.12f, float duration = 0.15f)
    {
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            shakeOffset = Random.insideUnitSphere * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }





    // 3D 틸트 줌인 시작
    public void StartCinematicFocus(Transform target, float zoomRatio = 0.72f, float duration = 0.4f)
    {
        isResetting = false;
        targetTransform = null; // 고정 앵글 줌인이므로 동적 트래킹은 잠시 해제
        StopAllCoroutines();
        StartCoroutine(CinematicFocusRoutine(target.position, zoomRatio, duration));
    }

    private IEnumerator CinematicFocusRoutine(Vector3 targetWorldPos, float zoomRatio, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startVal = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;

        // 1. 목표 회전 각도: 기본 각도에서 살짝 비틀기
        Quaternion targetRot = defaultRot * Quaternion.Euler(cinematicPitchTilt, cinematicYawTilt, 0f);

        // 2. 목표 위치: 각도를 비틀면서 시전자 앞으로 파고드는 3D 좌표
        float targetDist = defaultDistance * zoomRatio;
        Vector3 targetPos = targetWorldPos - (targetRot * Vector3.forward * targetDist);

        targetPos.x += 5f; // X축으로 살짝 이동 (좌우 시점 조정)

        float targetFov = defaultSizeOrFov * zoomRatio;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-Out Cubic
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            transform.position = Vector3.Lerp(startPos, targetPos, ease);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, ease);

            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(startVal, targetFov, ease);
            else cam.fieldOfView = Mathf.Lerp(startVal, targetFov, ease);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }
}