using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics;
using Unity.Physics.Extensions;
using System.Numerics;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;
using Unity.Entities.CodeGeneratedJobForEach;
using System.Threading;
using UnityEngine.Rendering;
using System.Linq;
using Assets.EarthBuilder.Systems;
using TMPro;

// TODO: How do I order my systems?
// TODO: See Unity.Entities.UpdateAfterAttribute and UpdateBeforeAttribute
public class GlobPointSystem : SystemBase
{
    public struct GlobPoint : IComponentData
    {
        public const int STRIDE = 32;
        public float Mass; // 4
        public float WaterCharge; // 8
        public float3 Position; // 12, 16, 20
        public float3 Resultant; // 24, 28, 32
    }

    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnDestroy()
    {

        base.OnDestroy();
    }

    private BarnesHutTree _bhTree;
    public BarnesHutTree Tree
    {
        get
        {
            return _bhTree;
        }
    }
    // TODO: Minimize allocations somehow.
    protected override void OnUpdate()
    {
        _bhTree = new BarnesHutTree(new float3(-32, -32, -32), new float3(32, 32, 32)); // tree is rebuilt every update.
        Entities.ForEach((in Entity entity, in PhysicsMass mass, in Translation translation) =>
        {
            _bhTree.AddEntityToTree(entity, 1f / mass.InverseMass, translation.Value);
        }).WithoutBurst().Run();

        Entities.ForEach((ref PhysicsVelocity velocity, in Entity entity, in PhysicsMass mass, in Translation translation, in Rotation rotation) =>
        {
            var resultant = _bhTree.ResultantOnEntity(entity, 1f/mass.InverseMass, translation.Value);
            velocity.ApplyImpulse(mass, translation, rotation, resultant * Time.DeltaTime, float3.zero);
        }).WithoutBurst().Run();
    }
}
