using UnityEngine;
using UnityEngine.UI;

public class HotBarSlot : MonoBehaviour
{
    public GameObject myGameobject;// Какой блок лежит в слоте
    public Sprite mySprite;// Картинка этого блока
    [SerializeField] Image itemImage;// Картинка Item внутри слота

    void Awake()
    {
        if (itemImage == null || itemImage.transform == transform)
        {
            Transform item = transform.Find("Item");
            if (item != null && item.TryGetComponent(out Image image))
                itemImage = image;// Картинка лежит внутри ячейки
        }
    }

    void Update()
    {
        if (itemImage == null)
            return;

        itemImage.sprite = mySprite;
        itemImage.enabled = mySprite != null;// Если картинки нет, прячем её
    }
}
