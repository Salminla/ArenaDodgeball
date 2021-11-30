using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public List<Transform> spawnPoints;
    private int spawnIndex;
    void Start()
    {
        //NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApproval;
    }
    Transform GetSpawnPoint()
    {
        Transform newSpawn = spawnIndex != 0 ? spawnPoints[0] : spawnPoints[spawnIndex];

        spawnIndex++;
        return newSpawn;
    }
    void ConnectionApproval(byte[] payload, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Transform newSpawn = GetSpawnPoint();
        callback(true, null, true, newSpawn.position, newSpawn.rotation);
    }
}
