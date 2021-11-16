
using Unity.Netcode;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float sensitivity = 60;
    private float mouseX;
    private float mouseY;
    
    float _xRotation = 0f;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (IsServer)        
            UpdateServer();
        if (IsClient)        
            UpdateClient();  
        //RotateCamera();
    }

    private void UpdateClient()
    {
        if (!IsLocalPlayer)        
            return;
        
        GetRotation();
        
        InputValueServerRpc(_xRotation);
    }

    private void UpdateServer()
    {
        RotateCamera();
    }

    void RotateCamera()
    {
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        playerTransform.Rotate(Vector3.up * mouseX);
    }

    void GetRotation()
    {
        mouseX = Input.GetAxis("Mouse X") * sensitivity;
        mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
    }
    [ServerRpc]
    public void InputValueServerRpc(float rot)
    {
        _xRotation = rot;
    }
}
