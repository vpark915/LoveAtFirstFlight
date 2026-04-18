using UnityEngine;
using UnityEngine.UI;

public class CoffeeMonster : MonoBehaviour
{

    public AudioClip playerHit;
    public AudioClip explosion;
    public GameObject hitPopup;
    public Canvas UICanvas;
    
    [Header("Health")]
    public float health = 10f;
    public Image healthBar;

    [Header("Combat")]
    public float hitRange = 3f;
    public float knockbackForce = 10f;
    public float spinForce = 15f;

    [Header("Hitstun")]
    public bool inHitstun = false;
    public bool isDead = false;
    public float hitstunDuration = 2f;
    public float hitstunTimer = 0f;

    [Header("Missiles")]
    public Vector3[] spawnPosition = new Vector3[] { new Vector3(1.130867f, -0.09f, 1.339f), new Vector3(1.13f, -0.09f, -1.21f) };
    public Quaternion spawnRotation = Quaternion.Euler(new Vector3(0, 180, 0));
    public float missileSpawnTimer = 0f;
    public float missileSpawnInterval = 4f;
    public float flurryChance = 0.2f; // 20% chance for flurry
    public int flurryMissileCount = 5;
    public float flurryInterval = 0.3f;
    private bool isInFlurry = false;
    private int flurryMissilesLeft = 0;
    private float flurryTimer = 0f;

    [Header("References")]
    public GameObject player;

    private Rigidbody rb;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void OnEnable()
    {
        playerHit = Resources.Load<AudioClip>("Music/Hit/Exported/Hit Edit 1 Export 1");
        spawnPosition = new Vector3[] { new Vector3(1.130867f, -0.09f, 1.339f), new Vector3(1.13f, -0.09f, -1.21f) };
        rb = GetComponent<Rigidbody>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Lock position and rotation constraints initially
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ |
                        RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (isDead) return;

        UpdateHealth();
        UpdateHitstun(); // Handle hitstun separately

        if (!inHitstun)
        {
            StayRotating();
            HandleMissileSpawning();
            HandleClickDetection();
        }
    }

    void HandleMissileSpawning()
    {
        if (isInFlurry)
        {
            flurryTimer += Time.deltaTime;
            if (flurryTimer >= flurryInterval && flurryMissilesLeft > 0)
            {
                SpawnMissile();
                flurryMissilesLeft--;
                flurryTimer = 0f;

                if (flurryMissilesLeft <= 0)
                {
                    isInFlurry = false;
                    missileSpawnTimer = 0f; // Reset normal missile timer
                }
            }
        }
        else
        {
            missileSpawnTimer += Time.deltaTime;
            if (missileSpawnTimer >= missileSpawnInterval)
            {
                // Check for flurry chance
                if (Random.Range(0f, 1f) < flurryChance)
                {
                    StartFlurry();
                }
                else
                {
                    SpawnMissile();
                    missileSpawnTimer = 0f;
                }
            }
        }
    }

    void StartFlurry()
    {
        isInFlurry = true;
        flurryMissilesLeft = flurryMissileCount;
        flurryTimer = 0f;
        Debug.Log("Coffee Monster starting missile flurry!");
    }

    void SpawnMissile()
    {
        GetComponent<AudioSource>().PlayOneShot(explosion);
        GameObject prefab = Resources.Load<GameObject>("Missile");
        if (prefab != null)
        {
            GameObject missile = Instantiate(prefab, transform);
            missile.transform.localPosition = spawnPosition[Random.Range(0, 2)];
            missile.transform.localRotation = Quaternion.identity;
            missile.transform.SetParent(null);

            if (missile.GetComponent<MissileBehavior>() == null)
            {
                missile.AddComponent<MissileBehavior>();
            }
        }
        else
        {
            Debug.LogError("missilePrefab not found in Resources.");
        }
    }

    void StayRotating()
    {
        Vector3 targetPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        transform.LookAt(targetPosition);

        // Apply -90 degrees offset on the Y axis
        transform.rotation *= Quaternion.Euler(0, -90f, 0);
    }

    public void Hit()
    {
        // Don't take damage if already dead
        if (isDead) return;

        if (!inHitstun) // Only trigger hitstun if not already in hitstun
        {
            GetComponent<AudioSource>().PlayOneShot(playerHit);
            CreateHitAnim();
            health -= 2f;
            GetComponent<BoxCollider>().isTrigger = false;
            inHitstun = true;
            hitstunTimer = 0f;

            // Stop flurry if in progress
            if (isInFlurry)
            {
                isInFlurry = false;
                flurryMissilesLeft = 0;
            }

            // Unlock constraints for physics during hitstun
            rb.constraints = RigidbodyConstraints.None;

            // Apply knockback force and enable gravity
            Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
            rb.useGravity = true;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

            // Apply random wild rotation spin
            Vector3 randomSpin = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            rb.AddTorque(randomSpin * spinForce, ForceMode.Impulse);

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

        // Keep gravity on for ragdoll effect and unlock all constraints
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

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
                    break;
                }
            }
        }
    }

    void UpdateHitstun()
    {
        if (inHitstun)
        {
            hitstunTimer += Time.deltaTime;

            // Check if hitstun duration is over
            if (hitstunTimer >= hitstunDuration)
            {
                GetComponent<BoxCollider>().isTrigger = true;
                inHitstun = false;
                hitstunTimer = 0f; // Reset timer for next hitstun

                // Turn off gravity and stop all physics motion when hitstun ends
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Reset position and rotation to original/upright
                transform.position = new Vector3(transform.position.x, originalPosition.y, transform.position.z);
                transform.rotation = originalRotation;

                // Re-lock position and rotation constraints
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ |
                                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                // Reset angular drag
                rb.angularDamping = 0.05f;
                StartFlurry();

                Debug.Log("Coffee Monster recovered from hitstun and is standing upright!");
            }
            else
            {
                // Add angular drag to help stop any residual spinning during hitstun
                rb.angularDamping = 10f;
            }
        }
        else
        {
            // Reset angular drag when not in hitstun for normal movement
            rb.angularDamping = 0.05f;
        }
    }

    public void UpdateHealth()
    {
        healthBar.fillAmount = health / 20f;
    }

    void CreateHitAnim()
    {
        Vector3 mousePos = Input.mousePosition;
        GameObject popup = Instantiate(hitPopup, UICanvas.transform);
        popup.GetComponent<RectTransform>().position = mousePos;

        Destroy(popup, 3f); // Destroy after 3 seconds
    }
}