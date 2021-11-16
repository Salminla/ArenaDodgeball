using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public List<Transform> spawnPoints;
    
    void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApproval;
    }
    Transform GetSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
    void ConnectionApproval(byte[] payload, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Transform newSpawn = GetSpawnPoint();
        callback(true, null, true, newSpawn.position, newSpawn.rotation);
    }
}
