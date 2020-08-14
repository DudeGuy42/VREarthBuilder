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

public class ForceFieldSystem : SystemBase
{
    NativeArray<float3> positions;

    protected override void OnCreate()
    {
        positions = new NativeArray<float3>(5000, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        positions.Dispose();
        base.OnDestroy();
    }

    private EntityQuery query;
    protected override void OnUpdate()
    {
        int entitiesInQuery = query.CalculateEntityCount();
        float dt = Time.DeltaTime;
        var positions = this.positions;

        Entities
            .ForEach((int entityInQueryIndex, in Translation translation) =>
        {
            positions[entityInQueryIndex] = translation.Value;
        })
        .WithStoreEntityQueryInField(ref query)
        .ScheduleParallel();

        Entities
            .ForEach((
                ref PhysicsVelocity velocity,
                in PhysicsMass mass,
                in Translation translation,
                in Rotation rotation) =>
        {
            float3 resultant = float3.zero;
            float3 diff = float3.zero;

            for (int i = 0; i < entitiesInQuery; i++)
            {
                diff = positions[i] - translation.Value;

                if (math.lengthsq(diff) < 1f)
                {
                    continue;
                }

                // Newton's law of gravity
                resultant += (1f / math.pow(math.pow(diff.x, 2) + math.pow(diff.y, 2) + math.pow(diff.z, 2), 1.5f)) * math.normalize(diff);
            }

            // newtons second
            velocity.ApplyImpulse(mass, translation, rotation, resultant, translation.Value);
        })
        .ScheduleParallel();

    }
}
