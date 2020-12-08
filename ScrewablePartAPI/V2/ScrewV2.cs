using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
namespace ScrewablePartAPI.V2
{
    public class ScrewV2
    {
        public enum Type
        {
            Nut,
            Screw1,
            Screw2,
            Screw3
        }
        public Vector3 position { get; set; }
        public Vector3 rotation { get; set; }
        public float scale { get; set; } = 1;
        public int size { get; set; } = 10;
        public Type type { get; set; } = Type.Screw1;
        public string id;
        public ScrewInfo screwInfo;

        public GameObject gameObject;
        public MeshRenderer renderer; 
        public int tightness = 0;

        public ScrewV2(Vector3 position, Vector3 rotation, float scale = 1, int size = 10, Type type = Type.Screw2)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.size = size;
            this.type = type;
        }
    }
}
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element