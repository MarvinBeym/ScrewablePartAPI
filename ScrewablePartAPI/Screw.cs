using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScrewablePartAPI
{
    /// <summary>
    /// A single screw
    /// </summary>
    public class Screw
    {

        public GameObject model;
        public Vector3 position { get; set; }
        public Vector3 rotation { get; set; }
        public float scale { get; set; } = 1;
        public int size { get; set; } = 10;
        public ScrewablePart.ScrewType type { get; set; } = ScrewablePart.ScrewType.Screw1;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="position">The vector3 position</param>
        /// <param name="rotation">The vector3 rotation</param>
        /// <param name="scale">The scale applied to x y and z</param>
        /// <param name="size">The wrench size needed</param>
        /// <param name="type">The ScrewType enum</param>
        public Screw(Vector3 position, Vector3 rotation, float scale = 1, int size = 10, ScrewablePart.ScrewType type = ScrewablePart.ScrewType.Screw2)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.size = size;
            this.type = type;
        }
    }
}
