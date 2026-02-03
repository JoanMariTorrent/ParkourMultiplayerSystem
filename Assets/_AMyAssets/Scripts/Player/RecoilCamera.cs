using PurrNet;
using Steamworks;
using UnityEngine;

public class RecoilCamera : NetworkBehaviour
{
    //scripts
    //[SerializeField] private PlayerController playerScript;
    [SerializeField] private Gun gunScript;
    [SerializeField] private WeaponManager weaponManager;

    //bools
    [SerializeField] private bool isAiming;

    //rotations
    private Vector3 currentRotation;
    private Vector3 targetRotation;


    [Range(0.001f, 0.01f)]
    [SerializeField] private float Amount = 0.002f;
    [Range(1f, 30f)]
    [SerializeField] private float Frequency = 10f;
    [Range(10f, 100f)]
    [SerializeField] private float Smooth = 10f;

    Vector3 StartPos;


    private void Start()
    {
        //playerScript = GetComponentInParent<PlayerController>();
        StartPos = transform.localPosition;
    }



    private void Update()
    {
        if (!isOwner) return;

        if (weaponManager._currentGun != null)
        {
            gunScript = weaponManager._currentGun.GetComponent<Gun>();
        }

        if(gunScript == null) return;

        if(gunScript.equipedGun == false) return;
        

        //if (playerScript != null)
        //    isAiming = playerScript._isAiming;

        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, gunScript.returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, gunScript.snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }



    [ObserversRpc]
    public void RecoilFire()
    {
        if(!isOwner) return;
        bool isMoving = false;
        if(gunScript.playerCharacter != null)
        {
            Vector3 velocity = gunScript.playerCharacter._state.Velocity;

            velocity.y = 0;

            isMoving = velocity.magnitude > 0.4f;
        }
        if (gunScript.isAiming) targetRotation += new Vector3(gunScript.aimRecoilX, Random.Range(-gunScript.aimRecoilY, gunScript.aimRecoilY), Random.Range(-gunScript.aimRecoilZ, gunScript.aimRecoilZ));
        else if (isMoving) targetRotation += new Vector3(gunScript.movingRecoilX, Random.Range(-gunScript.movingRecoilY, gunScript.movingRecoilY), Random.Range(-gunScript.movingRecoilZ, gunScript.movingRecoilZ));
        else targetRotation += new Vector3(gunScript.recoilX, Random.Range(-gunScript.recoilY, gunScript.recoilY), Random.Range(-gunScript.recoilZ, gunScript.recoilZ));
    }


    

}
