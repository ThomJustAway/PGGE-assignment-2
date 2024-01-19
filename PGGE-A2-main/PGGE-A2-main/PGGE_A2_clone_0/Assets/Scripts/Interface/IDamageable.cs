using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//simple interface for scripts to inherit so that it can take damage
public interface IDamageable
{
    //Function called once a bullet hits an object.
    void TakeDamage();
}
