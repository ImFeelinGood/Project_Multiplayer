using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    public int damage = 30;

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the projectile hits an object
        if (collision.gameObject.CompareTag("Player"))
        {
            // Assuming you want to deal damage to the player
            PlayerInfo targetHealth = collision.gameObject.GetComponent<PlayerInfo>();
            if (targetHealth != null)
            {
                // Deal damage (you may want to adjust this)
                targetHealth.TakeDamage(damage); // Example damage
            }
        }

        // Destroy the projectile upon hitting something
        Destroy(gameObject);
    }
}
