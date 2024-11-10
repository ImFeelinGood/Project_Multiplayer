using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private float shootForce;
    private Rigidbody rb;

    void Start()
    {
        // Reference for the rigidbody
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Move the fireball forward based on the player facing direction
        rb.velocity = transform.forward * shootForce;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Instantiate the hit particles when we collide with something then destroy the fireball
        GameObject hitImpact = Instantiate(hitParticles, transform.position, Quaternion.identity);
        hitImpact.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
        Destroy(gameObject);

    }
}