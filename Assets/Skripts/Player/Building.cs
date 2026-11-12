using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]// Луч идёт после поворота головы
public class Building : MonoBehaviour
{
    [SerializeField] Camera firstCamera;
    [SerializeField] Camera secondCamera;
    [SerializeField] Inventory hotBar;
    [SerializeField] Movement cameraSkript;

    public float placeMaxDistance = 4f;
    private int placeMask;

    void Awake()
    {
        if (hotBar == null)
            hotBar = FindFirstObjectByType<Inventory>();// Берём хотбар из сцены

        placeMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");// Луч проходит сквозь игрока
    }

    void LateUpdate()// Хотбар и камера уже обновились
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)// Правоя кнопка миши нажата
            PlaceBlock();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)// Левоя кнопка миши нажата
            BrokeBlock();
    }

    void PlaceBlock()
    {
        if (hotBar == null)
            return;

        GameObject currentBlock = hotBar.currentItem;
        if (currentBlock == null || !TryHit(out RaycastHit hit))
            return;

        Vector3 blockPosition = Vector3Int.RoundToInt(hit.point + hit.normal * 0.5f);// Клетка за гранью
        Vector3 halfSize = Vector3.one * 0.499f;// Чтобы соседние блоки могли касаться гранями
        if (Physics.CheckBox(blockPosition, halfSize, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;// В клетке уже есть игрок или другой блок

        Instantiate(currentBlock, blockPosition, Quaternion.identity);
    }

    void BrokeBlock()
    {
        if (TryHit(out RaycastHit hit) && hit.collider.gameObject.layer == LayerMask.NameToLayer("block"))
            Destroy(hit.collider.gameObject);// Убираем блок под прицелом
    }

    bool TryHit(out RaycastHit hit)
    {
        hit = default;
        if (cameraSkript == null)
            return false;

        Camera activeCamera = cameraSkript.isFirstFace ? firstCamera : secondCamera;
        if (activeCamera == null)
            return false;

        Physics.SyncTransforms();// Учитываем движение игрока в этом кадре
        Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 eyes = firstCamera != null ? firstCamera.transform.position : ray.origin;
        float rayLength = Vector3.Distance(ray.origin, eyes) + placeMaxDistance;
        float closest = float.PositiveInfinity;
        foreach (RaycastHit target in Physics.RaycastAll(ray, rayLength, placeMask, QueryTriggerInteraction.Ignore))
        {
            if (target.distance < closest && !target.collider.transform.IsChildOf(cameraSkript.transform))
            {
                hit = target;// Пропускаем и части игрока, которым не назначили слой Player
                closest = target.distance;
            }
        }

        return hit.collider != null && (hit.point - eyes).sqrMagnitude <= placeMaxDistance * placeMaxDistance;
    }
}
