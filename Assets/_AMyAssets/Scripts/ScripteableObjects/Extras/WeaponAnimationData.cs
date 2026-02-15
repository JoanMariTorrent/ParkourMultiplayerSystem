using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponAnimData", menuName = "Game/AnimationDataBase")]
public class WeaponAnimationData : ScriptableObject
{
    [Header("1. IDLE (Respiración)")]
    public float idleAmount = 0.02f;
    public float idleSpeed = 1f;

    [Header("2. WALK (Caminar)")]
    public float walkBobAmount = 0.02f;
    public float walkBobSpeed = 10f;

    [Header("3. RUN / SLIDE (Correr)")]
    public Vector3 runningTilt = new Vector3(15f, -45f, 0f);
    public float runBobMultiplier = 5f;
    public float runTransitionSpeed = 6f;

    [Header("4. SLIDE IMPACT (Golpe)")]
    public float slideDropAmount = -0.15f;
    public float slideKickRotation = -10f;
    public float slideRecoverySpeed = 8f;

    [Header("5. STRAFE & TILT (Inclinación)")]
    public float strafeTiltAmount = 4f;
    public float forwardTiltAmount = 3f;
    public float tiltSpeed = 5f;

    [Header("6. SWAY (Inercia)")]
    public float swayAmount = 0.02f;
    public float maxSway = 0.06f;
    public float swaySmooth = 4f;

    [Header("7. AIMING (Apuntado)")]
    public Vector3 aimPosition = new Vector3(-0.266f, 0.03f, 0f);
    public float aimSpeed = 12.5f;
}
