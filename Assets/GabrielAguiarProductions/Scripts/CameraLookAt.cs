using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CameraLookAt : MonoBehaviour 
{
    void Update() 
    {
        if (Application.isPlaying)
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
                transform.Rotate(0, 180, 0);
            }
        }
#if UNITY_EDITOR
        else 
        {
            var sceneCameras = SceneView.GetAllSceneCameras();
            if (sceneCameras.Length > 0 && sceneCameras[0] != null)
            {
                transform.LookAt(sceneCameras[0].transform.position, Vector3.up);
                transform.Rotate(0, 180, 0);
            }
        }
#endif
    }
}