
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    
    float _xRotation = 0f;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        RotateCamera();
    }

    void RotateCamera()
    {
        
        float mouseX = Input.GetAxis("Mouse X") * 60 * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * 60 * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        playerTransform.Rotate(Vector3.up * mouseX);
    }
}
