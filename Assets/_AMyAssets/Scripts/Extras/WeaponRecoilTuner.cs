using System.Collections;
using UnityEngine;

public class WeaponRecoilTuner : MonoBehaviour
{
    [Header("--- DEBUG SETTINGS ---")]
    [Tooltip("Activa o desactiva el recoil para ver la diferencia")]
    public bool enableRecoil = true;
    public KeyCode fireKey = KeyCode.Mouse0;

    [Header("--- COPIAR A GUN.CS ---")]
    [Header("Recoil Visual Settings")]
    [SerializeField] protected float _recoilStrenght = 1f;
    [SerializeField] protected float _recoilDuration = 0.2f;
    [SerializeField] protected float _rotationAmount = 25f;
    
    [Header("Curves")]
    // He puesto valores por defecto para que no empieces de cero
    [SerializeField] protected AnimationCurve _recoilCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 1), new Keyframe(1, 0));
    [SerializeField] protected AnimationCurve _rotationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 1), new Keyframe(1, 0));

    [Header("References")]
    [SerializeField] protected RecoilCamera recoilCamera; // Asigna esto si quieres testear tambien la camara

    // Estado interno
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Coroutine _recoilCoroutine;

    void Start()
    {
        // Guardamos la posición inicial al arrancar
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
    }

    void Update()
    {
        // Input simple para testear
        if (Input.GetKeyDown(fireKey) || Input.GetKeyDown(KeyCode.Space))
        {
            TestFire();
        }
    }

    void TestFire()
    {
        if (!enableRecoil) return;

        // 1. Efecto en la Cámara (si está asignada)
        if (recoilCamera != null)
        {
            recoilCamera.RecoilFire(); 
        }

        // 2. Efecto en el Arma (Modelo 3D)
        if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
        _recoilCoroutine = StartCoroutine(PlayRecoil());
    }

    // Esta es LA MISMA corrutina que tienes en Gun.cs
    protected IEnumerator PlayRecoil()
    {
        float elapsed = 0f;
        while (elapsed < _recoilDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _recoilDuration;

            // Calcular posición (retroceso hacia atrás en Z)
            float rVal = _recoilCurve.Evaluate(t) * _recoilStrenght;
            Vector3 posOffset = Vector3.back * rVal;

            // Calcular rotación (levantar el cañón en X)
            float rotVal = _rotationCurve.Evaluate(t) * _rotationAmount;
            Vector3 rotOffset = new Vector3(-rotVal, 0, 0); // Nota: A veces es positivo o negativo dependiendo de tu modelo. Prueba quitar el menos si rota hacia abajo.

            // Aplicar
            transform.localPosition = _originalPosition + posOffset;
            transform.localRotation = _originalRotation * Quaternion.Euler(rotOffset);

            yield return null;
        }

        // Volver a la posición exacta original
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
    }
    
    // Función extra para reiniciar la posición si tocas algo en el inspector mientras juegas
    [ContextMenu("Reset Position")]
    public void ResetPos()
    {
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
    }
}