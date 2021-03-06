using System.Collections;
using _project.Scripts;
using Unity.Netcode;
using UnityEngine;

public class Cannon : NetworkBehaviour, IWeapon
{
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileSpeed = 20;
    [SerializeField] private float shootInterval = 1f;
    private bool allowShooting = true;

    public NetworkObject owner;

    public void SetOwner(NetworkObject owner)
    {
        this.owner = owner;
    }
    
    public void Shoot(Vector3 dir, ulong ownerId)
    {
        if (!allowShooting) return;
        StartCoroutine(ShootDelay(shootInterval));
        //NetworkObject newProjectile = NetworkObjectPool.Singleton.GetNetworkObject(projectile, transform.position + dir.normalized + transform.forward + Vector3.up, Quaternion.identity);
        NetworkObject newProjectile = Instantiate(projectile,
            transform.position + dir.normalized + transform.forward + Vector3.up, Quaternion.identity).GetComponent<NetworkObject>();
        newProjectile.GetComponent<Projectile>().ownerId = ownerId;
        newProjectile.GetComponent<SelfDestructingNetworkObject>().Init(5f, this);
        newProjectile.GetComponent<Rigidbody>().AddForce(dir * projectileSpeed, ForceMode.Impulse);
    }

    IEnumerator ShootDelay(float amount)
    {
        allowShooting = false;
        yield return new WaitForSeconds(amount);
        allowShooting = true;
    }
    [ServerRpc]
    public void ShootServerRpc(Vector3 dir, ulong ownerId)
    {
        Shoot(dir, ownerId);
    }
}
