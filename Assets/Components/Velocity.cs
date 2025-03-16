using Frent.Components;
using UnityEngine;

namespace Components
{
    internal struct Velocity : IComponent<Transform>
    {
        public Vector2 Delta;

        public readonly void Update(ref Transform arg)
        {
            arg.Value.m03 += Delta.x;
            arg.Value.m13 += Delta.y;
        }
    }
}