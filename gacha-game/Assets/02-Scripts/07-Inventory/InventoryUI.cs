
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("기존 슬롯들이 들어있는 Group")]
    public Transform slotGroup;

    [Header("등급별 아이템 이미지")]
    public Sprite oneStarIcon;
    public Sprite twoStarIcon;
    public Sprite threeStarIcon;

    private readonly List<InventorySlotUI> slots =
        new List<InventorySlotUI>();

    private void Awake()
    {
        InitializeSlots();
    }

    private void OnEnable()
    {
        InventoryStorage.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        InventoryStorage.OnInventoryChanged -= Refresh;
    }

    private void InitializeSlots()
    {
        slots.Clear();

        if (slotGroup == null)
        {
            Debug.LogError("Slot Group이 연결되지 않았습니다!");
            return;
        }

        // Group의 기존 Image 슬롯들을 사용
        foreach (Transform child in slotGroup)
        {
            Image background = child.GetComponent<Image>();

            if (background == null)
                continue;

            InventorySlotUI slot =
                child.GetComponent<InventorySlotUI>();

            if (slot == null)
                slot = child.gameObject.AddComponent<InventorySlotUI>();

            // 아이콘이 없으면 자동 생성
            if (slot.itemIcon == null)
            {
                GameObject iconObj = new GameObject(
                    "ItemIcon",
                    typeof(RectTransform),
                    typeof(Image)
                );

                iconObj.transform.SetParent(child, false);

                RectTransform rect =
                    iconObj.GetComponent<RectTransform>();

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8, 8);
                rect.offsetMax = new Vector2(-8, -8);

                slot.itemIcon =
                    iconObj.GetComponent<Image>();

                slot.itemIcon.raycastTarget = false;
            }

            // 수량 텍스트가 없으면 자동 생성
            if (slot.amountText == null)
            {
                GameObject textObj = new GameObject(
                    "AmountText",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI)
                );

                textObj.transform.SetParent(child, false);

                RectTransform rect =
                    textObj.GetComponent<RectTransform>();

                rect.anchorMin = new Vector2(0.4f, 0f);
                rect.anchorMax = new Vector2(1f, 0.35f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                TMP_Text text =
                    textObj.GetComponent<TMP_Text>();

                text.fontSize = 20;
                text.alignment =
                    TextAlignmentOptions.BottomRight;

                text.raycastTarget = false;

                slot.amountText = text;
            }

            slot.ClearSlot();
            slots.Add(slot);
        }

        Debug.Log(
            "인벤토리 슬롯 " + slots.Count + "개 연결 완료"
        );
    }

    public void Refresh()
    {
        if (slots.Count == 0)
            InitializeSlots();

        if (slots.Count == 0)
            return;

        List<InventoryItemData> items =
            InventoryStorage.GetItems();

        // 모든 슬롯 비우기
        foreach (InventorySlotUI slot in slots)
            slot.ClearSlot();

        int index = 0;

        foreach (InventoryItemData item in items)
        {
            if (item.amount <= 0)
                continue;

            if (index >= slots.Count)
            {
                Debug.LogWarning(
                    "인벤토리 슬롯이 부족합니다!"
                );
                break;
            }

            Sprite icon = GetIcon(item.star);

            slots[index].SetItem(item, icon);
            index++;
        }
    }

    private Sprite GetIcon(int star)
    {
        switch (star)
        {
            case 1: return oneStarIcon;
            case 2: return twoStarIcon;
            case 3: return threeStarIcon;
            default: return null;
        }
    }
}
