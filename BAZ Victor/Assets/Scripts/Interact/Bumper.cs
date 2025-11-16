using System;
using UnityEngine;

namespace Interact
{
    public class Bumper : MonoBehaviour
    {
        [SerializeField] private float strenght;
        [SerializeField] private bool resetVelocity = false;
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Hider") || other.gameObject.CompareTag("Seeker"))
            {
                Bump(other.gameObject.GetComponent<Rigidbody>());
            }
        }

        private void Bump(Rigidbody rb)
        {
            if (resetVelocity)
            {
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
            }
            rb.AddForce(Vector3.up * strenght ,ForceMode.Impulse);
        }
    }
}
