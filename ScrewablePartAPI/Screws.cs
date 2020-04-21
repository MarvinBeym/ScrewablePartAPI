using System;
using UnityEngine;

namespace ScrewablePartAPI
{
    /// <summary>
    /// the Save information class, saves partName, position, rotation, screw tightness and screw size
    /// </summary>
    public class Screws
    {
        /// <summary>
        /// the parts name used for auto detecting what screws save to load.
        /// </summary>
        public String partName;

        /// <summary>
        /// array of Vectors for where to place screws on the parent.
        /// </summary>
        public Vector3[] screwsPositionsLocal;

        /// <summary>
        ///  array of Vectors for how to rotate screws on the parent.
        /// </summary>
        public Vector3[] screwsRotationLocal;

        /// <summary>
        ///  array of integers for how tight the screw should be 0-8| 0 = loose, 8 = tight
        /// </summary>
        public int[] screwsTightness;

        /// <summary>
        /// array of integers for what size each screw should have/which wrench is needed.
        /// </summary>
        public int[] screwsSize;
    }
}
