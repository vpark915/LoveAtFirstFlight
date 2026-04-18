using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButterflyBehavior : MonoBehaviour
{
    // Audio Stuff
    public AudioClip playerHit;
    public GameObject hitPopup;
    public Canvas UICanvas;

    // Details
    public float speed = 4f;
    public float health = 5f;
    public Transform player;
    public Image healthBar;
    public float hitRange = 6f;

    // Charge Attack
    public Transform playerRef;
    public float chargeTimer;
    public bool isCharging = false;
    public float chargeSpeed = 10f;

    // Hitstun 
    public float hitstunTimer;
    public bool inHitstun;
    public float hitstunDuration = 1.5f; // Duration of hitstun in seconds
    public float knockbackForce = 500f;
    public float spinForce = 1000f;

    // Death state
    public bool isDead = false;

    private Rigidbody rb;
    private Collider boxCollider;

    // Rotation smoothing
    public float rotationSpeed = 5f;

    void OnEnable()
    {
        playerHit = Resources.Load<AudioClip>("Music/Hit/Exported/Hit Edit 1 Export 1");
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<Collider>();
    }

    void Update()
    {
        DetectMouseHover();
        // Don't do anything if dead
        if (isDead) return;

        UpdateHealth();
        UpdateHitstun(); // Handle hitstun separately

        if (!inHitstun)
        {
            UpdatePosition();
        }

        HandleClickDetection();
    }

    void UpdateHitstun()
    {
        if (inHitstun)
        {
            hitstunTimer += Time.deltaTime;

            // Check if hitstun duration is over
            if (hitstunTimer >= hitstunDuration)
            {
                inHitstun = false;
                hitstunTimer = 0f; // Reset timer for next hitstun

                // Turn off gravity and stop all physics motion when hitstun ends
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Also add angular drag to help stop any residual spinning
                rb.angularDamping = 10f;
            }
        }
        else
        {
            // Reset angular drag when not in hitstun for normal movement
            rb.angularDamping = 0.05f;
        }
    }

    void UpdatePosition()
    {
        if (isCharging)
        {
            // Smooth rotation towards player during charge
            Vector3 targetDirection = (playerRef.position - transform.position).normalized;
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Move forward towards player
            transform.Translate(Vector3.forward * chargeSpeed * Time.deltaTime);

            chargeTimer += Time.deltaTime;

            if (chargeTimer > 1f)
            {
                isCharging = false;
                chargeTimer = 0f; // Reset charge timer
            }
        }
        else
        {
            // Smooth rotation towards player during normal movement
            Vector3 targetDirection = (player.position - transform.position).normalized;
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Move forward towards player
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            // Random chance to start charging (1% chance per frame)
            if (Random.Range(0, 1000) == 10)
            {
                StartCharge();
            }
        }
    }

    void StartCharge()
    {
        isCharging = true;
        playerRef = player;
        chargeTimer = 0f;
    }

    public void Hit()
    {
        // Don't take damage if already dead
        if (isDead) return;

        if (!inHitstun) // Only trigger hitstun if not already in hitstun
        {
            CreateHitAnim();
            GetComponent<AudioSource>().PlayOneShot(playerHit);
            inHitstun = true;
            hitstunTimer = 0f;

            // Stop charging if hit during charge
            if (isCharging)
            {
                isCharging = false;
                chargeTimer = 0f;
            }

            // Apply knockback force and enable gravity
            Vector3 knockbackDirection = (transform.position - player.position).normalized;
            rb.useGravity = true;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

            // Apply random wild rotation spin
            Vector3 randomSpin = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            rb.AddTorque(randomSpin * spinForce, ForceMode.Impulse);

            // Reduce health
            health -= 1f;

            // Check if dead
            if (health <= 0)
            {
                Die();
            }
        }
    }

    void Die()
    {
        isDead = true;

        // Set collider to not be a trigger so it can collide with ground/objects
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        // Keep gravity on for ragdoll effect
        rb.useGravity = true;

        // Stop any charging behavior
        isCharging = false;
        inHitstun = false;

        Debug.Log(gameObject.name + " has died and is now ragdolling!");
    }

    void HandleClickDetection()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // Sort hits by distance to get the closest one first
            System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                    Debug.Log($"Clicked on: {gameObject.name}, Distance to player: {distanceToPlayer}");

                    if (distanceToPlayer < hitRange)
                    {
                        Hit();
                    }
                    else
                    {
                        Debug.Log("Too far from player to hit!");
                    }
                    break; // Stop checking once we find this butterfly
                }
            }
        }
    }

    public void UpdateHealth()
    {
        healthBar.fillAmount = health / 5f;
    }

    void OnTriggerEnter(Collider other)
    {
        // Don't damage player if dead
        if (isDead) return;

        if (other.GetComponent<CharacterMovement>())
        {
            other.GetComponent<CharacterMovement>().Hit(4f);
        }
    }

    void DetectMouseHover()
    {
        // Don't show outline if dead
        if (isDead)
        {
            if (GetComponent<Outline>() != null)
                GetComponent<Outline>().enabled = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        // Only check for hover if within hit range
        if (distance > hitRange)
        {
            if (GetComponent<Outline>() != null)
                GetComponent<Outline>().enabled = false;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        // Sort hits by distance to prioritize closest objects
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

        bool shouldOutline = false;

        // Check if this butterfly is the first/closest valid target
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform)
            {
                shouldOutline = true;
                break;
            }
            // If we hit another butterfly or interactive object first, don't outline this one
            else if (hit.transform.GetComponent<ButterflyBehavior>() != null)
            {
                break;
            }
        }

        // Apply outline state
        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = shouldOutline;
        }
    }

    void CreateHitAnim()
    {
        Vector3 mousePos = Input.mousePosition;
        GameObject popup = Instantiate(hitPopup, UICanvas.transform);
        popup.GetComponent<RectTransform>().position = mousePos;

        Destroy(popup, 3f); // Destroy after 3 seconds
    }
}