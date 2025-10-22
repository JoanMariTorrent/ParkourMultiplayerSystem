using PurrNet;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using PurrNet.StateMachine;


[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float wallCheckDistance = 0.4f;
    [Space(2)]
    [SerializeField] private float wallRunSpeed = 6f;
    [SerializeField] private float wallRunGravity = -2f;
    [SerializeField] private float wallInitialImpulse = 5f; // mini impulso inicial
    [SerializeField] private float wallStickForce = 2f;

    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private int timesJump = 2;
    public bool _isAiming;
    public bool isGrounded;


    [Header("References")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private List<Renderer> renderers = new ();
    [SerializeField] private StateMachine _stateMachine;
    [SerializeField] private WeaponManager _weaponManager;
    [SerializeField] private Transform _checkGround;
    [SerializeField] private Transform _checkWall;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _wallLayer;


    private CharacterController characterController;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    private RaycastHit leftWallhit;
    private RaycastHit rightWallhit;
    private bool isOnRightWall;
    private bool isOnLeftWall;

    private bool isWallRunning;
    private bool isTouchingWall;




    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
        playerCamera.gameObject.SetActive(isOwner);

        if (isOwner)
        {
            foreach (var rend in renderers)
            {
                rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }



    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        HandleWeaponSwitching();
        HandleMovement();
        HandleRotation();
        HandleRunningWall();
    }



    private void HandleMovement()
    {
        isGrounded = IsGrounded();
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            timesJump = 2;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);


        // Input de saltar
        if (Input.GetButtonDown("Jump") && timesJump > 0)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            timesJump--;
        }


        if (!isWallRunning)
        {
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
        

        
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(_checkGround.position, Vector3.down, groundCheckDistance, _groundLayer);
    }

    private void HandleWeaponSwitching()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponManager.SwitchWeapon(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _weaponManager.SwitchWeapon(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _weaponManager.SwitchWeapon(4);
        }
    }


    private void HandleRunningWall()
    {
        isOnRightWall = Physics.Raycast(transform.position, _checkWall.right, out rightWallhit, wallCheckDistance, _wallLayer);
        isOnLeftWall = Physics.Raycast(transform.position, -_checkWall.right, out leftWallhit, wallCheckDistance, _wallLayer);

        isTouchingWall = isOnRightWall || isOnLeftWall;

        //Tocando una pared
        if (isTouchingWall && velocity.y > 0 && !isGrounded)
        {
            // Direccion del wall run
            Vector3 wallNormal = Vector3.zero;

            wallNormal = isOnRightWall ? rightWallhit.normal : leftWallhit.normal;

            Vector3 wallRunDirection = Vector3.Cross(wallNormal, Vector3.up);

            if (isOnLeftWall)
                wallRunDirection = -wallRunDirection;

            Debug.DrawRay(transform.position, wallRunDirection * 3f, Color.green);

            // Aceleración / Mantener momentum
            Vector3 wallRunVelocity = wallRunDirection.normalized * wallRunSpeed;

            // se suma a la velocidad del jugador, asi se mantiene el momentum
            float acceleration = 10f;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, wallRunVelocity, acceleration * Time.deltaTime);

            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;

            // Gravedad reducida
            velocity.y += wallRunGravity * Time.deltaTime;

            //Reinicio de saltos
            timesJump = 1;

            isWallRunning = true;
        }
        else
        {
            isWallRunning = false;
        }

        

    }




#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.03f, Vector3.down * groundCheckDistance);
    }
#endif
}