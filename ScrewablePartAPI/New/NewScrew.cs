using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScrewablePartAPI.New
{
    public class NewScrew
    {
        public enum Type
        {
            /// <summary>Nut 3D-Model</summary>
            Nut,
            /// <summary>Screw 1 3D-Model</summary>
            Screw1,
            /// <summary>Screw 2 3D-Model</summary>
            Screw2,
            /// <summary>Screw 3 3D-Model</summary>
            Screw3
        }

        public GameObject model;
        public GameObject gameObject;
        public Vector3 position { get; set; }
        public Vector3 rotation { get; set; }
        public float scale { get; set; } = 1;
        public int size { get; set; } = 10;
        public Type type { get; set; } = Type.Screw1;

        public int tightness = 0;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="position">The vector3 position</param>
        /// <param name="rotation">The vector3 rotation</param>
        /// <param name="scale">The scale applied to x y and z</param>
        /// <param name="size">The wrench size needed</param>
        /// <param name="type">The ScrewType enum</param>
        public NewScrew(Vector3 position, Vector3 rotation, float scale = 1, int size = 10, Type type = Type.Screw2)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.size = size;
            this.type = type;
        }
    }
}
