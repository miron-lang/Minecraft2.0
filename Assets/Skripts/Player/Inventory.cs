using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    public GameObject[] slots;// Слоты инвентаря
    private GameObject currentSlot;// Ячейка под рамкой
    public GameObject currentItem
    {
        get
        {
            if (currentSlot != null && currentSlot.TryGetComponent(out HotBarSlot slot))
                return slot.myGameobject;// Берём блок из выбранной ячейки
            return null;
        }
    }

    [Range(0, 8)]
    public int currentSlotIndex = 0;// Номер слота с нуля
    private RectTransform selectionFrame;// Рамка выбранного слота

    private void OnValidate()
    {
        SyncCurrentSlot();
    }

    private void OnEnable()
    {
        MakeRoomForFrame();
        SyncCurrentSlot();
        UpdateHighlight();
    }

    private void MakeRoomForFrame()
    {
        var bar = transform as RectTransform;
        if (bar == null || bar.anchorMin.y != 0f || bar.anchorMax.y != 0f)
            return;

        Vector2 position = bar.anchoredPosition;
        float minY = SlotSelectionFrame.BorderWidth - bar.rect.yMin;
        if (position.y < minY)
            bar.anchoredPosition = new Vector2(position.x, minY);// Чтобы рамку было видно снизу
    }

    private void Update()
    {
        if (slots != null && slots.Length > 0)
            currentSlotIndex = ReadSlotIndex();

        SyncCurrentSlot();
        UpdateHighlight();
    }

    private void SyncCurrentSlot()
    {
        if (slots == null || slots.Length == 0)
        {
            currentSlotIndex = 0;
            currentSlot = null;
            return;
        }

        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, slots.Length - 1);
        currentSlot = slots[currentSlotIndex];// Берём слот по его номеру
    }

    private int ReadSlotIndex()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            for (int i = 0; i < Mathf.Min(9, slots.Length); i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame ||
                    keyboard[Key.Numpad1 + i].wasPressedThisFrame)// Если нажата цифра от 1 до 9
                    return i;
            }
        }

        float scroll = Mouse.current == null ? 0f : Mouse.current.scroll.ReadValue().y;// Колёсико мышки
        int index = Mathf.Clamp(currentSlotIndex, 0, slots.Length - 1);
        if (scroll == 0f)
            return index;

        int step = scroll > 0f ? -1 : 1;// Колёсико вверх на слот назад, вниз на слот вперёд
        return (index + step + slots.Length) % slots.Length;// С последнего слота на первый и обратно
    }

    private void UpdateHighlight()
    {
        if (currentSlot == null)
        {
            HideFrame();
            return;
        }

        if (selectionFrame == null)
        {
            var frame = new GameObject("Selection Frame", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(SlotSelectionFrame));
            frame.GetComponent<SlotSelectionFrame>().raycastTarget = false;
            selectionFrame = (RectTransform)frame.transform;
        }
        if (selectionFrame.parent == currentSlot.transform && selectionFrame.gameObject.activeSelf)
            return;

        selectionFrame.gameObject.layer = currentSlot.layer;
        selectionFrame.SetParent(currentSlot.transform, false);// Переносим рамку на выбранный слот
        selectionFrame.anchorMin = Vector2.zero;
        selectionFrame.anchorMax = Vector2.one;
        Vector2 border = Vector2.one * SlotSelectionFrame.BorderWidth;
        selectionFrame.offsetMin = -border;
        selectionFrame.offsetMax = border;// Рамка снаружи слота

        var parentCanvas = currentSlot.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            var frameCanvas = selectionFrame.GetComponent<Canvas>();
            frameCanvas.overrideSorting = true;
            frameCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            frameCanvas.sortingOrder = parentCanvas.sortingOrder + 1;// Поверх соседних слотов
        }
        selectionFrame.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        HideFrame();
    }

    private void HideFrame()
    {
        if (selectionFrame != null && selectionFrame.gameObject.activeSelf)
            selectionFrame.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (selectionFrame != null)
            Destroy(selectionFrame.gameObject);
    }
}
