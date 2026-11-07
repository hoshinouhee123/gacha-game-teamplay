
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("아이템 표시")]
    public Image itemIcon;
    public TMP_Text amountText;

    [Header("등급 표시 (선택)")]
    public TMP_Text starText;

    private void Awake()
    {
        ClearSlot();
    }

    public void SetItem(
        InventoryItemData item,
        Sprite sprite)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
        }

        if (amountText != null)
        {
            amountText.text = "x" + item.amount;
            amountText.gameObject.SetActive(true);
        }

        if (starText != null)
        {
            starText.text = item.star + "성";
            starText.gameObject.SetActive(true);
        }
    }

    public void ClearSlot()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }

        if (starText != null)
        {
            starText.text = "";
            starText.gameObject.SetActive(false);
        }
    }
}
