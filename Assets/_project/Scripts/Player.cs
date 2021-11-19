using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    //[SerializeField] private MouseLook mouseLook;
    [SerializeField] private float walkSpeed = 500f;
    [SerializeField] private float sprintSpeed = 800f;
    [SerializeField] private float jumpForce = 10f;
    public float inputAcceleration = 1f;
    public float inputDeceleration = 2f;
    
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes(""));
    
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

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            playerCamera.enabled = true;
        }
         
        if (IsServer)
        {
            playerName.Value = "Player " + (OwnerClientId + 1);
        }
        else
        {
            SetNameClientRpc("Player " + (OwnerClientId + 1));
        }
    }

    void Update()
    {
        isGrounded = IsGrounded();
        if (IsServer)        
            UpdateServer();
        if (IsClient)        
            UpdateClient();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }
    private void UpdateServer()
    {
        //MovePlayer();
    }
    private void UpdateClient()
    {
        if (!IsLocalPlayer)        
            return;

        SetInputVector();

        InputValueServerRpc(input);

        if (Input.GetKeyDown(KeyCode.Mouse0))
            ShootServerRpc();
    }
    void SetInputVector()
    {
        rawInput.x = InputSmoothing("Horizontal", ref xSmoothed);
        rawInput.z = InputSmoothing("Vertical", ref ySmoothed);
        
        Transform playerTransform = transform;
        //var forward = playerTransform.forward;
        var forward = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z) ;
        var right = playerCamera.transform.right;
        
        input = (forward * rawInput.z) + (right * rawInput.x);
        input = Vector3.ClampMagnitude(input, 1f);
        
        Sprint();
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            Jump();
    }
    // Make this related to the player velocity and direction?
    float InputSmoothing(string axis, ref float smoothed)
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
        Debug.Log("JUMP");
        rb.AddForce(Vector3.up * (jumpForce - rb.velocity.y * 0.5f), ForceMode.Impulse);
        //rb.velocity += Vector3.up * (jumpForce - rb.velocity.y * 0.5f);
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
        // Ground movement
        if (isGrounded)
        {
            if (!sprint)
                rb.velocity = input * ((walkSpeed - Mathf.Clamp(rb.velocity.y * 40, 0, walkSpeed)) * Time.deltaTime) +
                              rb.velocity.y * Vector3.up;
            else
                rb.velocity =
                    input * ((sprintSpeed - Mathf.Clamp(rb.velocity.y * 40, 0, sprintSpeed)) * Time.deltaTime) +
                    rb.velocity.y * Vector3.up;
            return;
        }
        // Air movement
        rb.AddForce(input * 5); 
    }

    bool IsGrounded()
    {
        return Physics.CheckCapsule(transform.position + Vector3.up * 0.5f, 
            transform.position + Vector3.down * 0.7f, 
            0.40f, 
            LayerMask.GetMask("Default"));
    }
    [ServerRpc]
    public void InputValueServerRpc(Vector3 move)
    {
        input = move;
    }
    [ServerRpc]
    public void ShootServerRpc()
    {
        Shoot();
    }
    [ClientRpc]
    public void SetNameClientRpc(string name)
    {
        playerName.Value = name;
    }
}
