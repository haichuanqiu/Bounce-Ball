using UnityEngine;
using System.Collections.Generic;

namespace Utils
{
    public class TriggerCollector : MonoBehaviour
    {
        public List<Collider2D> Colliders = new List<Collider2D>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Colliders.Contains(other))
            {
                Colliders.Add(other);
            }
        }

        private void OnDisable()
        {
            Colliders = new List<Collider2D>();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Colliders.Remove(other);
        }
    }
}
