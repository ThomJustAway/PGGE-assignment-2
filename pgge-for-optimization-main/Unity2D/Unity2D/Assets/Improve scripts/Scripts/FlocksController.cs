using Patterns;
using System.Collections;
using UnityEngine;

namespace Assets.Improve_scripts.Scripts
{
    public class FlocksController : Singleton<FlocksController>
    {
        #region properties
        [SerializeField] private BoxCollider2D bound;
        public BoxCollider2D BoxCollider2D { get { return bound; } }
        [SerializeField] private int BoidsToSpawn = 100; 
        public int numberOfBoidsToSpawn { get { return BoidsToSpawn; } } 
        [SerializeField] private FlockCreator[] flocks;
        public int numberOfBoidsCurrently { get
            {
                int number = 0;
                foreach (var f in flocks)
                {
                    number += f.numberOfBoids;
                }
                return number;
            } }
        #endregion
        
    }
}