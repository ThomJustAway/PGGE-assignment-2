using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Autonomous : MonoBehaviour
{
    public float MaxSpeed = 10.0f;

    public float Speed
    {
    get;
    private set;
    } = 0.0f;

    public Vector2 accel = new Vector2(0.0f, 0.0f);

    public float TargetSpeed = 0.0f; 
    public Vector3 TargetDirection = Vector3.zero; 
    /*targe direction is set by the flock behaviour 
     * used to determine where the boid/ obstacles will move
    */
    public float RotationSpeed = 0.0f;

    public SpriteRenderer spriteRenderer;

    void Start()
    {
        Speed = 0.0f;
        SetRandomSpeed();
        SetRandomDirection();
    }

    public void Update()
    {
        RotateGameObjectBasedOnTargetDirection();

        Speed = Speed + ((TargetSpeed - Speed) / 10.0f) * Time.deltaTime;

        if (Speed > MaxSpeed) //cap the next speed
            Speed = MaxSpeed;

        transform.Translate(Vector3.right * Speed * Time.deltaTime, Space.Self);
    }

    private void RotateGameObjectBasedOnTargetDirection()
    {
        Vector3 targetDirection = TargetDirection.normalized;
        //get the normalize value of the target direction
        Vector3 rotatedVectorToTarget =
            Quaternion.Euler(0, 0, 90) *
            targetDirection;
        //not too sure why they rotate the target direction by 90 degree for this...

        Quaternion targetRotation = Quaternion.LookRotation(
            forward: Vector3.forward, //want to rotate the object through the z axis
            upwards: rotatedVectorToTarget);
        //then create a rotation based of the vector. 
        //from: vector3.up to: rotatedVectorToTarget

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            RotationSpeed * Time.deltaTime); //give out the next rotation
    }

    //private IEnumerator Coroutine_LerpTargetSpeed(
    //  float start,
    //  float end,
    //  float seconds = 2.0f)
    //{
    //  float elapsedTime = 0;
    //  while (elapsedTime < seconds)
    //  {
    //    Speed = Mathf.Lerp(
    //      start,
    //      end,
    //      (elapsedTime / seconds));
    //    elapsedTime += Time.deltaTime;

    //    yield return null;
    //  }
    //  Speed = end;
    //}

    //private IEnumerator Coroutine_LerpTargetSpeedCont(
    //float seconds = 2.0f)
    //{
    //  float elapsedTime = 0;
    //  while (elapsedTime < seconds)
    //  {
    //    Speed = Mathf.Lerp(
    //      Speed,
    //      TargetSpeed,
    //      (elapsedTime / seconds));
    //    elapsedTime += Time.deltaTime;

    //    yield return null;
    //  }
    //  Speed = TargetSpeed;
    //}

    static public Vector3 GetRandom(Vector3 min, Vector3 max)
    {
        return new Vector3(
            Random.Range(min.x, max.x), 
            Random.Range(min.y, max.y), 
            Random.Range(min.z, max.z));
    }
    void SetRandomSpeed()
    {
        Speed = Random.Range(0.0f, MaxSpeed);
    }

    void SetRandomDirection()
    {
        float angle = 30.0f;// Random.Range(-180.0f, 180.0f);
        Vector2 dir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));//, 0.0f);
        dir.Normalize();
        TargetDirection = dir;
    }

    public void SetColor(Color c)
    {
        spriteRenderer.color = c;
    }
}
