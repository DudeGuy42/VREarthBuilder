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

// TODO: How do I order my systems?
// TODO: See Unity.Entities.UpdateAfterAttribute and UpdateBeforeAttribute
public class GlobPointSystem : SystemBase
{
    
    [Flags]
    public enum GlobKind: byte
    {
        MASS_PARTICLE = 1,
        ION = 2
    }
    public const float GRAVITATIONAL_CONSTANT = 0.1f;//0.0075f;
    public const float GLOBINESS_CONSTANT = 1f;

    struct GlobPoint
    {
        public float Globiness;
        public float Mass;
        public float3 Position;
    }

    NativeArray<GlobPoint> _globPoints;

    protected override void OnCreate()
    {
        _globPoints = new NativeArray<GlobPoint>(5000, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        _globPoints.Dispose();
        base.OnDestroy();
    }

    private EntityQuery query;
    protected override void OnUpdate()
    {
        int entitiesInQuery = query.CalculateEntityCount();
        float dt = Time.DeltaTime;
        var globPoints = this._globPoints;

        Entities
            .ForEach((int entityInQueryIndex, in Translation translation, in PhysicsMass mass, in PhysicsCustomTags tag) =>
        {
            if ((tag.Value & (byte)GlobKind.ION) != 0)
            {
                globPoints[entityInQueryIndex] = new GlobPoint { Mass = 1f/mass.InverseMass, Globiness = 0.01f, Position = translation.Value };
            }
            else
            {
                globPoints[entityInQueryIndex] = new GlobPoint { Mass = 1f/mass.InverseMass, Globiness = 0f, Position = translation.Value };
            }

        })
        .WithStoreEntityQueryInField(ref query)
        .ScheduleParallel();

        Entities
            .ForEach((
                ref PhysicsVelocity velocity,
                in PhysicsMass mass,
                in Translation translation,
                in Rotation rotation,
                in PhysicsCustomTags tag) =>
        {
            float3 resultant = float3.zero;
            float3 diff = float3.zero;
            
            for (int i = 0; i < entitiesInQuery; i++)
            {
                diff = globPoints[i].Position - translation.Value;

                if (math.lengthsq(diff) < 1f)
                {
                    continue;
                }
                
                // Newton's law of gravity
                resultant += GRAVITATIONAL_CONSTANT * (globPoints[i].Mass / math.pow(math.pow(diff.x, 2) + math.pow(diff.y, 2) + math.pow(diff.z, 2), 1.5f)) * math.normalize(diff);

                if ((tag.Value & (byte)GlobKind.ION) != 0)
                {
                    resultant += GLOBINESS_CONSTANT * (globPoints[i].Globiness / math.pow(math.pow(diff.x, 2) + math.pow(diff.y, 2) + math.pow(diff.z, 2), 1.5f)) * math.normalize(diff);
                }                

            }

            // newtons second
            velocity.ApplyImpulse(mass, translation, rotation, resultant, translation.Value);
        })
        .ScheduleParallel();

    }
}
