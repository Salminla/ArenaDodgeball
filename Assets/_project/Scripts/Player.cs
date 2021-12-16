using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [SerializeField] private GameObject playerUI;
    [SerializeField] private TMP_Text healthText;
    public GameObject hitMarker;

    // TEST
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private GameObject fpsWeapon;
    [SerializeField] private GameObject playerBody;
    
    public float inputAcceleration = 1f;
    public float inputDeceleration = 2f;
    
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes(""));
    public NetworkVariable<int> Health = new NetworkVariable<int>(30);
    public NetworkVariable<int> Ammo = new NetworkVariable<int>(2);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    
    private Vector3 rawInput;
    private Vector3 input;
    private float xSmoothed;
    private float ySmoothed;
    private Rigidbody rb;

    private bool isGrounded;
    private bool sprint;
    private bool hasInput;
    private bool allowShooting = true;

    private bool playerActive;
    
    void Start()
    {
        // Application.targetFrameRate = 144;
        
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            playerCamera.enabled = true;
            fpsWeapon.SetActive(true);
            playerBody.SetActive(false);
            playerUI.transform.SetParent(null);
            if (healthText != null)
                healthText.text = "Health " + Health.Value;
        }
         
        if (IsServer)
        {
            playerName.Value = "Player " + (OwnerClientId + 1);
            UpdateWeapon();
            //NetworkObjectPool.Singleton.InitializePool();
        }
        else
        {
            SetNameClientRpc("Player " + (OwnerClientId + 1));
            UpdateWeaponServerRpc();
        }
        
        GameController.GameStarted.OnValueChanged += HandleGameStarted;
    }

    void Update()
    {
        if (!playerActive) return;
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
        
        healthText.text = "Health " + Health.Value;
        
        if (Input.GetKeyDown(KeyCode.Mouse0))
            ShootServerRpc();
    }

    private void HandleGameStarted(bool _old, bool _new)
    {
        playerActive = _new;
    }

    private void UpdateWeapon()
    {
        if (transform.GetComponentInChildren<IWeapon>() != null)
        {
            IWeapon weapon = transform.GetComponentInChildren<IWeapon>();
            weapon.SetOwner(transform.GetComponent<NetworkObject>());
            return;
        }
    }
    void SetInputVector()
    {
        //rawInput.x = InputSmoothing("Horizontal", ref xSmoothed);
        //rawInput.z = InputSmoothing("Vertical", ref ySmoothed);
        rawInput.x = Input.GetAxisRaw("Horizontal");
        rawInput.z = Input.GetAxisRaw("Vertical");
        
        Transform playerTransform = transform;
        //var forward = playerTransform.forward;
        var forward = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z) ;
        var right = playerCamera.transform.right;
        
        input = (forward * rawInput.z) + (right * rawInput.x);
        input = Vector3.ClampMagnitude(input, 1f);

        //playerAnimator.SetBool("HasInput", input.magnitude > 0.01f);
        AnimationBoolServerRpc("HasInput", input.magnitude > 0.01f);
        //playerAnimator.SetFloat("PlayerVel", rb.velocity.magnitude / 10f);
        AnimationFloatServerRpc("PlayerVel", rb.velocity.magnitude / 10f);
        
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
    void Jump()
    {
        rb.AddForce(Vector3.up * (jumpForce - rb.velocity.y * 0.5f), ForceMode.Impulse);
        //playerAnimator.SetTrigger("JumpTrig");
        AnimationTriggerServerRpc("JumpTrig");
    }
    void Shoot()
    {
        if (transform.GetComponentInChildren<IWeapon>() != null)
        {
            IWeapon weapon = transform.GetComponentInChildren<IWeapon>();
            weapon.Shoot(playerCamera.transform.forward, OwnerClientId);
            
            //playerAnimator.SetTrigger("Shoot");
            AnimationTriggerServerRpc("Shoot");
            return;
        }

        Debug.Log("No weapon!");
    }
    public void TakeDamage(Vector3 _hitPoint, ulong from ,int _amount)
    {
        // Instantiate<SelfDestructingNetworkObject>(HitExplosionEffect, _hitPoint, Quaternion.identity).Init(3f);
        if (OwnerClientId != from)
        {
            Health.Value = Health.Value - _amount;
            
            var client = NetworkManager.Singleton.ConnectedClients[from];
            StartCoroutine(ShowHitMarker(client.PlayerObject.GetComponent<Player>(), 0.1f));
        }
        
        if (Health.Value <= 0)
        {
            //Debug.Log(playerName.Value + " killed " + from.GetComponent<Player>().playerName.Value);
            //from.GetComponent<Player>().Score.Value++;
            var client = NetworkManager.Singleton.ConnectedClients[from];
            client.PlayerObject.GetComponent<Player>().Score.Value++;
            
            Health.Value = 0;

            //"Ulkoistettu" CheckPlayerStatusServerRpc käy läpi kaikkien tilan.
            //Tässä voisi pitää yksinkertaisempaakin kirjanpitoa siitä, kuinka
            //moni pelaajista on kuollut ja pitäisikö peli päättää. Mutta pelistä
            //riippuen voi olla tarpeen tarkastella ja päivittää arvoja kaikkien
            //pelaajien osalta, johon voi käyttää tämän tyylistä ideaa.
            GameController.Instance.CheckPlayerStatusServerRpc(false);
        }
    }
    void MovePlayer()
    {
        // Ground movement
        if (isGrounded)
        {

            Vector3 direction = input;
            direction.Normalize();
 
            rb.AddForce(direction * walkSpeed * 10 * Time.deltaTime, ForceMode.Acceleration);
 
            if (rb.velocity.magnitude > 8) 
            {
                rb.velocity = rb.velocity.normalized * 8;
            }

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

    IEnumerator ShowHitMarker(Player player, float time)
    {
        player.hitMarker.SetActive(true);
        yield return new WaitForSeconds(time);
        player.hitMarker.SetActive(false);
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
    [ServerRpc]
    public void UpdateWeaponServerRpc()
    {
        UpdateWeapon();
    }
    // Animation triggers done with server RPC
    [ServerRpc]
    void AnimationTriggerServerRpc(string trigger)
    {
        playerAnimator.SetTrigger(trigger);
    }
    [ServerRpc]
    void AnimationFloatServerRpc(string trigger, float value)
    {
        playerAnimator.SetFloat(trigger, value);
    }
    [ServerRpc]
    void AnimationBoolServerRpc(string trigger, bool value)
    {
        playerAnimator.SetBool(trigger, value);
    }
    [ClientRpc]
    public void SetNameClientRpc(string name)
    {
        playerName.Value = name;
    }
}
