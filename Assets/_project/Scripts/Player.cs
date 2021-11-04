using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float walkSpeed = 500f;
    [SerializeField] private float sprintSpeed = 800f;
    [SerializeField] private float jumpForce = 10f;

    private Vector3 rawInput;
    private Vector3 input;
    private Rigidbody rb;

    private bool sprint;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        SetInputVector();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    void SetInputVector()
    {
        rawInput.x = Input.GetAxis("Horizontal");
        rawInput.z = Input.GetAxis("Vertical");
        
        Transform playerTransform = transform;
        var forward = playerTransform.forward;
        var right = playerTransform.right;
        
        input = (forward * rawInput.z) + (right * rawInput.x);
        input = Vector3.ClampMagnitude(input, 1f);
        
        Sprint();
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Shoot();
        }

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
    void Sprint()
    {
        sprint = Input.GetKey(KeyCode.LeftShift);

    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    void MovePlayer()
    {
        if (!sprint)
            rb.velocity = input * walkSpeed * Time.deltaTime + rb.velocity.y * Vector3.up;
        else
            rb.velocity = input * sprintSpeed * Time.deltaTime + rb.velocity.y * Vector3.up;

    }
}
