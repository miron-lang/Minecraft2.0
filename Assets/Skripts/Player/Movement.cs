using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    [Header("Movement settings")]
    [SerializeField] float walkingSpeed = 10f;// Скорость ходьбы
    [SerializeField] float runningSpeed = 25f;// Скорость бега
    [SerializeField] float acceleration = 60f;// Как быстро набираем и сбрасываем скорость
    public float backwardsMultiplayer = 0.5f;// Скорость при движении назад
    public bool isRunning;

    [Header("Camera")]
    [SerializeField] Transform head;// Голова персонажа
    [SerializeField] float mouseSensitivity = 100f;
    [SerializeField] GameObject firstFace;
    [SerializeField] GameObject thirdFace;
    public float cameraPitch;
    public bool isFirstFace = true;

    [Header("Jump settings")]
    [SerializeField] float jumpForce = 6f;// Сила прыжка
    [SerializeField] Transform groundCheck;// Точка под ногами
    [SerializeField] LayerMask groundLayer;

    [Header("Animations")]
    [SerializeField] Animator anim;

    private Rigidbody rb;
    private Vector2 moveInput;
    private float yaw;
    private bool jumpRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        yaw = rb.rotation.eulerAngles.y;
        if (head == null)
            head = transform.Find("Torso/Head");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);// Чтобы по диагонали не бегать быстрее
        isRunning = Input.GetKey(KeyCode.LeftControl);

        if (Input.GetKeyDown(KeyCode.Space))
            jumpRequested = true;// Запоминаем нажатие до шага физики

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
    }

    void LateUpdate()
    {
        if (head != null)
            head.rotation = transform.rotation * Quaternion.Euler(cameraPitch, 0f, 0f);// После анимации наклоняем голову по X относительно игрока
    }

    void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(rotation);// Разворачиваем тело по мышке

        float forward = moveInput.y;
        if (forward < 0f)
            forward *= backwardsMultiplayer;

        float speed = isRunning ? runningSpeed : walkingSpeed;
        Vector3 targetVelocity = rotation * new Vector3(moveInput.x, 0f, forward) * speed;
        Vector3 velocityChange = targetVelocity - rb.linearVelocity;
        velocityChange.y = 0f;// Высоту меняют гравитация и прыжок
        rb.AddForce(Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime), ForceMode.VelocityChange);

        if (jumpRequested && Physics.CheckSphere(groundCheck.position, 0.1f, groundLayer, QueryTriggerInteraction.Ignore))
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        jumpRequested = false;
    }
}
