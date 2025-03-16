using SpatialHash = System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.List<Frent.Entity>>;
using Frent;
using Frent.Systems;
using UnityEngine;
using Components;
using System;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;
using Transform = Components.Transform;

public class Root : MonoBehaviour
{
    private const int EntityCount = 200;

    public Mesh Bird;
    public Material Material;
    
    private World _world;
    private Matrix4x4[] _matrixBuffer;

    public CameraBounds Camera = new();

    public SpatialHash Space;

    public float AvoidanceMultipler = 0.1f;
    public float VelocityMatchMultipler = 0.02f;
    public float FlockCenteringMultipler = 0.02f;

    void Start()
    {
        DefaultUniformProvider defaultUniformProvider = new DefaultUniformProvider();
        defaultUniformProvider
            .Add(Space = new SpatialHash())
            .Add(this)
            .Add(Camera);
        _world = new(defaultUniformProvider);

        _matrixBuffer = Array.Empty<Matrix4x4>();

        for (int i = 0; i < EntityCount; i++)
        {
            Create(Random.insideUnitCircle * 8f, Random.insideUnitCircle.normalized * 0.03f);
        }

        void Create(Vector2 position, Vector2 vel)
        {
            var pos = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.1f);
            _world.Create<Transform, Velocity, Bird>(new() { Value = pos }, new() { Delta = vel  }, default);
        }
    }

    void Update()
    {
        _world.Update();
        CameraBounds();

        if (!Material.enableInstancing)
            Material.enableInstancing = true;

        foreach (var positionSpan in _world.Query<With<Transform>, With<Bird>>()
            .EnumerateChunks<Transform>())
        {
            if (_matrixBuffer.Length < positionSpan.Span.Length)
                Array.Resize(ref _matrixBuffer, positionSpan.Span.Length);

            MemoryMarshal.Cast<Transform, Matrix4x4>(positionSpan.Span).CopyTo(_matrixBuffer);
            Graphics.DrawMeshInstanced(Bird, 0, Material, _matrixBuffer, positionSpan.Span.Length);
        }

        void CameraBounds()
        {
            Camera cam = UnityEngine.Camera.main;

            float heightHalf = cam.orthographicSize;
            float widthHalf = heightHalf * cam.aspect;
            Camera.TL = new Vector2(widthHalf, -heightHalf);
            Camera.BR = new Vector2(-widthHalf, heightHalf);
        }
    }
}

public class CameraBounds
{
    public Vector2 TL;
    public Vector2 BR;
}