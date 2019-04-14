using Lattice;
using UnityEngine;

namespace xRLab.ForceDirected
{
    public class VizUtility
    {
        public static Vector GetVector(Vector3 vector)
        {
            return new Vector(vector.x, vector.y, vector.z) * 10;
        }

        public static Vector3 GetVector3(Vector vector)
        {
            return new Vector3((float)vector.X, (float)vector.Y, (float)vector.Z) / 10;
        }
    }
}