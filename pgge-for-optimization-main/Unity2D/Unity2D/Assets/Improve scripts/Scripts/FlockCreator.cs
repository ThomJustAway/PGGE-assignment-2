using Assets.Improve_scripts.Jobs;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using UnityEngine;

namespace Assets.Improve_scripts.Scripts
{
    public class FlockCreator : MonoBehaviour
    {
        //public GameObject PrefabBoid;

        //#region attributes
        //[Space(10)]
        //[Header("A flock is a group of Automous objects.")]
        //public string name = "Default Name";
        //public bool isPredator = false;
        //public Color colour = new Color(1.0f, 1.0f, 1.0f);
        //public float maxSpeed = 20.0f;
        //public float maxRotationSpeed = 200.0f;
        //public int numberOfBoids { get { return Boids.Count; } }
        //[Space(10)]
        //#endregion

        //#region rule
        //[Header("Flocking Rules")]
        ////help ranging the rule
        //public bool useRandomRule = true;
        //public bool useAlignmentRule = true;
        //public bool useCohesionRule = true;
        //public bool useSeparationRule = true;
        //public bool useFleeOnSightEnemyRule = true;
        //public bool useAvoidObstaclesRule = true;
        //[Space(10)]
        //#endregion

        //#region weight
        //[Header("Rule Weights")]
        ////use cap case to know that this is a constant
        //[Range(0.0f, 1.0f)]
        //public float WEIGHT_RANDOM = 1.0f;
        //[Range(0.0f, 1.0f)]
        //public float WEIGHT_ALIGNMENT;
        //[Range(0.0f, 1.0f)]
        //public float WEIGHT_COHESION;
        //[Range(0.0f, 1.0f)]
        //public float WEIGHT_SEPERATION;
        //[Range(0.0f, 50.0f)]
        //public float WEIGHT_FLEE_ENEMY_ON_SIGHT = 50.0f;
        //[Range(0.0f, 50.0f)]
        //public float WEIGHT_AVOID_OBSTICLES = 10.0f;
        //public float WEIGHT_SPEED = 1.0f;
        //#endregion

        //[Space(10)]
        //[Header("Properties")]
        //#region properties
        //[SerializeField] private float seperationRadius;
        //public float SeparationRadius { get { return seperationRadius; } }

        //[SerializeField] private float alignmentRadius;
        //public float AlignmentRadius { get { return alignmentRadius; } }

        //[SerializeField] private float enemySeperationDistance;
        //public float EnemySeparationDistance { get { return enemySeperationDistance; } }

        ////[SerializeField] private float visibility;
        ////public float Visibility { get { return visibility; } }

        //[SerializeField] private bool bounceWall;
        //public bool BounceWall { get { return bounceWall; } }

        //public Vector3 TotalCohesionPoint { get; private set; } = Vector3.zero;

        ////public Vector2 TotalSumBoidsVelocity { get; private set; } = Vector2.zero;
        ////to know where the flock should combine in the end
        //#endregion

        //public List<Boid> Boids { get; private set; } = new List<Boid>();
        //private DataForJobRule dataForJobRule;
        //public DataForJobRule DataForJobRule { get { return dataForJobRule; } }
        ////check do cohesion as well as spawning of boids

        

        //private void Update()
        //{
        //    UpdateJobRule();
        //    FindCohesionPoint();
        //    ListenToAddBoidsInput();
        //}

        //private void UpdateJobRule()
        //{
        //    //for alignment
        //    dataForJobRule.SeparationRadius = seperationRadius;
        //    dataForJobRule.cohesionPoint = TotalCohesionPoint;
        //    dataForJobRule.AlignmentRadius = alignmentRadius;

        //    //for rules
        //    dataForJobRule.useCohesionRule = useCohesionRule;
        //    dataForJobRule.useAlignmentRule = useAlignmentRule;
        //    dataForJobRule.useSeparationRule = useSeparationRule;

        //    //for weights
        //    dataForJobRule.WEIGHT_ALIGNMENT = WEIGHT_ALIGNMENT;
        //    dataForJobRule.WEIGHT_COHESION = WEIGHT_COHESION;
        //    dataForJobRule.WEIGHT_SEPERATION = WEIGHT_SEPERATION;

        //    NativeArray<BoidData> boidDatas = new NativeArray<BoidData>(Boids.Count , Allocator.TempJob);

        //    for(int i = 0; i < boidDatas.Length; i++)
        //    {
        //        boidDatas[i] = new BoidData(Boids[i].transform.position, Boids[i].velocity);
        //    }
        //    //try temp job first bah
        //}

        //private void ListenToAddBoidsInput()
        //{
        //    if(Input.GetKeyUp(KeyCode.Space))
        //    {
        //        AddBoids();
        //    }
        //}

        ////might probably need to use unity job system here
        //private void FindCohesionPoint()
        //{
        //    Vector3 newCohesionPoint = Vector3.zero;
        //    if (Boids.Count == 0) return;
        //    foreach(var boid in Boids)
        //    {
        //        newCohesionPoint += boid.transform.position;
        //    }
        //    //replacing the values
        //    TotalCohesionPoint = newCohesionPoint;
        //}

        //private void AddBoids()
        //{
        //    for(int i = 0 ; i < FlocksController.Instance.numberOfBoidsToSpawn; i++)
        //    {
        //        GameObject boid = Instantiate(PrefabBoid, transform);
        //        Boid component = boid.GetComponent<Boid>();
        //        Boids.Add(component);
        //        component.Init(this);
        //    }
        //}

        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawSphere(TotalCohesionPoint, 2f);
        //}
    }
}