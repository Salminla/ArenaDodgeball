using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Cannon : NetworkBehaviour, IWeapon
{
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileSpeed = 20;
    
    
    public void Shoot(Vector3 dir)
    {
        Debug.Log("Shooting towards " + dir);
        
        NetworkObject newProjectile = NetworkObjectPool.Singleton.GetNetworkObject(projectile, transform.position + dir.normalized + transform.forward + Vector3.up, Quaternion.identity);
        //newProjectile.Spawn();
        newProjectile.GetComponent<Rigidbody>().AddForce(dir * projectileSpeed, ForceMode.Impulse);
        StartCoroutine(DespawnDelay(newProjectile));
    }

     
    IEnumerator DespawnDelay(NetworkObject obj)
    {
        yield return new WaitForSeconds(5f);
        //obj.Despawn();
        NetworkObjectPool.Singleton.ReturnNetworkObject(obj, projectile);
        
        
    }
    
    [ServerRpc]
    public void ShootServerRpc(Vector3 dir)
    {
        Shoot(dir);
    }
}
