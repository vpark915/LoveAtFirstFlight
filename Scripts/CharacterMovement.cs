using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CharacterMovement : MonoBehaviour
{
    public bool continueAllowed = false;
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float turnSmoothTime = 0.1f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -18.81f;

    [Header("Dodge Settings")]
    public float dodgeDistance = 5f;
    public float dodgeDuration = 0.3f;
    public float invulnerabilityDuration = 0.5f;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    public CharacterController controller;
    private float turnSmoothVelocity;
    private Vector3 velocity;

    [Header("Health and Stamina")]
    public float health = 20f;
    public float maxStam = 20f;
    public float stam = 20f;
    public float sprintStaminaDrain = 5f; // Stamina per second while sprinting
    public float dodgeStaminaCost = 5f; // Stamina cost per dodge
    public float staminaRegenRate = 3f; // Stamina per second when regenerating
    public float staminaRegenDelay = 2f; // Delay before stamina starts regenerating
    public Image healthBar;
    public Image stamBar;
    public Animator hurtScreenAnimator;

    [Header("Status Effects")]
    public bool isInvulnerable = false;
    public float hitKnockupForce = 10f;
    public float hitSlowDuration = 2f;
    public float hitSlowMultiplier = 0.5f; // 50% of normal speed when slowed

    [Header("Death Settings")]
    public PlayableDirector endGameScreen; // Reference to the EndGameScreen timeline director
    public float deathForceMin = 500f; // Minimum death force
    public float deathForceMax = 1000f; // Maximum death force
    public float deathTorqueMin = 100f; // Minimum death torque
    public float deathTorqueMax = 300f; // Maximum death torque
    public float timelineDelay = 3f; // Delay before playing timeline
    public float knockbackIntensity = 2f;

    [Header("Hitstun Settings")]
    public float hitstunDuration = 1.2f;
    public float hitstunForceMin = 300f; // Minimum hitstun force
    public float hitstunForceMax = 600f; // Maximum hitstun force
    public float hitstunTorqueMin = 50f; // Minimum hitstun torque
    public float hitstunTorqueMax = 150f; // Maximum hitstun torque

    // Private variables for stamina and dodge system
    private float lastStaminaUseTime;
    private bool isDodging = false;
    private Vector3 dodgeDirection;
    private float dodgeTimer;
    private float invulnerabilityTimer;

    // Hit effect variables
    private bool isSlowed = false;
    private float slowTimer = 0f;

    // Death variables
    private bool isDead = false;
    private Rigidbody rb;
    private float deathTimer = 0f;

    // Hitstun variables
    private bool isHitstunned = false;
    private float hitstunTimer = 0f;

    // Audio Clips 
    public AudioSource source;
    public AudioClip enemyHit;

    void OnEnable()
    {
        source = GetComponent<AudioSource>();
        // Get the CharacterController component
        controller = GetComponent<CharacterController>();

        // Get or add Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Initially disable rigidbody and freeze all rotations
        rb.isKinematic = true;
        rb.freezeRotation = true;

        // If no camera is assigned, try to find the main camera
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        // Initialize stamina
        stam = maxStam;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && stam > 0)
        {
            stam -= 10f;
        }
        // Update UI
        UpdateStats();
        // If dead, handle death logic
        if (isDead)
        {
            HandleDeathLogic();
            return;
        }

        // If hitstunned, handle hitstun logic
        if (isHitstunned)
        {
            HandleHitstunLogic();
            UpdateStats();
            return;
        }

        // Update timers
        UpdateTimers();

        // Handle dodge input first (highest priority)
        HandleDodgeInput();

        // If dodging, handle dodge movement
        if (isDodging)
        {
            HandleDodgeMovement();
            UpdateStats();
            return; // Skip normal movement while dodging
        }

        // Ground check using CharacterController
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Get input (WASD or arrow keys)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Calculate current move speed
        float currentMoveSpeed = moveSpeed;

        // Apply slow effect if active
        if (isSlowed)
        {
            currentMoveSpeed *= hitSlowMultiplier;
        }

        // Movement
        if (direction.magnitude >= 0.1f)
        {
            GetComponent<Animator>().SetTrigger("running");
            // Calculate movement direction relative to camera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            // Rotate the character to face movement direction
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Move in the direction the character is facing
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentMoveSpeed * Time.deltaTime);
        }
        else
        {
            GetComponent<Animator>().SetTrigger("idling");
        }

        // Jump input
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Apply vertical movement
        controller.Move(velocity * Time.deltaTime);

        // Handle stamina regeneration
        HandleStaminaRegeneration();
    }

    void HandleDodgeInput()
    {
        if (Input.GetMouseButtonDown(1) && !isDodging && stam >= dodgeStaminaCost)
        {
            // Get current input direction
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            // If no input, dodge backward
            if (inputDirection.magnitude < 0.1f)
            {
                inputDirection = -transform.forward;
            }
            else
            {
                // Convert input direction relative to camera
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                inputDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }

            // Start dodge
            StartDodge(inputDirection);
        }
    }

    void StartDodge(Vector3 direction)
    {
        isDodging = true;
        dodgeDirection = direction.normalized;
        dodgeTimer = dodgeDuration;

        // Activate invulnerability
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;

        // Consume stamina
        stam -= dodgeStaminaCost;
        stam = Mathf.Max(0, stam);
        lastStaminaUseTime = Time.time;

        // Reset vertical velocity during dodge
        velocity.y = 0f;
    }

    void HandleDodgeMovement()
    {
        if (dodgeTimer > 0)
        {
            // Calculate dodge speed (faster at start, slower at end)
            float dodgeProgress = 1f - (dodgeTimer / dodgeDuration);
            float currentDodgeSpeed = dodgeDistance * (1f - dodgeProgress) / dodgeDuration;

            // Move in dodge direction
            Vector3 dodgeMovement = dodgeDirection * currentDodgeSpeed * Time.deltaTime;
            controller.Move(dodgeMovement);

            dodgeTimer -= Time.deltaTime;
        }
        else
        {
            isDodging = false;
        }
    }

    void UpdateTimers()
    {
        // Update invulnerability timer
        if (isInvulnerable && invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
            }
        }

        // Update slow effect timer
        if (isSlowed && slowTimer > 0)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0)
            {
                isSlowed = false;
            }
        }
    }

    void HandleStaminaRegeneration()
    {
        // Only regenerate stamina if enough time has passed since last use
        if (Time.time - lastStaminaUseTime >= staminaRegenDelay && stam < maxStam)
        {
            stam += staminaRegenRate * Time.deltaTime;
            stam = Mathf.Min(maxStam, stam);
        }
    }

    void HandleDeathLogic()
    {
        // Increment death timer
        deathTimer += Time.deltaTime;

        // Check if it's time to play the timeline
        if (!continueAllowed && deathTimer >= timelineDelay && endGameScreen != null && endGameScreen.state != PlayState.Playing)
        {
            endGameScreen.Play();
        }

        if (continueAllowed && Input.GetMouseButtonDown(0))
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }

    void HandleHitstunLogic()
    {
        // Increment hitstun timer
        hitstunTimer += Time.deltaTime;

        // Check if hitstun duration is over
        if (hitstunTimer >= hitstunDuration)
        {
            RecoverFromHitstun();
        }
    }

    void UpdateStats()
    {
        healthBar.fillAmount = health / 20f;
        stamBar.fillAmount = stam / maxStam;
    }

    // Public method to check if character can take damage (for other scripts)
    public bool CanTakeDamage()
    {
        return !isInvulnerable && !isDead && !isHitstunned;
    }

    // Public method to deal damage (for other scripts)
    public void TakeDamage(float damage)
    {
        if (!isInvulnerable && !isDead && !isHitstunned)
        {
            hurtScreenAnimator.SetTrigger("hurt");
            health -= damage;
            health = Mathf.Max(0, health);

            // Check if player died
            if (health <= 0)
            {
                Die();
            }
        }
    }

    // Public method for enemies to hit the player (knockup + slow)
    public void Hit(float damage, Vector3 hitDirection)
    {
        if (!isInvulnerable && !isDead && !isHitstunned)
        {
            GetComponent<AudioSource>().PlayOneShot(enemyHit);
            // Deal damage
            TakeDamage(damage);

            // Only apply hit effects if not dead
            if (!isDead)
            {
                // Apply knockup force
                velocity.y = hitKnockupForce;

                // Apply slow effect
                isSlowed = true;
                slowTimer = hitSlowDuration;

                // Optional: Add slight horizontal knockback
                Vector3 horizontalKnockback = hitDirection.normalized;
                horizontalKnockback.y = 0; // Remove vertical component
                controller.Move(horizontalKnockback * knockbackIntensity * Time.deltaTime);
            }
        }
    }

    // Overload for hit without specific direction (uses random direction)
    public void Hit(float damage)
    {
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;
        Hit(damage, randomDirection);
    }

    // New hitstun function that ragdolls the player temporarily
    public void HitstunHit(float damage)
    {
        if (!isInvulnerable && !isDead && !isHitstunned)
        {
            // Deal damage first
            TakeDamage(damage);

            // Only apply hitstun if not dead
            if (!isDead)
            {
                StartHitstun();
            }
        }
    }

    void StartHitstun()
    {
        isHitstunned = true;
        hitstunTimer = 0f;

        // Disable character controller
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Enable rigidbody physics
        rb.isKinematic = false;
        rb.freezeRotation = false; // Unfreeze all rotation axes

        // Apply random horizontal force (less intense than death)
        Vector3 randomForce = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.1f, 0.5f), // Small upward component
            Random.Range(-1f, 1f)
        ).normalized;

        float forceStrength = Random.Range(hitstunForceMin, hitstunForceMax);
        rb.AddForce(randomForce * forceStrength);

        // Apply random torque for rotation (less intense than death)
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        float torqueStrength = Random.Range(hitstunTorqueMin, hitstunTorqueMax);
        rb.AddTorque(randomTorque * torqueStrength);

        Debug.Log("Player hitstunned! Temporary ragdoll activated.");
    }

    void RecoverFromHitstun()
    {
        isHitstunned = false;

        // Re-enable character controller
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Disable rigidbody physics
        rb.isKinematic = true;
        rb.freezeRotation = true;

        // Reset player rotation to upright
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Reset velocity
        velocity = Vector3.zero;

        // Add brief invulnerability after recovery
        isInvulnerable = true;
        invulnerabilityTimer = 0.5f;

        Debug.Log("Player recovered from hitstun!");
    }

    void Die()
    {
        if (isDead) return; // Prevent multiple death calls

        isDead = true;
        deathTimer = 0f;

        // Disable character controller
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Enable rigidbody physics
        rb.isKinematic = false;
        rb.freezeRotation = false; // Unfreeze all rotation axes

        // Apply random horizontal force
        Vector3 randomForce = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.2f, 0.8f), // Small upward component
            Random.Range(-1f, 1f)
        ).normalized;

        float forceStrength = Random.Range(deathForceMin, deathForceMax);
        rb.AddForce(randomForce * forceStrength);

        // Apply random torque for wild rotation
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        float torqueStrength = Random.Range(deathTorqueMin, deathTorqueMax);
        rb.AddTorque(randomTorque * torqueStrength);

        Debug.Log("Player died! Ragdoll activated.");
    }
}