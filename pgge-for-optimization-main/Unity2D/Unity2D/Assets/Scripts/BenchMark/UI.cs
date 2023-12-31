using Assets.Improve_scripts.Scripts;
using experimenting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Text textNumBoids;
    public Text textNumEnemies;

    public bool original = false;

    public FlockBehaviour flockbehaviourOriginal;
    //public FlockBehaviourImprove improveflockbehaviour;
    public FlockBehaviourImproveV2 flockBehaviour;
    void Start()
    {
        StartCoroutine(coroutine_updatetext());
    }

    IEnumerator coroutine_updatetext()
    {
        while (true)
        {
            if (original)
            {
                int boidcount = 0;
                foreach (var flock in flockbehaviourOriginal.flocks)
                {
                    boidcount += flock.numBoids;
                }
                textNumBoids.text = "boids: " + boidcount.ToString();
                //textNumEnemies.text = "predators: " + enemycount.tostring();
                yield return new WaitForSeconds(0.5f);
            }
            else{
                //int enemycount = 0;
                int boidcount = 0;
                foreach(var flock in flockBehaviour.flocks) {
                    boidcount += flock.numBoids;
                }
                textNumBoids.text = "boids: " + boidcount.ToString();
                //textNumEnemies.text = "predators: " + enemycount.tostring();
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    //ienumerator coroutine_updatetextforimprove()
    //{
    //    while (true)
    //    {
    //        int enemycount = 0;
    //        int boidcount = 0;
    //        foreach (flock flock in improveflockbehaviour.flocks)
    //        {
    //            if (flock.ispredator)
    //                enemycount += flock.mautonomous.count;
    //            else
    //                boidcount += flock.mautonomous.count;
    //        }
    //        textnumboids.text = "boids: " + boidcount.tostring();
    //        textnumenemies.text = "predators: " + enemycount.tostring();
    //        yield return new waitforseconds(0.5f);
    //    }
    //}

}
