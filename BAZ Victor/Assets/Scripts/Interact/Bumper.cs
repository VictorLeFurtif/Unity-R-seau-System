using System;
using UnityEngine;

namespace Interact
{
    public class Bumper : MonoBehaviour
    {
        [SerializeField] private float strenght;
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Hider") || other.gameObject.CompareTag("Seeker"))
            {
                Bump(other.gameObject.GetComponent<Rigidbody>());
            }
        }

        private void Bump(Rigidbody rb)
        {
            rb.AddForce(Vector3.up * strenght ,ForceMode.Impulse);
        }
    }
}
