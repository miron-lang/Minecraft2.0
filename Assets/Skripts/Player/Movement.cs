using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    [Header("Movement settings")]
    [SerializeField] float walkingSpeed = 4.3f;// Скорость ходьбы
    [SerializeField] float runningSpeed = 5.6f;// Скорость бега
    [SerializeField] float crouchingSpeed = 1.4f;// Скорость с зажатым Shift
    [SerializeField] float acceleration = 60f;// Как быстро набираем и сбрасываем скорость
    [Range(0f, 1f)] public float backwardsMultiplayer = 0.8f;// Доля скорости при движении назад
    public bool isRunning;

    [Header("Camera")]
    [SerializeField] Transform head;// Голова персонажа
    [SerializeField] float mouseSensitivity = 100f;
    [SerializeField] GameObject firstFace;
    [SerializeField] GameObject thirdFace;
    public float cameraPitch;
    public bool isFirstFace = true;

    [Header("Jump settings")]
    [SerializeField] float jumpForce = 5f;// Хватает, чтобы запрыгнуть на один блок
    [SerializeField, HideInInspector] bool balanceUpdated;
    [SerializeField] Transform groundCheck;// Точка под ногами
    [SerializeField] LayerMask groundLayer;

    [Header("Animations")]
    [SerializeField] Animator anim;

    private Rigidbody rb;
    private Vector2 moveInput;
    private float yaw;
    private float jumpBuffer;
    private bool crouchHeld;
    private bool isCrouching;
    private BoxCollider bodyBox;
    private BoxCollider groundBox;
    private Vector3 standingSize;
    private Vector3 standingCenter;
    private const float CrouchingHeight = 1.5f;// Высота хитбокса с зажатым Shift
    private float crouchDistance;
    private const float EdgeInset = 0.02f;// Сколько подошвы оставляем на краю
    private int solidLayers;
    private Transform cameraPivot;
    private Vector3 standingPivot;
    private float crouchDrop;
    private Camera thirdCamera;
    private Vector3 thirdCameraHome;
    private float thirdCameraDistance;
    private int cameraMask;

    void OnValidate()
    {
        ApplySavedBalance();
    }

    void ApplySavedBalance()
    {
        if (balanceUpdated)
            return;

        // Один раз обновляем старые значения, сохранённые в Inspector
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
        rb.centerOfMass = Vector3.zero;// Анимация рук и ног не должна сдвигать тело у края
        rb.constraints |= RigidbodyConstraints.FreezeRotation;// Контакты не разворачивают игрока, поворот задаёт мышь
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bodyBox = GetComponent<BoxCollider>();
        standingSize = bodyBox.size;
        standingCenter = bodyBox.center;
        crouchDistance = Mathf.Max(0f, standingSize.y - CrouchingHeight);
        groundBox = groundCheck.GetComponent<BoxCollider>();
        groundBox.isTrigger = true;// Датчик видит пол, но не держит игрока на весу
        solidLayers = Physics.AllLayers & ~(1 << gameObject.layer);
        yaw = rb.rotation.eulerAngles.y;
        if (head == null)
            head = transform.Find("Torso/Head");

        // Обе камеры следуют за игроком, а покачивание костей остаётся на модели
        cameraPivot = new GameObject("Camera Pivot").transform;
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

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);// Чтобы по диагонали не бегать быстрее
        var keyboard = Keyboard.current;
        crouchHeld = Input.GetKey(KeyCode.LeftShift) || (keyboard != null && keyboard.leftShiftKey.isPressed);
        isRunning = !isCrouching && keyboard != null && keyboard.leftCtrlKey.isPressed;

        if (Input.GetKeyDown(KeyCode.Space) || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame))
            jumpBuffer = 0.12f;// При нажатии перед касанием земли прыгаем после приземления

        float sensitivity = mouseSensitivity / 100f;
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch - Input.GetAxis("Mouse Y") * sensitivity, -89f, 89f);

        if (Input.GetKeyDown(KeyCode.F5))
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
            head.rotation = cameraPivot.rotation;// Голова смотрит туда же, куда и камера

        if (!isFirstFace && thirdCamera != null)
            UpdateThirdCamera();
    }

    void UpdateThirdCamera()
    {
        Transform mount = thirdCamera.transform;
        Vector3 eyes = firstFace.transform.position;
        Vector3 home = mount.parent.TransformPoint(thirdCameraHome);
        Vector3 offset = home - eyes;
        float near = thirdCamera.nearClipPlane;
        float halfHeight = near * Mathf.Tan(thirdCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 halfScreen = new Vector3(halfHeight * thirdCamera.aspect, halfHeight, 0.01f);

        // Проверяем всю ближнюю плоскость камеры, в том числе её углы
        Vector3 planeStart = eyes + mount.forward * near;
        float fullDistance = offset.magnitude;
        if (fullDistance < 0.001f)
            return;
        float distance = fullDistance;
        Vector3 direction = offset / fullDistance;
        if (Physics.CheckBox(planeStart, halfScreen, mount.rotation, cameraMask, QueryTriggerInteraction.Ignore))
            distance = 0f;
        else if (Physics.BoxCast(planeStart, halfScreen, direction, out RaycastHit hit, mount.rotation, distance, cameraMask, QueryTriggerInteraction.Ignore))
            distance = Mathf.Max(0f, hit.distance - 0.02f);

        // Сам корпус камеры тоже остаётся перед стеной
        if (distance > 0f && Physics.SphereCast(eyes, 0.05f, direction, out RaycastHit bodyHit, distance, cameraMask, QueryTriggerInteraction.Ignore))
            distance = Mathf.Max(0f, bodyHit.distance - 0.02f);
        // Перед стеной придвигаем сразу, на свободное место возвращаем плавно
        thirdCameraDistance = Mathf.Min(distance, thirdCameraDistance + 12f * Time.deltaTime);
        mount.localPosition = Vector3.Lerp(firstFace.transform.localPosition, thirdCameraHome, thirdCameraDistance / fullDistance);
    }

    void UpdateCrouch(Quaternion rotation)
    {
        if (crouchHeld == isCrouching)
            return;
        if (!crouchHeld && crouchDistance > 0.002f)
        {
            // При вставании проверяем только пространство над головой
            Vector3 top = standingCenter + Vector3.up * ((standingSize.y - crouchDistance) * 0.5f);
            Vector3 half = new Vector3(standingSize.x, crouchDistance, standingSize.z) * 0.5f - Vector3.one * 0.001f;
            if (Physics.CheckBox(rb.position + rotation * top, half, rotation, solidLayers, QueryTriggerInteraction.Ignore))
                return;
        }
        isCrouching = crouchHeld;
        float drop = isCrouching ? crouchDistance : 0f;
        bodyBox.size = standingSize - Vector3.up * drop;
        bodyBox.center = standingCenter - Vector3.up * (drop * 0.5f);// Подошва остаётся на той же высоте
    }

    void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(rotation);// Разворачиваем тело по мышке
        UpdateCrouch(rotation);

        float forward = moveInput.y;
        if (forward < 0f)
            forward *= backwardsMultiplayer;

        float speed = isRunning ? runningSpeed : walkingSpeed;
        if (isCrouching)
            speed = crouchingSpeed;

        Vector3 targetVelocity = rotation * new Vector3(moveInput.x, 0f, forward) * speed;
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;// Высоту меняют гравитация и прыжок
        Vector3 nextVelocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        bool grounded = (jumpBuffer > 0f || isCrouching) && rb.linearVelocity.y < 0.4f &&
            groundBox.enabled && groundBox.gameObject.activeInHierarchy && HasSupport(rb.position, rb.rotation);
        bool canJump = jumpBuffer > 0f && grounded;
        if (isCrouching && grounded && !canJump)
            nextVelocity = StayOnBlock(rotation, Vector3.ClampMagnitude(nextVelocity, crouchingSpeed));
        rb.AddForce(nextVelocity - velocity, ForceMode.VelocityChange);

        if (canJump)
        {
            Vector3 landingVelocity = rb.linearVelocity;
            if (landingVelocity.y < 0f)
                rb.linearVelocity = new Vector3(landingVelocity.x, 0f, landingVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpBuffer = 0f;
        }

        jumpBuffer = Mathf.Max(0f, jumpBuffer - Time.fixedDeltaTime);
    }

    bool HasSupport(Vector3 position, Quaternion rotation)
    {
        // По высоте проверяем зону groundCheck, по ширине — реальную подошву игрока
        Bounds sensor = groundBox.bounds;
        Vector3 start = position + rotation * standingCenter;
        start.y = sensor.max.y + Physics.defaultContactOffset;
        Vector3 half = new Vector3(standingSize.x * 0.5f - EdgeInset, 0.002f, standingSize.z * 0.5f - EdgeInset);
        return Physics.BoxCast(start, half, Vector3.down, out RaycastHit hit, rotation,
            start.y - sensor.min.y, groundLayer, QueryTriggerInteraction.Ignore) && hit.normal.y >= 0.7f;
    }

    Vector3 StayOnBlock(Quaternion rotation, Vector3 velocity)
    {
        Vector3 step = velocity * Time.fixedDeltaTime;
        if (HasSupport(rb.position + step, rotation))
            return velocity;

        // У края можно идти вдоль него, в том числе по диагонали
        if (step.x != 0f && HasSupport(rb.position + Vector3.right * step.x, rotation))
            return Vector3.right * velocity.x;
        if (step.z != 0f && HasSupport(rb.position + Vector3.forward * step.z, rotation))
            return Vector3.forward * velocity.z;
        return Vector3.zero;
    }
}
