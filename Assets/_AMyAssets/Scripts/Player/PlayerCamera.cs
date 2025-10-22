using Steamworks;
using UnityEngine;
using PurrNet;


public struct CameraInput
{
    public Vector2 Look;
}



public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private float sensitivity = 0.1f;

    private Vector3 _eulerAngles;


    public void Intialize(Transform target)
    {
        transform.position = target.position;

        transform.eulerAngles = _eulerAngles = target.eulerAngles;
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
