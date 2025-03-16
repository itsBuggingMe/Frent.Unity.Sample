using UnityEngine;

namespace Components
{
    internal struct Transform
    {
        public Matrix4x4 Value;

        public Vector2 XY => new Vector2(Value.m03, Value.m13);
    }
}