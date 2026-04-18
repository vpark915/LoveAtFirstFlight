using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Labuboss : MonoBehaviour
{
    public Image healthBar;
    public AudioClip playerHit;
    public AudioClip explosion;
    public GameObject hitPopup;
    public Canvas UICanvas;

    public GameObject player;
    public float dashSpeed = 10f;
    public float attackCooldownMin = 4f;
    public float attackCooldownMax = 5f;
    public int maxHealth = 50;
    public PlayableDirector dialogueFourTimeline; // Reference to the DialogueFour timeline

    private Rigidbody rb;
    private Camera mainCamera;
    private bool isAttacking = false;
    private bool isDashing = false;
    private bool isJumping = false;
    private bool isVulnerable = false; // Can take damage when true
    private bool isRagdolling = false; // New state for ragdoll effect
    public int currentHealth;
    private Vector3 originalPosition;
    private RigidbodyConstraints originalConstraints;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        playerHit = Resources.Load<AudioClip>("Music/Hit/Exported/Hit Edit 1 Export 1");
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Get main camera reference
        mainCamera = Camera.main;
        // Initialize health
        currentHealth = maxHealth;

        // Store original constraints (assuming XZ rotation is locked initially)
        originalConstraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.constraints = originalConstraints;

        // Start the attack cycle
        StartCoroutine(AttackCycle());
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHealthBar();
        // Always look at player when dashing (but not when ragdolling)
        if (isDashing && player != null && !isRagdolling)
        {
            Vector3 direction = (player.transform.position - transform.position).normalized;
            direction.y = 0; // Keep boss upright
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Handle damage input (only when vulnerable and not ragdolling)
        if (!isRagdolling && Input.GetMouseButtonDown(0))
        {
            CheckForDamage();
        }
    }

    private IEnumerator AttackCycle()
    {
        while (true)
        {
            // Wait for cooldown period
            float cooldown = Random.Range(attackCooldownMin, attackCooldownMax);
            yield return new WaitForSeconds(cooldown);

            // Don't attack if ragdolling or already attacking
            if (!isAttacking && !isRagdolling && player != null)
            {
                // Randomly choose attack type
                if (Random.Range(0, 2) == 0)
                {
                    StartCoroutine(DashAttack());
                }
                else
                {
                    StartCoroutine(JumpAttack());
                }
            }
        }
    }

    private IEnumerator DashAttack()
    {
        isAttacking = true;
        isDashing = true;

        // Disable gravity for dash
        rb.useGravity = false;

        Vector3 startPos = transform.position;
        Vector3 targetDirection = (player.transform.position - transform.position).normalized;
        // Keep Y component at 0 to prevent vertical movement
        targetDirection.y = 0;
        targetDirection = targetDirection.normalized;

        // Dash towards player
        float dashDuration = 1.5f;
        float timer = 0f;

        while (timer < dashDuration && isDashing && !isRagdolling)
        {
            Vector3 dashVelocity = targetDirection * dashSpeed;
            dashVelocity.y = 0; // Ensure no Y movement
            rb.linearVelocity = dashVelocity;
            timer += Time.deltaTime;
            yield return null;
        }

        // Stop dash
        rb.linearVelocity = Vector3.zero;
        isDashing = false;

        // Stand still for 3 seconds (vulnerable phase) - unless ragdolling
        if (!isRagdolling)
        {
            isVulnerable = true;
            yield return new WaitForSeconds(3f);
            isVulnerable = false;
        }

        isAttacking = false;
    }

    private IEnumerator JumpAttack()
    {
        isAttacking = true;
        isJumping = true;

        originalPosition = transform.position;

        // Phase 1: Jump up to y = 40
        Vector3 targetPos = new Vector3(transform.position.x, 40f, transform.position.z);
        float jumpDuration = 1f;
        float timer = 0f;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        while (timer < jumpDuration && !isRagdolling)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPos, timer / jumpDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        if (!isRagdolling)
        {
            transform.position = targetPos;
            Vector3 playerXZ = new Vector3(player.transform.position.x, 40f, player.transform.position.z);
            transform.position = playerXZ;

            // Phase 2: Wait 2 seconds, then move to player's X,Z position
            yield return new WaitForSeconds(2f);
        }

        // Phase 3: Enable gravity and ragdoll (vulnerable phase)
        if (!isRagdolling)
        {
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None; // Unlock all rotational axes
            isVulnerable = true;

            // Wait 3 seconds for ragdolling
            yield return new WaitForSeconds(3f);

            // Phase 4: Return to normal
            isVulnerable = false;
            rb.constraints = originalConstraints; // Lock XZ rotation again
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;

            // Reset rotation to upright
            transform.rotation = Quaternion.identity;
        }

        isJumping = false;
        isAttacking = false;
    }

    private void CheckForDamage()
    {
        if (mainCamera == null || player == null) return;

        // Check if player is within distance 5
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > 6f) return;

        // Create ray from camera through mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        // Check if this gameobject was hit by the raycast
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                TakeDamage(5);
                break;
            }
        }
    }

    private void TakeDamage(int damage)
    {
        GetComponent<AudioSource>().PlayOneShot(playerHit);
        CreateHitAnim();
        currentHealth -= damage;
        Debug.Log($"Boss took {damage} damage! Health: {currentHealth}/{maxHealth}");

        // Start ragdoll effect when hit
        if (!isRagdolling && currentHealth > 0)
        {
            StartCoroutine(RagdollEffect());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator RagdollEffect()
    {
        isRagdolling = true;

        // Stop current actions
        isDashing = false;
        isVulnerable = false;

        // Enable gravity and unlock all rotational constraints for ragdoll effect
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        // Add some random force for ragdoll effect
        Vector3 randomForce = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(2f, 5f),
            Random.Range(-5f, 5f)
        );
        rb.AddForce(randomForce, ForceMode.Impulse);

        // Add random torque for spinning effect
        Vector3 randomTorque = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        // Wait for 1.5 seconds
        yield return new WaitForSeconds(1.5f);

        // Get back up
        rb.useGravity = false;
        rb.constraints = originalConstraints; // Restore original constraints
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset rotation to upright
        transform.rotation = Quaternion.identity;

        isRagdolling = false;
    }

    private void Die()
    {
        Debug.Log("Boss defeated!");

        if (dialogueFourTimeline != null)
        {
            // Subscribe to the stopped event before playing
            dialogueFourTimeline.stopped += OnTimelineFinished;
            dialogueFourTimeline.Play();
        }
        else
        {
            Debug.LogWarning("DialogueFour timeline reference is not assigned!");
            // Proceed directly if no timeline
            SceneManager.LoadScene("AirportGoodbye");
        }

        // Disable the boss immediately or you could delay this until after the timeline
        gameObject.SetActive(false);
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        // Unsubscribe to avoid repeated calls
        dialogueFourTimeline.stopped -= OnTimelineFinished;

        // Load the new scene
        SceneManager.LoadScene("AirportGoodbye");
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponent<CharacterMovement>())
        {
            CharacterMovement playerMovement = player.GetComponent<CharacterMovement>();
            if (playerMovement != null)
            {
                playerMovement.HitstunHit(10f);
            }

            // Stop the dash after hitting player
            isDashing = false;
            rb.linearVelocity = Vector3.zero;
        }
    }

    void CreateHitAnim()
    {
        Vector3 mousePos = Input.mousePosition;
        GameObject popup = Instantiate(hitPopup, UICanvas.transform);
        popup.GetComponent<RectTransform>().position = mousePos;

        Destroy(popup, 3f); // Destroy after 3 seconds
    }

    void UpdateHealthBar()
    {
        healthBar.fillAmount = (float) currentHealth / 50f;
    }
}