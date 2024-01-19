using origin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlockBehaviour : MonoBehaviour
{
  List<Obstacle> mObstacles = new List<Obstacle>();

  [SerializeField]
  GameObject[] Obstacles;

  [SerializeField]
  BoxCollider2D Bounds;

  public float TickDuration = 1.0f;
  public float TickDurationSeparationEnemy = 0.1f;
  public float TickDurationRandom = 1.0f;

  public int BoidIncr = 100; //the number of boid to spawn
  public bool useFlocking = false;
  public int BatchSize = 100;

  public List<Flock> flocks = new List<Flock>();
  void Reset()
  {
    flocks = new List<Flock>()
    {
      new Flock()
    };
  }

  void Start()
    {
        SetObstacles();

        foreach (Flock flock in flocks)
        {
            CreateFlock(flock);
        }

        //probably have to change this to something that runs
        //on the worker thread using burst compile plus job system
        StartCoroutine(Coroutine_Flocking());
        StartCoroutine(Coroutine_Random());
        StartCoroutine(Coroutine_AvoidObstacles());
        StartCoroutine(Coroutine_SeparationWithEnemies());
        StartCoroutine(Coroutine_Random_Motion_Obstacles());
    }

    private void SetObstacles()
    {
        // Randomize obstacles placement.
        for (int i = 0; i < Obstacles.Length; ++i)
        {
            float x = Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

            Obstacles[i].transform.position = new Vector3(x, y, 0.0f); //set position of the obstacles

            Obstacle obs = Obstacles[i].AddComponent<Obstacle>();
            Autonomous autono = Obstacles[i].AddComponent<Autonomous>();
            //add the the obstacle and autonomouse component to the game

            autono.MaxSpeed = 1.0f;
            obs.mCollider = Obstacles[i].GetComponent<CircleCollider2D>();

            mObstacles.Add(obs); //add collider for reference for the boid?
        }
    }

    void CreateFlock(Flock flock)
    {

    for(int i = 0; i < flock.numBoids; ++i)
    {
        float x = Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
        float y = Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

        AddBoid(x, y, flock);
    }
    }//they are the groups of boid (flock)

    void Update()
  {
    HandleInputs();
    Rule_CrossBorder();
    Rule_CrossBorder_Obstacles();
  }

    void HandleInputs()
    {
    if (EventSystem.current.IsPointerOverGameObject() ||
        enabled == false)
    {
        return;
    }

    if (Input.GetKeyDown(KeyCode.Space))
    {
        AddBoids(BoidIncr);
    }
    }

    void AddBoids(int count)
    {
    for(int i = 0; i < count; ++i)
    {
        float x = Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
        float y = Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

        AddBoid(x, y, flocks[0]);
    }
    flocks[0].numBoids += count;
    }

    void AddBoid(float x, float y, Flock flock)
    {
        GameObject obj = Instantiate(flock.PrefabBoid);
        obj.name = "Boid_" + flock.name + "_" + flock.mAutonomous.Count;
        obj.transform.position = new Vector3(x, y, 0.0f);
        Autonomous boid = obj.GetComponent<Autonomous>();

        flock.mAutonomous.Add(boid); //add the autonomous component into the flock component

        boid.MaxSpeed = flock.maxSpeed; //set the speed and rotation
        boid.RotationSpeed = flock.maxRotationSpeed;
    }

    static float Distance(Autonomous a1, Autonomous a2)
    {
        return (a1.transform.position - a2.transform.position).magnitude;
    }

    void Execute(Flock flock, int i)
    {
        Vector3 flockDir = Vector3.zero;
        Vector3 separationDir = Vector3.zero;
        Vector3 cohesionDir = Vector3.zero;

        float speed = 0.0f;
        float separationSpeed = 0.0f;

        int count = 0;
        int separationCount = 0;
        Vector3 steerPos = Vector3.zero;

        Autonomous curr = flock.mAutonomous[i];

        for (int j = 0; j < flock.numBoids; ++j)
        {
            if (i != j) continue; //if not the same then move on

            Autonomous other = flock.mAutonomous[j];
            float dist = (curr.transform.position - other.transform.position).magnitude;

            if(dist < flock.visibility)
            { //if it is around the current boid visible range (circle)
                speed += other.Speed;
                flockDir += other.TargetDirection;
                steerPos += other.transform.position;
                count++;
            }
            if (dist < flock.separationDistance)
            {//if the distance is lesser than the acceptable range

                Vector3 targetDirection = (
                curr.transform.position -
                other.transform.position).normalized;
                //get direction vector from other to current

                separationDir += targetDirection;
                separationSpeed += dist * flock.WEIGHT_SEPERATION;
            }
        }

        if (count > 0)
        {
            speed = speed / count;
            flockDir = flockDir / count;
            //getting the average speed and direction the flock needs to go

            flockDir.Normalize();

            steerPos = steerPos / count;
        }
        if (separationCount > 0)
        {
            separationSpeed = separationSpeed / count;
            separationDir = separationDir / separationSpeed;
            separationDir.Normalize();
        }

        curr.TargetDirection =
            flockDir * speed * (flock.useAlignmentRule ? flock.WEIGHT_ALIGNMENT : 0.0f) +
            separationDir * separationSpeed * (flock.useSeparationRule ? flock.WEIGHT_SEPERATION : 0.0f) +
            (steerPos - curr.transform.position) * (flock.useCohesionRule ? flock.WEIGHT_COHESION : 0.0f);
    }


#region coroutine
    //this function is big o of n^2 , n^3 if u consider the excute function

    IEnumerator Coroutine_Flocking()
    {
        while (true)
        {
            if (useFlocking) 
            {
                foreach (Flock flock in flocks)
                {
                    List<Autonomous> autonomousList = flock.mAutonomous;
                    //get all the boid from the flock
                    for (int i = 0; i < autonomousList.Count; ++i)
                    {
                        Execute(flock, i);
                        if (i % BatchSize == 0)
                        {
                            yield return null;
                        }
                    }
                    yield return null;
                }
            }
            yield return new WaitForSeconds(TickDuration);
        }
    }

    IEnumerator Coroutine_Random()
    {
        while (true)
        {
            foreach (Flock flock in flocks)
            {
                if (flock.useRandomRule)
                {
                    List<Autonomous> autonomousList = flock.mAutonomous;
                    for (int i = 0; i < autonomousList.Count; ++i)
                    {
                        float rand = Random.Range(0.0f, 1.0f);
                        autonomousList[i].TargetDirection.Normalize(); 
                        float angle = Mathf.Atan2(autonomousList[i].TargetDirection.y, autonomousList[i].TargetDirection.x);

                        if (rand > 0.5f)
                        {
                            angle += Mathf.Deg2Rad * 45.0f;
                        }
                        else
                        {
                            angle -= Mathf.Deg2Rad * 45.0f;
                        }
                        Vector3 dir = Vector3.zero;
                        dir.x = Mathf.Cos(angle);
                        dir.y = Mathf.Sin(angle);

                        autonomousList[i].TargetDirection += dir * flock.WEIGHT_RANDOM;
                        autonomousList[i].TargetDirection.Normalize();
                        //Debug.Log(autonomousList[i].TargetDirection);

                        float speed = Random.Range(1.0f, autonomousList[i].MaxSpeed);
                        autonomousList[i].TargetSpeed += speed * flock.WEIGHT_SEPERATION;
                        autonomousList[i].TargetSpeed /= 2.0f;
                    }
                }
            //yield return null;
            }
            yield return new WaitForSeconds(TickDurationRandom);
        }
    }

    IEnumerator Coroutine_AvoidObstacles()
    {
    while (true)
    {
        foreach (Flock flock in flocks)
        {
        if (flock.useAvoidObstaclesRule)
        {
            List<Autonomous> autonomousList = flock.mAutonomous;
            for (int i = 0; i < autonomousList.Count; ++i)
            {
            for (int j = 0; j < mObstacles.Count; ++j)
            {
                float dist = (
                mObstacles[j].transform.position -
                autonomousList[i].transform.position).magnitude;
                if (dist < mObstacles[j].AvoidanceRadius)
                {
                Vector3 targetDirection = (
                    autonomousList[i].transform.position -
                    mObstacles[j].transform.position).normalized;

                autonomousList[i].TargetDirection += targetDirection * flock.WEIGHT_AVOID_OBSTICLES;
                autonomousList[i].TargetDirection.Normalize();
                }
            }
            }
        }
        //yield return null;
        }
        yield return null;
    }
    }

    IEnumerator Coroutine_SeparationWithEnemies()
    {
    while (true)
    {
        foreach (Flock flock in flocks)
        {
        if (!flock.useFleeOnSightEnemyRule || flock.isPredator) continue;

        foreach (Flock enemies in flocks)
        {
            if (!enemies.isPredator) continue;

            SeparationWithEnemies_Internal(
            flock.mAutonomous, 
            enemies.mAutonomous, 
            flock.enemySeparationDistance, 
            flock.WEIGHT_FLEE_ENEMY_ON_SIGHT);
        }
        //yield return null;
        }
        yield return null;
    }
    }

    IEnumerator Coroutine_Random_Motion_Obstacles()
    {
    while (true)
    {
        for (int i = 0; i < Obstacles.Length; ++i)
        {
        Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
        float rand = Random.Range(0.0f, 1.0f);
        autono.TargetDirection.Normalize();
        float angle = Mathf.Atan2(autono.TargetDirection.y, autono.TargetDirection.x);

        if (rand > 0.5f)
        {
            angle += Mathf.Deg2Rad * 45.0f;
        }
        else
        {
            angle -= Mathf.Deg2Rad * 45.0f;
        }
        Vector3 dir = Vector3.zero;
        dir.x = Mathf.Cos(angle);
        dir.y = Mathf.Sin(angle);

        autono.TargetDirection += dir * 0.1f;
        autono.TargetDirection.Normalize();
        //Debug.Log(autonomousList[i].TargetDirection);

        float speed = Random.Range(1.0f, autono.MaxSpeed);
        autono.TargetSpeed += speed;
        autono.TargetSpeed /= 2.0f;
        }
        yield return new WaitForSeconds(2.0f);
    }
    }

#endregion

    void SeparationWithEnemies_Internal(
    List<Autonomous> boids, 
    List<Autonomous> enemies, 
    float sepDist, 
    float sepWeight)
  {
    for(int i = 0; i < boids.Count; ++i)
    {
      for (int j = 0; j < enemies.Count; ++j)
      {
        float dist = (
          enemies[j].transform.position -
          boids[i].transform.position).magnitude;
        if (dist < sepDist)
        {
          Vector3 targetDirection = (
            boids[i].transform.position -
            enemies[j].transform.position).normalized;

          boids[i].TargetDirection += targetDirection;
          boids[i].TargetDirection.Normalize();

          boids[i].TargetSpeed += dist * sepWeight;
          boids[i].TargetSpeed /= 2.0f;
        }
      }
    }
  }


  void Rule_CrossBorder_Obstacles()
  {
    for (int i = 0; i < Obstacles.Length; ++i)
    {
      Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
      Vector3 pos = autono.transform.position;
      if (autono.transform.position.x > Bounds.bounds.max.x)
      {
        pos.x = Bounds.bounds.min.x;
      }
      if (autono.transform.position.x < Bounds.bounds.min.x)
      {
        pos.x = Bounds.bounds.max.x;
      }
      if (autono.transform.position.y > Bounds.bounds.max.y)
      {
        pos.y = Bounds.bounds.min.y;
      }
      if (autono.transform.position.y < Bounds.bounds.min.y)
      {
        pos.y = Bounds.bounds.max.y;
      }
      autono.transform.position = pos;
    }

    //for (int i = 0; i < Obstacles.Length; ++i)
    //{
    //  Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
    //  Vector3 pos = autono.transform.position;
    //  if (autono.transform.position.x + 5.0f > Bounds.bounds.max.x)
    //  {
    //    autono.TargetDirection.x = -1.0f;
    //  }
    //  if (autono.transform.position.x - 5.0f < Bounds.bounds.min.x)
    //  {
    //    autono.TargetDirection.x = 1.0f;
    //  }
    //  if (autono.transform.position.y + 5.0f > Bounds.bounds.max.y)
    //  {
    //    autono.TargetDirection.y = -1.0f;
    //  }
    //  if (autono.transform.position.y - 5.0f < Bounds.bounds.min.y)
    //  {
    //    autono.TargetDirection.y = 1.0f;
    //  }
    //  autono.TargetDirection.Normalize();
    //}
  }

  void Rule_CrossBorder()
  {
    foreach (Flock flock in flocks)
    {
      List<Autonomous> autonomousList = flock.mAutonomous;
      if (flock.bounceWall)
      {
        for (int i = 0; i < autonomousList.Count; ++i)
        {
          Vector3 pos = autonomousList[i].transform.position;
          if (autonomousList[i].transform.position.x + 5.0f > Bounds.bounds.max.x)
          {
            autonomousList[i].TargetDirection.x = -1.0f;
          }
          if (autonomousList[i].transform.position.x - 5.0f < Bounds.bounds.min.x)
          {
            autonomousList[i].TargetDirection.x = 1.0f;
          }
          if (autonomousList[i].transform.position.y + 5.0f > Bounds.bounds.max.y)
          {
            autonomousList[i].TargetDirection.y = -1.0f;
          }
          if (autonomousList[i].transform.position.y - 5.0f < Bounds.bounds.min.y)
          {
            autonomousList[i].TargetDirection.y = 1.0f;
          }
          autonomousList[i].TargetDirection.Normalize();
        }
      }
      else
      {
        for (int i = 0; i < autonomousList.Count; ++i)
        {
          Vector3 pos = autonomousList[i].transform.position;
          if (autonomousList[i].transform.position.x > Bounds.bounds.max.x)
          {
            pos.x = Bounds.bounds.min.x;
          }
          if (autonomousList[i].transform.position.x < Bounds.bounds.min.x)
          {
            pos.x = Bounds.bounds.max.x;
          }
          if (autonomousList[i].transform.position.y > Bounds.bounds.max.y)
          {
            pos.y = Bounds.bounds.min.y;
          }
          if (autonomousList[i].transform.position.y < Bounds.bounds.min.y)
          {
            pos.y = Bounds.bounds.max.y;
          }
          autonomousList[i].transform.position = pos;
        }
      }
    }
  }
}
