using UnityEngine;
using PurrNet;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private SettingsData settings;
    
    private const float SENS_MULTIPLIER = 0.01f; 

    private float _currentSensitivity; 
    private AimType _currentAimType = AimType.Normal;
    
    private float _pitch;
    private float _yaw;

    public void Intialize(Transform target)
    {
        transform.position = target.position;
        Vector3 currentAngles = target.eulerAngles;
        _pitch = currentAngles.x;
        _yaw = currentAngles.y;

        settings.OnSensitivityChanged += RecalculateSensitivity;
        RecalculateSensitivity();
    }


    public void SetSensitivityMode(AimType newAimType)
    {
        if (_currentAimType == newAimType) return; 

        _currentAimType = newAimType;
        RecalculateSensitivity();
    }

    public void RecalculateSensitivity()
    {
        float targetSensValue = settings.sensitivity; 

        switch (_currentAimType)
        {
            case AimType.Normal:
                targetSensValue = settings.sensitivity;
                break;
            case AimType.Aiming:
                targetSensValue = settings.aimingSensitivity;
                break;
            case AimType.Sniper:
                targetSensValue = settings.sniperSensitivity;
                break;
        }

        _currentSensitivity = targetSensValue * SENS_MULTIPLIER;
    }

    public void UpdateRotation(CameraInput input)
    {
        _pitch -= input.Look.y * _currentSensitivity;
        _pitch = Mathf.Clamp(_pitch, -85f, 85f); 
        _yaw += input.Look.x * _currentSensitivity;

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }

    private void OnDestroy()
    {
        if(settings != null) settings.OnSensitivityChanged -= RecalculateSensitivity;
    }
}