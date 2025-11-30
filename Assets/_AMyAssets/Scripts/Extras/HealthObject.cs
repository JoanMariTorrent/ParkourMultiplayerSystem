using PurrNet;
using UnityEngine;

public class HealthObject : NetworkBehaviour
{
    public int health = 100;
    [SerializeField] private GameObject deathVFX;
    [SerializeField] private GameObject floatingDamage;
    [SerializeField] private MeshRenderer mesh;

    private int maxHealth;
    private bool death;
    public float counter;

    void Start()
    {
        maxHealth = health;
    }

    void Update()
    {
        if(!death) return;
        counter += Time.deltaTime;
        if(counter >= 3) Restart();
    }

    public void ChangeHealth(int _amount,Vector3 playerPos, RPCInfo _info = default)
    {
        health += _amount;
        floatingDamage.GetComponent<TextMesh>().text = (_amount * -1).ToString("f0");
        Instantiate(floatingDamage, transform.position, Quaternion.LookRotation(playerPos - transform.position));
        if (health <= 0)
        {
            Vector3 spawnVFX = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            Instantiate(deathVFX, spawnVFX, Quaternion.identity);
            death = true;
            mesh.enabled = false;
        }
    }

    private void Restart()
    {
        health = maxHealth;
        death = false;
        mesh.enabled = true;
        counter = 0;
    }
}
