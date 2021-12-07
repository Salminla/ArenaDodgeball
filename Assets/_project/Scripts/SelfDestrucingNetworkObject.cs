using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SelfDestructingNetworkObject : NetworkBehaviour
{
    public AudioClip PlayAudioOnSpawn;

    protected AudioSource AudioSrc => GetComponent<AudioSource>();
    protected NetworkBehaviour spawner;

    public virtual void Init(float _lifeTime, NetworkBehaviour _spawner)
    {
        Debug.Log("Spawn: " + gameObject.name);
        
        spawner = _spawner;
        if(IsServer)
            NetworkObject.Spawn();

        if (IsServer && _lifeTime > 0)
            StartCoroutine(DestroyDelayed(_lifeTime));
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (AudioSrc != null && PlayAudioOnSpawn != null)
            AudioSrc.PlayOneShot(PlayAudioOnSpawn);
    }

    private IEnumerator DestroyDelayed(float _lifeTime)
    {
        yield return new WaitForSeconds(_lifeTime);
        DespawnDestroy();
    }

    protected void DespawnDestroy()
    {
        if (!NetworkObject.IsSpawned)
            return;

        NetworkObject.Despawn(true);
    }
}