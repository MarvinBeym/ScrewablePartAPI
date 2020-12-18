using UnityEngine;
namespace ScrewablePartAPI.V2
{
    /// <summary>
    /// This is the screw class. This class is used to define how many screw to create, where to place them 
    /// and saves information like the current tightness
    /// </summary>
    public class ScrewV2
    {
        /// <summary>
        /// This defines what type of model the screw is supposed to be (how it will look)
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// The nut model
            /// </summary>
            Nut,
            /// <summary>
            /// The first screw model (screw with screwdriver slid)
            /// </summary>
            Screw1,
            /// <summary>
            /// The second screw model with a normal spanner/ratchet head (normal size)
            /// </summary>
            Screw2,
            /// <summary>
            /// The third screw model with a normal spanner/ratchet head (long version)
            /// </summary>
            Screw3
        }
        /// <summary>
        /// The local position of where it should be placed on the parent
        /// Also used by the logic for screwing in/out
        /// </summary>
        public Vector3 position { get; set; }
        /// <summary>
        /// The local rotation of where it should be placed on the parent
        /// Also used by the logic for screwing in/out
        /// </summary>
        public Vector3 rotation { get; set; }
        /// <summary>
        /// This scales the model
        /// This value get's applied to the models scale in all directions (x, y, z)
        /// </summary>
        public float scale { get; set; } = 1;
        /// <summary>
        /// The size of spanner/ratchet required for this screw
        /// </summary>
        public int size { get; set; } = 10;
        /// <summary>
        /// The screw type used for loading/instantiating the model
        /// </summary>
        public Type type { get; set; } = Type.Screw1;
        /// <summary>
        /// The id of the screw. This is always {parent name}_SCREW{index}
        /// </summary>
        public string id;

        /// <summary>
        /// The unity GameObject of this screw.
        /// Scale, rotation, position get applied to this
        /// </summary>
        public GameObject gameObject;
        /// <summary>
        /// The unity MeshRenderer used to highlight the screw in green and to reset back to the old texture
        /// </summary>
        public MeshRenderer renderer;
        /// <summary>
        /// The tightness of the screw.
        /// This value is adjusted when screwing in/out the screw
        /// </summary>
        public int tightness = 0;

        /// <summary>
        /// This decides if when the user aims at the screw will show the size
        /// </summary>
        public bool showSize = false;

        /// <summary>
        /// The ScrewV2 object constructor
        /// </summary>
        /// <param name="position">Position to place it on the parent</param>
        /// <param name="rotation">Rotation to place it on the parent</param>
        /// <param name="scale">The scale to apply to the gameobject of the screw</param>
        /// <param name="size">The size of spanner/ratchet required</param>
        /// <param name="type">The type of the screw (model to use)</param>
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