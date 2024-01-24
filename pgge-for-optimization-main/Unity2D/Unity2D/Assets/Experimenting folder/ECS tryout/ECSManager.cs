using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class ECSManager : MonoBehaviour
{
    EntityManager entityManager;
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.CreateEntity();
    }
}
