using SpatialHash = System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.List<Frent.Entity>>;
using Frent;
using Frent.Components;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Components
{
    //implementation based on https://www.red3d.com/cwr/papers/1987/SIGGRAPH87.pdf
    internal struct Bird : IInitable, IUniformComponent<Root, Transform, Velocity>
    {
        private const int BlockSection = 1;
        private const int DetectionRadius = 1;
        private const float TrueDetectionRadius = 0.5f;
        private const int FlockCenteringRadius = 2;

        private static readonly (int DX, int DY)[] Detection = CalculateDeltaTable(DetectionRadius);
        private static readonly (int DX, int DY)[] Centering = CalculateDeltaTable(FlockCenteringRadius);

        private Entity _self;
        private (int X, int Y) _previous;

        public void Init(Entity self)
        {
            _self = self;
            ref var transform = ref self.Get<Transform>();
            var spaitial = self.World.UniformProvider.GetUniform<SpatialHash>();
            _previous = ((int)transform.XY.x, (int)transform.XY.y);
            if (!spaitial.ContainsKey(_previous))
                spaitial[_previous] = new();
        }

        public void Update(Root root, ref Transform transform, ref Velocity vel)
        {
            var space = root.Space;

            Vector2 pos = transform.XY;
            (int X, int Y) current = ((int)(pos.x / BlockSection), (int)(pos.y / BlockSection));

            Vector2 cumulativeVelocity = default;
            Vector2? deltaToClosest = null;

            foreach(var delta in Detection)
            {
                foreach(var entity in GetChunkData(space, Add(current, delta)))
                {
                    if (entity == _self)
                        continue;
                    if(InFieldOfView(entity, pos, vel.Delta, out Vector2 distanceTo))
                    {
                        cumulativeVelocity += entity.Get<Velocity>().Delta;
                        if (distanceTo.sqrMagnitude < (deltaToClosest?.sqrMagnitude ?? float.MaxValue))
                            deltaToClosest = distanceTo;
                    }
                }
            }

            //Collision Avoidance
            if(deltaToClosest is { } rotateAway)
            {
                float weight = 1 - rotateAway.magnitude;
                AlignVelocity(ref vel, rotateAway, weight * weight * -root.AvoidanceMultipler);
            }

            //Velocity Matching (rotate to be in same direction as neighbors)
            AlignVelocity(ref vel, cumulativeVelocity, root.VelocityMatchMultipler);

            //Flock Centering
            Vector2 cumulativeLocation = default;
            int count = 0;

            foreach (var delta in Centering)
            {
                foreach (var entity in GetChunkData(space, Add(current, delta)))
                {
                    if (entity == _self)
                        continue;

                    cumulativeLocation += entity.Get<Transform>().XY;
                    count++;
                }
            }

            Vector2 averageLocation = count > 0 ? cumulativeLocation / count : Vector2.zero;

            Vector2 deltaToAvgLocation = (averageLocation - transform.XY).normalized;

            AlignVelocity(ref vel, deltaToAvgLocation, root.FlockCenteringMultipler);

            if (current != _previous)
            {
                space[_previous].Remove(_self);
                space[current].Add(_self);
                _previous = current;
            }
        }

        private bool InFieldOfView(Entity entity, Vector2 currentPos, Vector2 selfVel, out Vector2 distanceTo)
        {
            distanceTo = entity.Get<Transform>().XY - currentPos;

            if (distanceTo == default)
            {
                return false;
            }

            if (
                distanceTo.x * distanceTo.x + distanceTo.y * distanceTo.y < TrueDetectionRadius * TrueDetectionRadius && // we are close
                Vector2.Dot(distanceTo.normalized, selfVel.normalized) > -0.4f //field of view
                )
            {

                return true;
            }

            return false;
        }

        private void AlignVelocity(ref Velocity selfVel, Vector2 direction, float multipler)
        {
            float cross = direction.x * selfVel.Delta.y - direction.y * selfVel.Delta.x;
            selfVel.Delta = RotateVector(selfVel.Delta, multipler * -Math.Sign(cross));
        }

        private static List<Entity> GetChunkData(SpatialHash hash, (int, int) coord)
        {
            if(hash.TryGetValue(coord, out var list))
            {
                return list;
            }

            return hash[coord] = new();
        }

        private static (int X, int Y) Add((int X, int Y) l, (int X, int Y) r)
        {
            return (l.X + r.X, l.Y + r.Y);
        }

        private static (int dX, int dY)[] CalculateDeltaTable(int radius)
        {
            radius = Math.Max(1, radius);
            int grid = radius * 2 + 1;
            (int, int)[] deltas = new (int, int)[grid * grid];

            int index = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    deltas[index++] = (x, y);
                }
            }

            return deltas;
        }

        //how does unity not have this function!!
        public static Vector2 RotateVector(Vector2 v, float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
