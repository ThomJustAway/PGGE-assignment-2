using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PGGE
{
    public static class CameraConstants
    {
        //Allows the camera to look at an angle that is relative to the original angle position. 
        public static Vector3 CameraAngleOffset { get; set; }

        //Allows the camera to position itself to a space that is relative to the original position
        public static Vector3 CameraPositionOffset { get; set; }

        //How fast/slow the camera move after a change has been made
        public static float Damping { get; set; }

        //How fast the camera move around the rotation
        public static float RotationSpeed { get; set; }

        //min and max pitch is how much the camera can how much the camera can move (applies on the TPC independent)
        public static float MinPitch { get; set; }
        public static float MaxPitch { get; set; }
    }
}
