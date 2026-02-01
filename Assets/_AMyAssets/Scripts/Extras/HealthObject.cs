using PurrNet;
using UnityEngine;

public class HealthObject : NetworkBehaviour
{
    [Header("Stats")]
    public SyncVar<int> health = new SyncVar<int>(100);
    
    [Header("Visuals")]
    [SerializeField] private GameObject deathVFX;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private GameObject floatingDamage;
    [SerializeField] private MeshRenderer mesh;
    [SerializeField] private SphereCollider col;

    private int maxHealth;
    private bool isDead = false;
    private float counter;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        maxHealth = health.value;
        
        if (health.value <= 0) 
        {
            SetDeadState(true);
        }
    }

    void Update()
    {
        if (!isServer || !isDead) return;

        counter += Time.deltaTime;
        if (counter >= 3)
        {
            Restart();
        }
    }

    // --- LÓGICA DE SERVIDOR ---
    [ServerRpc(runLocally: false)]
    public void ChangeHealth(int _amount, Vector3 hitPoint)
    {
        if (!isServer) return;

        health.value += _amount;

        //SpawnDamageTextObserversRpc(_amount, hitPoint);

        if (health.value <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        PlayDeathEffectsObserversRpc();
        
        SetStateObserversRpc(false); 
    }

    private void Restart()
    {
        health.value = maxHealth;
        isDead = false;
        counter = 0;
        
        SetStateObserversRpc(true);
    }

    // --- LÓGICA VISUAL ---

    [ObserversRpc]
    private void SpawnDamageTextObserversRpc(int amount, Vector3 pos)
    {
        if (floatingDamage)
        {
            GameObject floatTxt = Instantiate(floatingDamage, pos, Quaternion.identity);
            
            floatTxt.transform.LookAt(Camera.main.transform);
            floatTxt.transform.Rotate(0, 180, 0); 
            
            if(floatTxt.TryGetComponent(out TextMesh tm))
            {
                tm.text = (amount * -1).ToString("f0");
            }
        }
    }

    [ObserversRpc]
    private void PlayDeathEffectsObserversRpc()
    {
        if (deathVFX) Instantiate(deathVFX, transform.position + Vector3.up, Quaternion.identity);
        if (deathParticles) deathParticles.Play();
    }

    [ObserversRpc]
    private void SetStateObserversRpc(bool active)
    {
        if (mesh) mesh.enabled = active;
        if (col) col.enabled = active;
        
        isDead = !active; 
    }
    
    private void SetDeadState(bool dead)
    {
        isDead = dead;
        if (mesh) mesh.enabled = !dead;
        if (col) col.enabled = !dead;
    }
}