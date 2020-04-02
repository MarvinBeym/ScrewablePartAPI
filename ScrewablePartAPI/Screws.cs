using System;
using UnityEngine;

namespace ScrewablePartAPI
{
    /// <summary>
    /// the Save information class, saves partName, position, rotation, screw tightness and screw size
    /// </summary>
    public class Screws
    {
        public String partName;
        public Vector3[] screwsPositionsLocal;
        public Vector3[] screwsRotationLocal;
        public int[] screwsTightness;
        public int[] screwsSize;
    }
}
