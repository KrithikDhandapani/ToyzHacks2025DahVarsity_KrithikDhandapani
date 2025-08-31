using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class HeroController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 12f;
    public float rotationSpeed = 12f;
    public float jumpHeight = 1.4f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;
    private CharacterController controller;

    [Header("Projectile Settings (Power1)")]
    public Transform muzzle;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    public float shootDelay = 0.3f;
    public float angleOffset = 0f;

    [Header("Hover Settings (Power2)")]
    public float hoverDuration = 2f;
    public float hoverHeight = 2f;
    public GameObject hoverSmokePrefab;
    public ParticleSystem hoverGlowEffect1;
    public ParticleSystem hoverGlowEffect2;

    [Header("Jump Effects")]
    public GameObject jumpBurstVFX;

    // Private state
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintHeld;
    private float yVelocity;
    private bool isGrounded;
    private PlayerControls controls;

    private GameObject activeHoverSmoke;

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
        controls.Gameplay.Power2.performed += ctx => DoPower2();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        // --- Movement & Gravity ---
        isGrounded = controller.isGrounded;
        if (isGrounded && yVelocity < 0f) yVelocity = -2f;

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 desired = forward * moveInput.y + right * moveInput.x;

        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 horizontal = desired.normalized * targetSpeed;

        // Rotate smoothly
        if (desired.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Gravity
        yVelocity += gravity * Time.deltaTime;

        // Apply movement
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

    // --- Jump ---
    private void TryJump()
    {
        if (!controller.isGrounded) return;

        yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (animator) animator.SetTrigger("Jump");

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
        yield return new WaitForSeconds(1.5f);
        if (jumpBurstVFX != null)
            jumpBurstVFX.SetActive(false);
    }

    // --- Power1: Projectile ---
    private void DoPower1()
    {
        if (animator) animator.SetTrigger("Power1");
        StartCoroutine(DelayedShoot());
    }

    private IEnumerator DelayedShoot()
    {
        yield return new WaitForSeconds(shootDelay);

        if (projectilePrefab == null || muzzle == null) yield break;

        Quaternion rotation = muzzle.rotation * Quaternion.Euler(0f, angleOffset, 0f);
        GameObject proj = Instantiate(projectilePrefab, muzzle.position, rotation);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = rotation * Vector3.forward * projectileSpeed;

        // Ignore collision with player
        Collider projCol = proj.GetComponent<Collider>();
        Collider playerCol = GetComponent<Collider>();
        if (projCol != null && playerCol != null)
            Physics.IgnoreCollision(projCol, playerCol);
    }

    // --- Power2: Hover ---
    private void DoPower2()
    {
        if (animator) animator.SetTrigger("Power2");
        StartCoroutine(HoverRoutine());
    }

    private IEnumerator HoverRoutine()
    {
        // Smoke
        if (hoverSmokePrefab)
        {
            activeHoverSmoke = Instantiate(hoverSmokePrefab, transform.position, Quaternion.identity);
            activeHoverSmoke.transform.parent = transform;
        }

        // Glow effects
        if (hoverGlowEffect1)
        {
            hoverGlowEffect1.gameObject.SetActive(true);
            hoverGlowEffect1.Play();
        }
        if (hoverGlowEffect2)
        {
            hoverGlowEffect2.gameObject.SetActive(true);
            hoverGlowEffect2.Play();
        }

        yield return new WaitForSeconds(hoverDuration);

        // Clean up
        if (activeHoverSmoke) Destroy(activeHoverSmoke);

        if (hoverGlowEffect1)
        {
            hoverGlowEffect1.Stop();
            hoverGlowEffect1.gameObject.SetActive(false);
        }
        if (hoverGlowEffect2)
        {
            hoverGlowEffect2.Stop();
            hoverGlowEffect2.gameObject.SetActive(false);
        }
    }
}
