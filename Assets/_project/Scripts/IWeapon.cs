using UnityEngine;

public interface IWeapon
{
    void Shoot(Vector3 dir);
    void ShootServerRpc(Vector3 dir);
}
