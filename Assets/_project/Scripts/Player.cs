using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float walkSpeed = 500f;
    [SerializeField] private float sprintSpeed = 800f;
    [SerializeField] private float jumpForce = 10f;
    public float inputAcceleration = 1f;
    public float inputDeceleration = 2f;
    private Vector3 rawInput;
    private Vector3 input;
    private float xSmoothed;
    private float ySmoothed;
    private Rigidbody rb;

    private bool isGrounded;
    private bool sprint;
    
    void Start()
    {
        Application.targetFrameRate = 144;
        
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        isGrounded = IsGrounded();
        if (IsOwner)
        {
            SetInputVector();
        }
        
        
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }
 
    void SetInputVector()
    {
        rawInput.x = InputSmoothing("Horizontal", ref xSmoothed);
        rawInput.z = InputSmoothing("Vertical", ref ySmoothed);
        
        Transform playerTransform = transform;
        var forward = playerTransform.forward;
        var right = playerTransform.right;
        
        input = (forward * rawInput.z) + (right * rawInput.x);
        input = Vector3.ClampMagnitude(input, 1f);
        
        Sprint();
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            Jump();
        if (Input.GetKeyDown(KeyCode.Mouse0))
            Shoot();

    }
    public float InputSmoothing(string axis, ref float smoothed)
    {
     
            var accelerating = Input.GetAxisRaw(axis);

            if (accelerating > 0)
                smoothed = Mathf.Clamp(smoothed + inputAcceleration * Time.deltaTime, -1f, accelerating);
            else if (accelerating < 0)
                smoothed = Mathf.Clamp(smoothed - inputAcceleration * Time.deltaTime, accelerating, 1f);
            else
                smoothed = Mathf.Clamp01(Mathf.Abs(smoothed) - inputDeceleration * Time.deltaTime) * Mathf.Sign(smoothed);
            return smoothed;
        
    }
    void Sprint()
    {
        sprint = Input.GetKey(KeyCode.LeftShift);
    }
    void Jump()
    {
        rb.AddForce(Vector3.up * (jumpForce - rb.velocity.y * 0.5f), ForceMode.Impulse);
    }
    void Shoot()
    {
        if (transform.GetComponentInChildren<IWeapon>() != null)
        {
            IWeapon weapon = transform.GetComponentInChildren<IWeapon>();
            weapon.Shoot(playerCamera.transform.forward);
            return;
        }

        Debug.Log("No weapon!");
    }
    void MovePlayer()
    {
        if (isGrounded)
        {
            if (!sprint)
                rb.velocity = input * ((walkSpeed - Mathf.Clamp(rb.velocity.y * 40, 0, walkSpeed)) * Time.deltaTime) + rb.velocity.y * Vector3.up;
            else
                rb.velocity = input * ((sprintSpeed - Mathf.Clamp(rb.velocity.y * 40, 0, sprintSpeed)) * Time.deltaTime) + rb.velocity.y * Vector3.up;
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckCapsule(transform.position + Vector3.up * 0.5f, 
            transform.position + Vector3.down * 0.7f, 
            0.40f, 
            LayerMask.GetMask("Default"));
    }
}
