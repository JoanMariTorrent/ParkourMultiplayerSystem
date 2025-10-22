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
        if (!isOwner)
        {
            enabled = false;
            return;
        }

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
        else
        {
            gunScript = null;
            return;
        }

        //if (playerScript != null)
        //    isAiming = playerScript._isAiming;

        if (gunScript == null)
            return; 

        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, gunScript.returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, gunScript.snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }



    [ObserversRpc]
    public void RecoilFire()
    {
        if(!isOwner) return;
        if (!isAiming) targetRotation += new Vector3(gunScript.recoilX, Random.Range(-gunScript.recoilY, gunScript.recoilY), Random.Range(-gunScript.recoilZ, gunScript.recoilZ));
        else targetRotation += new Vector3(gunScript.aimRecoilX, Random.Range(-gunScript.aimRecoilY, gunScript.aimRecoilY), Random.Range(-gunScript.aimRecoilZ, gunScript.aimRecoilZ));
    }


    

}
