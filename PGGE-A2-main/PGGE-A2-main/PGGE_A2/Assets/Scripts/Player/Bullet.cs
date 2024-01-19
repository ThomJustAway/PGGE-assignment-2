using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class used by the bullet. Used to hit object in contact.
public class Bullet : MonoBehaviour
{
    void Start()
    {
        // Destroy the bullet after 10 seconds if it does not hit any object.
        StartCoroutine(Coroutine_Destroy(10.0f));
    }

    void Update()
    {
    }

    IEnumerator Coroutine_Destroy(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    //if the bullet hits an object, do the following here.
    private void OnCollisionEnter(Collision collision)
    {
        //try and see if the object is able to take damage
        IDamageable obj = collision.gameObject.GetComponent<IDamageable>();
        if (obj != null)
        {//if it is, then ask it to start doing the function to take damage.
            obj.TakeDamage();
        }

        //decrease the time taken to destory the object, This is for testing purpose
        StartCoroutine(Coroutine_Destroy(5f));
    }
}
