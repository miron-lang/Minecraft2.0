using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class Building : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] Camera firstCamera;// Камера от первого лица
    [SerializeField] Camera secondCamera;// Камера от третьего лица
    [SerializeField] Movement cameraSkript;

    [Header("Hotbar")]
    [SerializeField] Inventory hotBar;// Хотбар игрока

    [Header("Building")]
    public float placeMaxDistance = 4f;// Дальность установки блока
    private int placeMask;

    void Awake()
    {
        if (hotBar == null)
            hotBar = FindFirstObjectByType<Inventory>();// Берём хотбар из сцены

        placeMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");// Луч проходит сквозь игрока
    }

    void LateUpdate()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)// Правоя кнопка миши нажата
            PlaceBlock();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)// Левоя кнопка миши нажата
            BrokeBlock();
    }

    void PlaceBlock()// Ставим выбранный блок
    {
        if (hotBar == null)
            return;

        GameObject currentBlock = hotBar.currentItem;
        if (currentBlock == null || !TryHit(out RaycastHit hit))
            return;

        Vector3 face = Vector3Int.RoundToInt(hit.normal);
        Vector3 blockPosition = hit.collider.gameObject.layer == LayerMask.NameToLayer("block")
            ? Vector3Int.RoundToInt(hit.collider.bounds.center) + face
            : Vector3Int.RoundToInt(hit.point + face * 0.5f);// Клетка за выбранной гранью
        Vector3 halfSize = Vector3.one * 0.499f;// Чуть меньше половины блока
        if (Physics.CheckBox(blockPosition, halfSize, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;// В клетке уже есть игрок или другой блок

        Vector3 forward = face.y == 0f ? Vector3.up : Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(forward, face);// Поворот блока по стороне
        Instantiate(currentBlock, blockPosition, rotation);
    }

    void BrokeBlock()// Ломаем блок под прицелом
    {
        if (TryHit(out RaycastHit hit) && hit.collider.gameObject.layer == LayerMask.NameToLayer("block"))
            Destroy(hit.collider.gameObject);
    }

    bool TryHit(out RaycastHit hit)// Куда смотрит игрок
    {
        hit = default;
        if (cameraSkript == null)
            return false;

        Camera activeCamera = cameraSkript.isFirstFace ? firstCamera : secondCamera;
        if (activeCamera == null)
            return false;

        Physics.SyncTransforms();
        Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 eyes = firstCamera != null ? firstCamera.transform.position : ray.origin;
        float rayLength = Vector3.Distance(ray.origin, eyes) + placeMaxDistance;
        float closest = float.PositiveInfinity;
        foreach (RaycastHit target in Physics.RaycastAll(ray, rayLength, placeMask, QueryTriggerInteraction.Ignore))
        {
            if (target.distance < closest && !target.collider.transform.IsChildOf(cameraSkript.transform))
            {
                hit = target;// Ближайший блок под прицелом
                closest = target.distance;
            }
        }

        return hit.collider != null && (hit.point - eyes).sqrMagnitude <= placeMaxDistance * placeMaxDistance;
    }
}
