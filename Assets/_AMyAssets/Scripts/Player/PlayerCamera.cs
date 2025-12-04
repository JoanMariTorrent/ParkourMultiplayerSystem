using UnityEngine;
using PurrNet;
using System;

public struct CameraInput
{
    public Vector2 Look;
}



public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private SettingsData settings;
    private float sensitivity;

    private Vector3 _eulerAngles;

    


    public void Intialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
        settings.OnSensitivityChanged += UpdateSens;
        UpdateSens();

    }

    public void UpdateSens()
    {
        sensitivity = settings.sensitivity;
    }

    

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }

    
}
