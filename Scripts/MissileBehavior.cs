using UnityEngine;

public class MissileBehavior : MonoBehaviour
{
    [Header("Missile Settings")]
    public float speed = 2f;
    public float lifetime = 10f; // Time in seconds before missile destroys itself
    public GameObject player;
    public GameObject parentBoss;

    void OnEnable()
    {
        player = GameObject.FindWithTag("Player");
        parentBoss = GameObject.FindWithTag("CoffeeBoss");
        // Destroy the missile after the specified lifetime
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (parentBoss.GetComponent<CoffeeMonster>().isDead)
        {
            Destroy(gameObject);
        }
        if (player != null)
        {
            // Calculate direction to player
            Vector3 direction = (player.transform.position - transform.position).normalized;

            // Rotate to face player
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
                transform.rotation *= Quaternion.Euler(0, 90f, 0);
            }

            // Move towards player
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if collided with player
        if (other.gameObject == player)
        {
            // Get CharacterMovement component and call hit method
            CharacterMovement characterMovement = other.GetComponent<CharacterMovement>();
            if (characterMovement != null)
            {
                characterMovement.Hit(100f);
            }
            else
            {
                Debug.LogWarning("MissileBehavior: CharacterMovement component not found on player!");
            }

            // Destroy the missile after hitting
            Destroy(gameObject);
        }
    }
}