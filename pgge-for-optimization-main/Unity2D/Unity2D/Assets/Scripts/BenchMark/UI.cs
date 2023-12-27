using Assets.Improve_scripts.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Text textNumBoids;
    public Text textNumEnemies;

    public FlockBehaviour flockbehaviour;
    public FlockBehaviourImprove improveflockbehaviour;
    void Start()
    {
        StartCoroutine(coroutine_updatetext());
    }

    IEnumerator coroutine_updatetext()
    {
        while (true)
        {
            int enemycount = 0;
            int boidcount = 0;
            foreach(var flock in  flockbehaviour.flocks) {
                boidcount += flock.numBoids;
            }
            textNumBoids.text = "boids: " + boidcount.ToString();
            //textNumEnemies.text = "predators: " + enemycount.tostring();
            yield return new WaitForSeconds(0.5f);
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
