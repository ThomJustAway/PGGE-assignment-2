using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGGE.Player
{
    public class DamagebyPlayer : MonoBehaviour, IDamageable
    {
        public void TakeDamage()
        {
            print($"{name} have taken damage");
        }
    }

}
