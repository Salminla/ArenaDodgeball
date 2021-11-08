using UnityEngine;

public class Cannon : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileSpeed = 20;
    

    public void Shoot(Vector3 dir)
    {
        Debug.Log("Shooting towards " + dir);

        GameObject newProjectile = Instantiate(projectile, transform.position + transform.forward + Vector3.up, Quaternion.identity);
        newProjectile.GetComponent<Rigidbody>().AddForce(dir * projectileSpeed, ForceMode.Impulse);
    }
}
