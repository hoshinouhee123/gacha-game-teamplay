using UnityEngine;
using UnityEngine.UI;

public class UnitHealthUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider hpSlider;
    public GameObject damageTextPrefab;
    public Transform textSpawnPoint;

    [Header("머리 위 여백 (기본 0.3)")]
    public float headMargin = 0.3f; // 머리 꼭대기에서 얼마나 더 띄울지

    [Header("데미지 폰트 색상")]
    public Color damageColor = Color.red;       // 기본 빨간색 (인스펙터에서 자유롭게 변경 가능)
    public Color healColor = Color.green;       // 기본 초록색

    private void Start()
    {
        // 게임 시작 시 자신의 위치를 캐릭터 머리 위로 자동 배치
        AutoFitHeadPosition();
    }

    // 에디터에서 부모를 바꾸거나 스프라이트가 바뀔 때도 자동 정렬
    private void OnValidate()
    {
        AutoFitHeadPosition();
    }

    private void LateUpdate()
    {
        // 2.5D 카메라 빌보드 (항상 카메라 정면 바라보기)
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    // 캐릭터 스프라이트의 머리 꼭대기를 찾아 자동으로 올라가는 함수
    public void AutoFitHeadPosition()
    {
        SpriteRenderer sr = GetComponentInParent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // 스프라이트의 가장 윗부분(bounds.max.y) 좌표를 찾아서 배치
            float topY = sr.bounds.max.y + headMargin;
            transform.position = new Vector3(transform.parent.position.x, topY, transform.parent.position.z);
        }
    }

    public void UpdateHP(int currentHp, int maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }
    }

    public void ShowDamage(int damage)
    {
        if (damageTextPrefab == null || Camera.main == null) return;

        // 머리 위 캔버스(WorldSpace)는 거르고, 진짜 모니터 화면 캔버스(ScreenSpaceOverlay)만 찾기
        Canvas mainScreenCanvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                mainScreenCanvas = c;
                break;
            }
        }

        if (mainScreenCanvas == null)
        {
            Debug.LogError("<color=red>[오류] 씬에 Screen Space - Overlay 모드의 메인 Canvas가 없습니다!</color>");
            return;
        }

        // 1. 캐릭터 머리 위 3D 월드 좌표를 2D 모니터 화면 픽셀 좌표로 변환
        Transform spawn = textSpawnPoint != null ? textSpawnPoint : transform;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(spawn.position);

        // 만약 카메라 뒤에 있으면 띄우지 않음 (Z축 검사)
        if (screenPos.z < 0) return;

        // 2. 화면 캔버스의 자식으로 생성
        GameObject obj = Instantiate(damageTextPrefab, mainScreenCanvas.transform);

        // 3. 위치, 크기, 각도 초기화 (우주로 날아가는 것 방지)
        obj.transform.position = screenPos;
        obj.transform.localScale = Vector3.one;
        obj.transform.rotation = Quaternion.identity;

        // 4. 데미지 적용
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Setup(damage, damageColor);
    }

    public void ShowHeal(int healAmount)
    {
        if (damageTextPrefab == null) return;
        Transform spawn = textSpawnPoint != null ? textSpawnPoint : transform;
        GameObject obj = Instantiate(damageTextPrefab, spawn.position, Quaternion.identity, transform);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Setup(healAmount, new Color(0.2f, 1f, 0.3f));
    }
}