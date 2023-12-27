using Assets.Improve_scripts.Jobs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockBehaviourV4 : MonoBehaviour
{
    //use compute shaders this time round
    public GameObject PrefabBoid;

    #region attributes
    [Space(10)]
    [Header("A flock is a group of Automous objects.")]
    public string name = "Default Name";
    public bool isPredator = false;
    public Color colour = new Color(1.0f, 1.0f, 1.0f);
    public float maxSpeed = 20.0f;
    public float maxRotationSpeed = 200.0f;
    [Space(10)]
    #endregion

    [SerializeField] private DataRule rules;
    [SerializeField] private ComputeShader CalculateBoid;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
