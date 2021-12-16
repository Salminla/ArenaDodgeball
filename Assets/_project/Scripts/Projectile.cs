using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace _project.Scripts
{
    public class Projectile : SelfDestructingNetworkObject
    {
        public int damageAmount = 10;
        
        public ulong ownerId;
        public override void OnNetworkSpawn()
        {
            StartCoroutine(SetCollision());
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!NetworkManager.Singleton.IsServer || !NetworkObject.IsSpawned)        
                return;

            Player player = collision.gameObject.GetComponentInParent<Player>();
            
            if (player != null)
            {
                player.TakeDamage(collision.GetContact(0).point, ownerId, damageAmount);
            }
        }

        IEnumerator SetCollision()
        {
            gameObject.GetComponent<SphereCollider>().enabled = false;
            yield return new WaitForSeconds(0.05f);
            gameObject.GetComponent<SphereCollider>().enabled = true;
        }
    }
}