using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Rigid body")]
    private Rigidbody rb;// Гравитацыя

    [Header("Movement settings")]
    [SerializeField] float walkingSpeed = 10f;// Скорость ходьбы
    [SerializeField] float runningSpeed = 25f;// Скорость бега
    [SerializeField] float mouseSensitivity = 100f;// Чувствительность мышки
    public float backwardsMultiplayer = 0.5f;// Наскоко медлене ходба/бег назад
    public bool isRunning = false;

    [Header("Camera")]
    [SerializeField] Transform playerCamera;// Камера
    public float cameraPitch;// Поворт камеры

    [Header("Jump settings")]
    [SerializeField] float jumpForce = 6f;// Сила прыжка
    [SerializeField] Transform groundCheck;// Точка под ногами
    [SerializeField] LayerMask groundLayer;// Слой земли

    [Header("Animations")]
    [SerializeField] Animator anim;

    [Header("Camera")]
    [SerializeField] GameObject firstFace;
    [SerializeField] GameObject thirdFace;
    public bool isFirstFace = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraPitch = Mathf.DeltaAngle(0f, playerCamera.localEulerAngles.x);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            isFirstFace = !isFirstFace;
            firstFace.SetActive(isFirstFace ? true : false);
            thirdFace.SetActive(isFirstFace ? false : true);
        }

        float sensitivity = mouseSensitivity / 100f;// Поворот мышкой при чувствительности 100 множитель равен 1
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        transform.Rotate(Vector3.up, mouseX);// Поворот игрока за камерой

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -89f, 89f);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        if (Input.GetKeyDown(KeyCode.LeftControl))// Если Control нажат, бег включяеца
        {
            isRunning = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))// Если Control отжат, бег выключяеца
        {
            isRunning = false;
        }

        if (Input.GetKey(KeyCode.W))// Вперёд
        {
            anim.SetBool(isRunning ? "Run" : "Walk", true);
            transform.Translate(Vector3.forward * (isRunning ? runningSpeed : walkingSpeed) * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))// Назад
        {
            anim.SetBool(isRunning ? "Run" : "Walk", true);
            transform.Translate(-Vector3.forward * (isRunning ? runningSpeed : walkingSpeed) * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))// Налего
        {
            anim.SetBool(isRunning ? "Run" : "Walk", true);
            transform.Translate(Vector3.left * (isRunning ? runningSpeed : walkingSpeed) * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))// Направо
        {
            anim.SetBool(isRunning ? "Run" : "Walk", true);
            transform.Translate(Vector3.right * (isRunning ? runningSpeed : walkingSpeed) * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space))// Если пробел нажат
        {
            if (Physics.CheckSphere(groundCheck.position, 0.1f, groundLayer, QueryTriggerInteraction.Ignore))// Если под ногами земля
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);// Прыгаем вверх
            }
        }
    }
}