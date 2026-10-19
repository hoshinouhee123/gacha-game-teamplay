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
        if (damageTextPrefab == null) return;
        Transform spawn = textSpawnPoint != null ? textSpawnPoint : transform;
        GameObject obj = Instantiate(damageTextPrefab, spawn.position, Quaternion.identity, transform);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Setup(damage, new Color(1f, 0.85f, 0.2f));
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