using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    [Header("Rigid body")]
    private Rigidbody rb;// Физика игрока
    private BoxCollider bodyBox;// Хитбокс игрока
    private int bodyId;
    private int blockLayer;
    private Collider[] nearbyBlocks = new Collider[96];
    private readonly Dictionary<int, Vector3Int> blockPositions = new Dictionary<int, Vector3Int>();
    private readonly HashSet<Vector3Int> filledCells = new HashSet<Vector3Int>();// Клетки с целыми блоками

    [Header("Movement settings")]
    [SerializeField] float walkingSpeed = 4.3f;// Скорость ходьбы
    [SerializeField] float runningSpeed = 5.6f;// Скорость бега
    [SerializeField] float acceleration = 60f;// Разгон игрока
    [Range(0f, 1f)] public float backwardsMultiplayer = 0.8f;// Наскоко медлене ходба/бег назад
    public bool isRunning;
    private Vector2 moveInput;
    [SerializeField, HideInInspector] bool balanceUpdated;

    [Header("Jump settings")]
    [SerializeField] float jumpForce = 5f;// Сила прыжка
    [SerializeField] Transform groundCheck;// Точка под ногами
    [SerializeField] LayerMask groundLayer;// Слой земли
    private BoxCollider groundBox;
    private float jumpBuffer;

    [Header("Crouch settings")]
    [SerializeField] float crouchingSpeed = 1.4f;// Скорость на Shift
    private bool crouchHeld;
    private bool isCrouching;
    private Vector3 standingSize;
    private Vector3 standingCenter;
    private const float CrouchingHeight = 1.5f;// Высота сидя
    private float crouchDistance;
    private const float EdgeInset = 0.02f;// Отступ от края блока
    private Vector3 sneakProbeHalf;
    private static readonly Quaternion DiagonalProbe = Quaternion.Euler(0f, 45f, 0f);
    private int solidLayers;

    [Header("Camera")]
    [SerializeField] Transform head;// Голова игрока
    [SerializeField] float mouseSensitivity = 100f;// Чувствительность мышки
    [SerializeField] GameObject firstFace;// Вид от первого лица
    [SerializeField] GameObject thirdFace;// Вид от третьего лица
    public float cameraPitch;// Наклон камеры
    public bool isFirstFace = true;
    private float yaw;
    private Transform cameraPivot;
    private Vector3 standingPivot;
    private float crouchDrop;
    private Camera thirdCamera;
    private Vector3 thirdCameraHome;
    private float thirdCameraDistance;
    private int cameraMask;

    [Header("Animations")]
    [SerializeField] Animator anim;

    void OnValidate()
    {
        ApplySavedBalance();
    }

    void ApplySavedBalance()// Обновляем старые скорости и прыжок
    {
        if (balanceUpdated)
            return;

        if (walkingSpeed == 7f) walkingSpeed = 4.3f;
        if (runningSpeed == 12f) runningSpeed = 5.6f;
        if (backwardsMultiplayer == 0.5f) backwardsMultiplayer = 0.8f;
        if (jumpForce == 6f) jumpForce = 5f;
        balanceUpdated = true;
    }

    void Awake()
    {
        ApplySavedBalance();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.constraints |= RigidbodyConstraints.FreezeRotation;// Чтобы игрок не заваливался
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bodyBox = GetComponent<BoxCollider>();
        bodyId = bodyBox.GetInstanceID();
        bodyBox.hasModifiableContacts = true;
        blockLayer = LayerMask.GetMask("block");
        standingSize = bodyBox.size;
        standingCenter = bodyBox.center;
        float radius = Mathf.Min(standingSize.x * 0.5f - Mathf.Abs(standingCenter.x),
            standingSize.z * 0.5f - Mathf.Abs(standingCenter.z));
        float extent = Mathf.Max(0.001f, radius - EdgeInset) / Mathf.Sqrt(2f);
        sneakProbeHalf = new Vector3(extent, 0.002f, extent);// Проверка края на Shift
        crouchDistance = Mathf.Max(0f, standingSize.y - CrouchingHeight);
        groundBox = groundCheck.GetComponent<BoxCollider>();
        groundBox.isTrigger = true;// Триггер под ногами
        solidLayers = Physics.AllLayers & ~(1 << gameObject.layer);
        yaw = rb.rotation.eulerAngles.y;
        if (head == null)
            head = transform.Find("Torso/Head");

        cameraPivot = new GameObject("Camera Pivot").transform;// Точка поворота камер
        cameraPivot.SetParent(transform, false);
        standingPivot = transform.InverseTransformPoint(firstFace.transform.parent.position);
        cameraPivot.localPosition = standingPivot;
        firstFace.transform.SetParent(cameraPivot, true);
        thirdFace.transform.SetParent(cameraPivot, true);
        thirdCamera = thirdFace.GetComponent<Camera>();
        thirdCameraHome = thirdFace.transform.localPosition;
        thirdCameraDistance = Vector3.Distance(firstFace.transform.position, thirdFace.transform.position);
        cameraMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        Physics.ContactModifyEvent += CheckBlockContacts;
        Physics.ContactModifyEventCCD += CheckFastBlockContacts;
    }

    void OnDisable()
    {
        Physics.ContactModifyEvent -= CheckBlockContacts;
        Physics.ContactModifyEventCCD -= CheckFastBlockContacts;
    }

    void FindNearbyBlocks()// Целые блоки вокруг игрока
    {
        Bounds area = bodyBox.bounds;
        area.Encapsulate(new Bounds(area.center + rb.linearVelocity * Time.fixedDeltaTime, area.size));
        area.Expand(2.2f);// По одному соседнему блоку с каждой стороны
        int count = Physics.OverlapBoxNonAlloc(area.center, area.extents, nearbyBlocks, Quaternion.identity, blockLayer, QueryTriggerInteraction.Ignore);
        while (count == nearbyBlocks.Length)
        {
            System.Array.Resize(ref nearbyBlocks, count * 2);
            count = Physics.OverlapBoxNonAlloc(area.center, area.extents, nearbyBlocks, Quaternion.identity, blockLayer, QueryTriggerInteraction.Ignore);
        }

        blockPositions.Clear();
        filledCells.Clear();
        for (int i = 0; i < count; i++)
        {
            if (!(nearbyBlocks[i] is BoxCollider box) || box.attachedRigidbody != null)
                continue;
            Bounds bounds = box.bounds;
            Vector3Int cell = Vector3Int.RoundToInt(bounds.center);
            if ((bounds.size - Vector3.one).sqrMagnitude > 0.000001f || (bounds.center - (Vector3)cell).sqrMagnitude > 0.000001f)
                continue;
            blockPositions[box.GetInstanceID()] = cell;
            filledCells.Add(cell);
        }
    }

    void CheckBlockContacts(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        SkipInsideFaces(pairs, 1f);
    }

    void CheckFastBlockContacts(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        SkipInsideFaces(pairs, -1f);// При быстром столкновении нормаль обратная
    }

    void SkipInsideFaces(NativeArray<ModifiableContactPair> pairs, float sign)// Пропускаем стыки двух блоков
    {
        foreach (var pair in pairs)
        {
            bool playerFirst = pair.colliderInstanceID == bodyId;
            if (!playerFirst && pair.otherColliderInstanceID != bodyId)
                continue;
            int blockId = playerFirst ? pair.otherColliderInstanceID : pair.colliderInstanceID;
            if (!blockPositions.TryGetValue(blockId, out Vector3Int cell))
                continue;
            float direction = playerFirst ? sign : -sign;
            for (int i = 0; i < pair.contactCount; i++)
            {
                Vector3 normal = pair.GetNormal(i) * direction;
                if (normal.x != 0f && filledCells.Contains(cell + (normal.x > 0f ? Vector3Int.right : Vector3Int.left)))
                    normal.x = 0f;
                if (normal.y != 0f && filledCells.Contains(cell + (normal.y > 0f ? Vector3Int.up : Vector3Int.down)))
                    normal.y = 0f;
                if (normal.z != 0f && filledCells.Contains(cell + (normal.z > 0f ? Vector3Int.forward : Vector3Int.back)))
                    normal.z = 0f;
                if (normal.sqrMagnitude < 0.0001f)
                    pair.IgnoreContact(i);
                else if (normal.sqrMagnitude < 0.99999f)
                    pair.SetNormal(i, normal.normalized * direction);// Вдоль стены игрок скользит вниз
            }
        }
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);// Та же скорость по диагонали
        var keyboard = Keyboard.current;
        crouchHeld = Input.GetKey(KeyCode.LeftShift) || (keyboard != null && keyboard.leftShiftKey.isPressed);
        isRunning = !isCrouching && keyboard != null && keyboard.leftCtrlKey.isPressed;

        if (Input.GetKeyDown(KeyCode.Space) || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame))// Если пробел нажат
            jumpBuffer = 0.12f;// Время на прыжок перед приземлением

        float sensitivity = mouseSensitivity / 100f;
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch - Input.GetAxis("Mouse Y") * sensitivity, -89f, 89f);

        if (Input.GetKeyDown(KeyCode.F5))// Смена камеры
        {
            isFirstFace = !isFirstFace;
            firstFace.SetActive(isFirstFace);
            thirdFace.SetActive(!isFirstFace);
        }

        bool isMoving = moveInput.sqrMagnitude > 0f;
        anim.SetBool("Walk", isMoving && !isRunning);
        anim.SetBool("Run", isMoving && isRunning);
        anim.SetBool("Crouch", isCrouching);
        anim.SetFloat("CrouchMove", moveInput.magnitude);
    }

    void LateUpdate()
    {
        cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        crouchDrop = Mathf.MoveTowards(crouchDrop, isCrouching ? crouchDistance : 0f, 2.2f * Time.deltaTime);
        cameraPivot.localPosition = standingPivot + Vector3.down * crouchDrop;
        if (head != null)
            head.rotation = cameraPivot.rotation;// Поворот головы за камерой

        if (!isFirstFace && thirdCamera != null)
            UpdateThirdCamera();
    }

    void UpdateThirdCamera()// Камера от третьего лица
    {
        Transform mount = thirdCamera.transform;
        Vector3 eyes = firstFace.transform.position;
        Vector3 home = mount.parent.TransformPoint(thirdCameraHome);
        Vector3 offset = home - eyes;
        float near = thirdCamera.nearClipPlane;
        float halfHeight = near * Mathf.Tan(thirdCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 halfScreen = new Vector3(halfHeight * thirdCamera.aspect, halfHeight, 0.01f);

        Vector3 planeStart = eyes + mount.forward * near;// Центр ближней плоскости камеры
        float fullDistance = offset.magnitude;
        if (fullDistance < 0.001f)
            return;
        float distance = fullDistance;
        Vector3 direction = offset / fullDistance;
        if (Physics.CheckBox(planeStart, halfScreen, mount.rotation, cameraMask, QueryTriggerInteraction.Ignore))
            distance = 0f;
        else if (Physics.BoxCast(planeStart, halfScreen, direction, out RaycastHit hit, mount.rotation, distance, cameraMask, QueryTriggerInteraction.Ignore))
            distance = Mathf.Max(0f, hit.distance - 0.02f);

        if (distance > 0f && Physics.SphereCast(eyes, 0.05f, direction, out RaycastHit bodyHit, distance, cameraMask, QueryTriggerInteraction.Ignore))
            distance = Mathf.Max(0f, bodyHit.distance - 0.02f);
        thirdCameraDistance = Mathf.Min(distance, thirdCameraDistance + 12f * Time.deltaTime);// Плавно отдаляем камеру
        mount.localPosition = Vector3.Lerp(firstFace.transform.localPosition, thirdCameraHome, thirdCameraDistance / fullDistance);
    }

    void UpdateCrouch(Quaternion rotation)// Высота игрока на Shift
    {
        if (crouchHeld == isCrouching)
            return;
        if (!crouchHeld && crouchDistance > 0.002f)
        {
            Vector3 top = standingCenter + Vector3.up * ((standingSize.y - crouchDistance) * 0.5f);
            Vector3 half = new Vector3(standingSize.x, crouchDistance, standingSize.z) * 0.5f - Vector3.one * 0.001f;
            if (Physics.CheckBox(rb.position + rotation * top, half, rotation, solidLayers, QueryTriggerInteraction.Ignore))
                return;// Над головой блок
        }
        isCrouching = crouchHeld;
        float drop = isCrouching ? crouchDistance : 0f;
        bodyBox.size = standingSize - Vector3.up * drop;
        bodyBox.center = standingCenter - Vector3.up * (drop * 0.5f);// Ноги остаются на земле
    }

    void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(rotation);// Поворот игрока мышкой
        UpdateCrouch(rotation);
        FindNearbyBlocks();

        float forward = moveInput.y;
        if (forward < 0f)
            forward *= backwardsMultiplayer;

        float speed = isRunning ? runningSpeed : walkingSpeed;
        if (isCrouching)
            speed = crouchingSpeed;

        Vector3 targetVelocity = rotation * new Vector3(moveInput.x, 0f, forward) * speed;
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;// Ходьба по X и Z
        Vector3 nextVelocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        bool grounded = (jumpBuffer > 0f || isCrouching) && rb.linearVelocity.y < 0.4f &&
            groundBox.enabled && groundBox.gameObject.activeInHierarchy && HasSupport(rb.position, rb.rotation);
        bool canJump = jumpBuffer > 0f && grounded;
        if (isCrouching && grounded && !canJump)
            nextVelocity = StayOnBlock(Vector3.ClampMagnitude(nextVelocity, crouchingSpeed));
        rb.AddForce(nextVelocity - velocity, ForceMode.VelocityChange);

        if (canJump)
        {
            Vector3 landingVelocity = rb.linearVelocity;
            if (landingVelocity.y < 0f)
                rb.linearVelocity = new Vector3(landingVelocity.x, 0f, landingVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);// Прыгаем вверх
            jumpBuffer = 0f;
        }

        jumpBuffer = Mathf.Max(0f, jumpBuffer - Time.fixedDeltaTime);
    }

    bool HasSupport(Vector3 position, Quaternion rotation)// Есть ли земля под ногами
    {
        Vector3 half = new Vector3(standingSize.x * 0.5f - EdgeInset, 0.002f, standingSize.z * 0.5f - EdgeInset);
        return FloorUnder(position + rotation * standingCenter, half, rotation);
    }

    bool FloorUnder(Vector3 start, Vector3 half, Quaternion rotation)
    {
        Bounds sensor = groundBox.bounds;
        start.y = sensor.max.y + Physics.defaultContactOffset;
        return Physics.BoxCast(start, half, Vector3.down, out RaycastHit hit, rotation,
            start.y - sensor.min.y, groundLayer, QueryTriggerInteraction.Ignore) && hit.distance > 0f && hit.normal.y >= 0.7f;
    }

    bool CanSneakTo(Vector3 position)
    {
        return FloorUnder(position, sneakProbeHalf, Quaternion.identity) ||
            FloorUnder(position, sneakProbeHalf, DiagonalProbe);
    }

    Vector3 StayOnBlock(Vector3 velocity)// Ходьба по краю на Shift
    {
        Vector3 step = velocity * Time.fixedDeltaTime;
        if (CanSneakTo(rb.position + step))
            return velocity;

        if (step.x != 0f && CanSneakTo(rb.position + Vector3.right * step.x))
            return Vector3.right * velocity.x;
        if (step.z != 0f && CanSneakTo(rb.position + Vector3.forward * step.z))
            return Vector3.forward * velocity.z;
        return Vector3.zero;
    }
}
