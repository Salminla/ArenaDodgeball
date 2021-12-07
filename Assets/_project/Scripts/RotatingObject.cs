using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RotatingObject : NetworkBehaviour

{
    private float rotateSpeed = 90f;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
    }
}
