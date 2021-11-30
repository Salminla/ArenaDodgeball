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
    
    private bool playerActive;

    void Update()
    {
        if (playerActive)
        {
            if (IsClient)        
                UpdateClient();
            RotateCamera();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }

    public override void OnNetworkSpawn()
    {
        GameController.GameStarted.OnValueChanged += HandleGameStarted;
    }

    private void HandleGameStarted(bool previousvalue, bool newvalue)
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerActive = true;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && playerActive)
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

    void RotateCamera()
    {
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
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
