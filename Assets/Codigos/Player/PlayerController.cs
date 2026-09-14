using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cerrado.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private CharacterController characterController;

        [Header("Velocidades")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float crouchSpeed = 2.2f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -18.0f;

        [Header("Sensibilidade da Câmera")]
        [SerializeField] private float mouseSensitivity = 1.8f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Agachamento")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        // Estados
        private float verticalVelocity;
        private float cameraPitch;
        private bool isCrouching;
        private bool isSprinting;
        private bool canControl = true;
        private Vector3 defaultCameraLocalPos;

        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public Camera PlayerCamera => playerCamera;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera != null)
            {
                defaultCameraLocalPos = playerCamera.transform.localPosition;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!canControl) return;

            HandleLook();
            HandleMovement();
            HandleCrouch();
        }

        private void HandleLook()
        {
            if (playerCamera == null) return;

            Vector2 lookInput = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue() * 0.1f;
            }
#else
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif

            float yaw = lookInput.x * mouseSensitivity;
            float pitch = lookInput.y * mouseSensitivity;

            // Rotação horizontal do corpo do jogador
            transform.Rotate(Vector3.up * yaw);

            // Rotação vertical da câmera (com clamp para não virar a cabeça 360)
            cameraPitch -= pitch;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            playerCamera.transform.localEulerAngles = Vector3.right * cameraPitch;
        }

        private void HandleMovement()
        {
            Vector2 moveInput = Vector2.zero;
            bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1f;

                isSprinting = Keyboard.current.leftShiftKey.isPressed && !isCrouching && moveInput.y > 0.1f;
                if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    isCrouching = !isCrouching;
                }

                jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#else
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && moveInput.y > 0.1f;
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                isCrouching = !isCrouching;
            }
            jumpPressed = Input.GetButtonDown("Jump");
#endif

            moveInput = moveInput.normalized;

            // Determina a velocidade atual
            float currentSpeed = walkSpeed;
            if (isCrouching) currentSpeed = crouchSpeed;
            else if (isSprinting) currentSpeed = sprintSpeed;

            Vector3 moveDirection = (transform.forward * moveInput.y) + (transform.right * moveInput.x);

            // Gravidade e Pulo
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f; // Mantém colado ao chão
                }

                if (jumpPressed && !isCrouching)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 finalMovement = (moveDirection * currentSpeed) + (Vector3.up * verticalVelocity);
            characterController.Move(finalMovement * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            float targetHeight = isCrouching ? crouchHeight : standingHeight;
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            if (playerCamera != null)
            {
                float targetCamY = isCrouching ? (defaultCameraLocalPos.y * 0.6f) : defaultCameraLocalPos.y;
                Vector3 camPos = playerCamera.transform.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
                playerCamera.transform.localPosition = camPos;
            }
        }

        public void SetControlActive(bool active)
        {
            canControl = active;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}

