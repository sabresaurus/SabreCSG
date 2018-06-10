using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeleteMeBeforePublishing
{
    public class WaterVolumeComponent : MonoBehaviour
    {
        public int thickness;

        public void Start()
        {
            Debug.Log("WATER VOLUME, THICKNESS " + thickness);
        }
    }
}
