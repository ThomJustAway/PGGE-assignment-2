using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace experimenting 
{
    public class BoidDebugger : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            print(transform.position);
            if (float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y) || float.IsNaN(transform.position.z))
            {
                Debug.LogError("NaN values detected in transform position. Resetting position.");

                // Reset the position to a valid value or destroy the GameObject
                // Example: transform.position = Vector3.zero;
                // Or: Destroy(gameObject);
            }
        }
    }

}
