using Unity.Netcode;
using UnityEngine;

namespace _project.Scripts
{
    public class Projectile : SelfDestructingNetworkObject
    {
        public int damageAmount = 10;
        
        public NetworkObject owner;

        void OnCollisionEnter(Collision collision)
        {
            if (!NetworkManager.Singleton.IsServer || !NetworkObject.IsSpawned)        
                return;

            Player player = collision.gameObject.GetComponentInParent<Player>();
            
            if (player != null)
            {
                player.TakeDamage(collision.GetContact(0).point, owner, damageAmount);
            }
        }
    }
}