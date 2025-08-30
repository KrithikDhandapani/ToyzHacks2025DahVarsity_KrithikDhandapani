using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class HeroController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 16f;
    public float sprintSpeed = 16f;
    public float rotationSpeed = 12f;
    public float jumpHeight = 1.4f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;
    public GameObject Effect1;
    public GameObject Effect2;

    [Header("Jump Effects")]
    public GameObject jumpBurstVFX;  

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintHeld;
    private float yVelocity;
    private bool isGrounded;

    private PlayerControls controls;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        
        controls = new PlayerControls();

        // Movement
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        // Look 
        controls.Gameplay.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Look.canceled += ctx => lookInput = Vector2.zero;

        // Sprint
        controls.Gameplay.Sprint.performed += ctx => sprintHeld = ctx.ReadValue<float>() > 0.5f;
        controls.Gameplay.Sprint.canceled += ctx => sprintHeld = false;

        // Jump
        controls.Gameplay.Jump.performed += ctx => TryJump();

        // Powers
        controls.Gameplay.Power1.performed += ctx => DoPower1();
      //  controls.Gameplay.Power2.performed += ctx => DoPower2();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        
        isGrounded = controller.isGrounded;
        if (isGrounded && yVelocity < 0f) yVelocity = -2f;

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 desired = forward * moveInput.y + right * moveInput.x;

        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 horizontal = desired.normalized * targetSpeed;

        
        if (desired.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        
        yVelocity += gravity * Time.deltaTime;

        
        Vector3 velocity = new Vector3(horizontal.x, 0f, horizontal.z);
        velocity.y = yVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Animator params
        if (animator)
        {
            float speedParam = new Vector2(horizontal.x, horizontal.z).magnitude;
            animator.SetFloat("Speed", speedParam);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void TryJump()
    {
        if (!controller.isGrounded) return;

        yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (animator) animator.SetTrigger("Jump");

        // Enable and play jump burst effect 
        if (jumpBurstVFX != null)
        {
            jumpBurstVFX.SetActive(true); 
            ParticleSystem ps = jumpBurstVFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }

            StartCoroutine(DisableJumpBurst()); 
        }
    }

    
    private IEnumerator DisableJumpBurst()
    {
        yield return new WaitForSeconds(1.5f); // match particle duration
        if (jumpBurstVFX != null)
            jumpBurstVFX.SetActive(false);
    }





    private void DoPower1()
    {
        if (animator) animator.SetTrigger("Power1");
        if (Effect1) Effect1.SetActive(true);
        if (Effect2) Effect2.SetActive(false);
    }
    
    private void DoPower2()
    {
        if (animator) animator.SetTrigger("Power2");
        if (Effect2) Effect2.SetActive(true);
        if (Effect1) Effect1.SetActive(false);
    }
    
}
