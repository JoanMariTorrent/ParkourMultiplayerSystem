using UnityEngine;

public class PlayerProceduralAnimations : MonoBehaviour
{
    [Header("Referencias Generales")]
    [SerializeField] private PlayerCharacter _playerCharacter; 
    [SerializeField] private Transform _weaponHolder; 

    [Header("1. Camera Tilt (Inclinación al moverse)")]
    [SerializeField] private float tiltAngle = 2f; 
    [SerializeField] private float tiltSpeed = 6f;

    [Header("2. Landing Impact")]
    [SerializeField] private float landDipAmount = 0.3f;
    [SerializeField] private float landRecoverSpeed = 5f;
    [SerializeField] private float landThreshold = -4f; // Velocidad mínima para activar el golpe

    // Variables internas
    private Vector3 _initialWeaponPos;
    private Quaternion _initialRotation;
    private float _initialYPos;
    private float _timer;
    private float _targetTilt;
    private Vector3 _currentBobPos; 
    

    private bool _wasGrounded;
    private float _currentLandDip = 0f;
    private float _fallSpeed;

    void Start()
    {
        if (_weaponHolder) _initialWeaponPos = _weaponHolder.localPosition;
        _initialRotation = transform.localRotation;
        _initialYPos = transform.localPosition.y;
        
        if (_playerCharacter != null) _wasGrounded = true; 
    }

    void Update()
    {
        if (_playerCharacter == null) return;


        Vector3 velocity = _playerCharacter._state.Velocity;
        float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;
        bool isGrounded = _playerCharacter._state.Grounded; 

        float inputX = Input.GetAxisRaw("Horizontal");

        
        if (!isGrounded)
        {
            if (velocity.y < _fallSpeed)
            {
                _fallSpeed = velocity.y;
            }
        }
        else
        {
            if (_wasGrounded) _fallSpeed = 0f;
        }

        if (!_wasGrounded && isGrounded)
        {

            if (_fallSpeed < landThreshold)
            {
                _currentLandDip = landDipAmount;
            }
            
            _fallSpeed = 0f;
        }
        
        _wasGrounded = isGrounded;

        _currentLandDip = Mathf.Lerp(_currentLandDip, 0, Time.deltaTime * landRecoverSpeed);
        
        Vector3 currentPos = transform.localPosition;
        currentPos.y = _initialYPos - _currentLandDip; 
        transform.localPosition = currentPos;

        
        // --- 2. CAMERA TILT ---
        _targetTilt = -inputX * tiltAngle;
        Quaternion targetRot = Quaternion.Euler(0, 0, _targetTilt);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _initialRotation * targetRot, Time.deltaTime * tiltSpeed);


    }
}