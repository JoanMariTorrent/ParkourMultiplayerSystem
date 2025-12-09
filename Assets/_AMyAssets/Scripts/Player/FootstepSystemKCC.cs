using UnityEngine;
using KinematicCharacterController;

public class FootstepSystemKCC : MonoBehaviour
{
    [Header("Audio config")]
    [SerializeField] private AudioClip[] footStepSounds;
    [SerializeField] private float stepDistance = 1.8f;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    [Space][Header("References")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private PlayerCharacter playerCharacter;

    [Space][Header("Ground (Remote)")]
    [SerializeField] private LayerMask groundLayer;

    private float _accumulatedDistance;
    private Vector3 _lastPosition;

    void Start()
    {
        _lastPosition = transform.position;
        if(playerCharacter == null) playerCharacter = GetComponent<PlayerCharacter>();
        if(motor == null) motor = GetComponent<KinematicCharacterMotor>();
    }

    void Update()
    {
        Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastPosFlat = new Vector3(_lastPosition.x, 0, _lastPosition.z);

        float distanceMoved = Vector3.Distance(currentPosFlat, lastPosFlat);
        _lastPosition = transform.position;

        bool isGrounded = CheckGrounded();

        bool isSliding = playerCharacter != null && playerCharacter._state.Stance == Stance.Slide;
        bool isOnWall = playerCharacter != null && playerCharacter._state.Stance == Stance.Wall;

        if((isGrounded || isOnWall) && distanceMoved > 0.005f && !isSliding)
        {
            float currentStepDist = (playerCharacter != null && playerCharacter._state.Stance == Stance.Crouch) ? stepDistance * 1.3f : stepDistance;

            _accumulatedDistance += distanceMoved;
            
            if(_accumulatedDistance >= currentStepDist)
            {
                PlayFootStep();
                _accumulatedDistance = 0f;
            }
        }
    }

    private void PlayFootStep()
    {
        if(footStepSounds.Length > 0 && AudioManager.Instance != null)
        {
            AudioClip clip = footStepSounds[Random.Range(0, footStepSounds.Length)];
            AudioManager.Instance.PlaySound(clip, transform.position, volume, pitch: Random.Range(minPitch, maxPitch));
        }
    }

    private bool CheckGrounded()
    {
        if(playerCharacter != null && playerCharacter.isOwner)
        {
            return motor.GroundingStatus.IsStableOnGround;
        }

        bool hit = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f, groundLayer);
        return hit;
    }
}