using Unity.Netcode;
using UnityEngine;

public interface IWeapon
{
    void SetOwner(NetworkObject owner);
    void Shoot(Vector3 dir);
    void ShootServerRpc(Vector3 dir);
}
