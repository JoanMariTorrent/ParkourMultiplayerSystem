using UnityEngine;

public class QuickGizmo : MonoBehaviour
{
    public enum GizmoType { Sphere, Cube, WireSphere, WireCube, RayForward }

    [Header("Configuración")]
    public GizmoType shape = GizmoType.Sphere;
    public Color color = new Color(0, 1, 0, 0.5f); // Verde semitransparente por defecto
    [Range(0.001f, 1f)] public float size = 0.1f;
    
    [Header("Opciones")]
    public bool alwaysShow = true; // Si es false, solo se ve al seleccionarlo

    private void OnDrawGizmos()
    {
        if (alwaysShow) DrawMyGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!alwaysShow) DrawMyGizmo();
    }

    private void DrawMyGizmo()
    {
        Gizmos.color = color;

        // ESTO ES MAGIA: Hace que el gizmo rote con el objeto
        // Así sabrás si la mano va a quedar torcida
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (shape)
        {
            case GizmoType.Sphere:
                Gizmos.DrawSphere(Vector3.zero, size);
                break;
            case GizmoType.Cube:
                Gizmos.DrawCube(Vector3.zero, Vector3.one * size);
                break;
            case GizmoType.WireSphere:
                Gizmos.DrawWireSphere(Vector3.zero, size);
                break;
            case GizmoType.WireCube:
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one * size);
                break;
            case GizmoType.RayForward:
                // Dibuja una línea hacia donde apunta el objeto (Z blue axis)
                // Ideal para saber hacia dónde disparará el arma o mirará la mano
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * (size * 5)); 
                Gizmos.DrawSphere(Vector3.forward * (size * 5), size * 0.2f);
                break;
        }

        // Devolvemos la matriz a la normalidad para no romper otros gizmos
        Gizmos.matrix = oldMatrix;
    }
}