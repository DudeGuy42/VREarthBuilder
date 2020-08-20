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

// TODO: How do I order my systems?
// TODO: See Unity.Entities.UpdateAfterAttribute and UpdateBeforeAttribute
public class GlobPointSystem : SystemBase
{
    GravityForceField _gravityField;
    public GravityForceField Gravity { 
        get
        {
            return _gravityField;
        } 
    }
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
        _gravityField = new GravityForceField(16, new float3(-32, -32, -32), new float3(32, 32, 32));

        base.OnCreate();
    }

    protected override void OnDestroy()
    {

        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        var gravity = _gravityField;

        gravity.Clear();

        Entities.ForEach((in PhysicsMass mass, in Translation translation) =>
        {
            gravity.AddMassPointGravityAtPosition(1f / mass.InverseMass, translation.Value);
        }).WithoutBurst().ScheduleParallel();
        
        Entities.ForEach((ref PhysicsVelocity velocity, in PhysicsMass mass, in Rotation rotation, in Translation translation) =>
        {
            velocity.ApplyImpulse(mass, translation, rotation, gravity.WeightAtPosition(1f / mass.InverseMass, translation.Value) * dt, float3.zero);
        }).WithoutBurst().ScheduleParallel();
    }
}
