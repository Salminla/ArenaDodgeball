using Unity.Netcode;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    public Transform playerTransform;
    [SerializeField] private float sensitivity = 60;
    private float mouseX;
    private float mouseY;

    private float _xRotation;
    private float _yRotation;
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
        RotateCamera();

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void UpdateClient()
    {
        if (!IsLocalPlayer)        
            return;
        
        GetRotation();
        
        InputValueServerRpc(_xRotation, _yRotation);
    }

    private void UpdateServer()
    {
        //RotateCamera();
    }

    void RotateCamera()
    {
        //transform.localRotation = Quaternion.Euler(_xRotation, transform.rotation.eulerAngles.y, 0f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        //transform.Rotate(Vector3.right * _xRotation);
        //transform.Rotate(Vector3.up * mouseX);
        playerTransform.Rotate(Vector3.up * _yRotation);
    }

    void GetRotation()
    {
        mouseX = Input.GetAxis("Mouse X") * sensitivity;
        mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        _yRotation = mouseX;
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
    }
    [ServerRpc]
    public void InputValueServerRpc(float rot, float _mouseX)
    {
        _xRotation = rot;
        _yRotation = _mouseX;
    }
}
