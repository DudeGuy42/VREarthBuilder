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

// TODO: How do I order my systems?
// TODO: See Unity.Entities.UpdateAfterAttribute and UpdateBeforeAttribute
public class GlobPointSystem : SystemBase
{
    const string FORCE_FIELD_KERNEL_NAME = "CSMain";

    double _lastApplyTime = 0f;

    NativeArray<GlobPoint> _gpuGlobData;

    ComputeBuffer _globDataComputeBuffer;

    ComputeShader _forceFieldShader;

    int _forceFieldKernel = -1;
    uint _groupSizeX = 1;
    uint _groupSizeY = 1;
    uint _groupSizeZ = 1;

    float _applyPeriod;

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
        _forceFieldShader = Resources.Load<ComputeShader>("GlobCompute");
        
        _forceFieldKernel = _forceFieldShader.FindKernel(FORCE_FIELD_KERNEL_NAME);
        _forceFieldShader.GetKernelThreadGroupSizes(_forceFieldKernel, out _groupSizeX, out _groupSizeY, out _groupSizeZ);

        _pollQuery = GetEntityQuery(typeof(GlobPoint));

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        _gpuGlobData.Dispose();
        _globDataComputeBuffer.Dispose();
        base.OnDestroy();
    }

    EntityQuery _pollQuery;

    private EntityQuery PollEntitiesForGlobData(out NativeArray<Entity> outEntities, out NativeArray<GlobPoint> data, out uint count)
    {
        Stopwatch dataSync = Stopwatch.StartNew();
        var query = GetEntityQuery(typeof(GlobPoint), typeof(Translation));
        count = (uint)query.CalculateEntityCount();
        var nativeGlobPoints = new NativeArray<GlobPoint>(query.CalculateEntityCount(), Allocator.TempJob);

        var entities = _pollQuery.ToEntityArray(Allocator.TempJob);
        var em = EntityManager;
        
        for(int i = 0; i < count; i++)
        {
            var translation = em.GetComponentData<Translation>(entities[i]);
            var globData = em.GetComponentData<GlobPoint>(entities[i]);
            var velocity = em.GetComponentData<PhysicsVelocity>(entities[i]);

            var prediction = _applyPeriod * velocity.Linear; // test; this will probably not even do shit unless the delay is exactly 20 ms.

            globData.Position = translation.Value + prediction;

            em.SetComponentData(entities[i], globData);

            nativeGlobPoints[i] = em.GetComponentData<GlobPoint>(entities[i]);
        }

        outEntities = entities;
        data = nativeGlobPoints;
        UnityEngine.Debug.Log($"Query Data: {dataSync.ElapsedMilliseconds} ms");

        return _pollQuery;
    }

    private EntityQuery _impulseQuery;

    private void ApplyGpuData(NativeArray<Entity> entities, NativeArray<GlobPoint> points, uint count)
    {
        Stopwatch applyPhysics = Stopwatch.StartNew();

        var dt = Time.ElapsedTime - _lastApplyTime;
        for(int i = 0; i < count; i++)
        {
            var mass = EntityManager.GetComponentData<PhysicsMass>(entities[i]);
            var translation = EntityManager.GetComponentData<Translation>(entities[i]);
            var rotation = EntityManager.GetComponentData<Rotation>(entities[i]);

            var velocity = EntityManager.GetComponentData<PhysicsVelocity>(entities[i]);

            // NOTE I DO NOT SUPPORT CHANGING THE MASS OF OBJECTS HERE. IF MASS CHANGES IN GPU YOU NEED TO CALL SET COMPONENT DATA ON PHYSICSMASS.

            velocity.ApplyImpulse(mass, translation, rotation, (float)dt*points[i].Resultant, float3.zero);
            EntityManager.SetComponentData<GlobPoint>(entities[i], points[i]);
            EntityManager.SetComponentData<PhysicsVelocity>(entities[i], velocity);
        }

        UnityEngine.Debug.Log($"Applying physics took: {applyPhysics.ElapsedMilliseconds} ms");
    }

    bool _waiting;

    AsyncGPUReadbackRequest _readbackRequest;
    protected override void OnUpdate()
    {
        // The hard part about offloading work to the GPU is keeping the ECS in sync with the GPU work I am doing.
        // First we query ECS for a pile of entitities, then we pass that pile to a compute shader which operates on it, then 
        // we retrieve that pile again.
        //UnityEngine.Debug.Log($"Glob Count: {_globCounter}");
        if (!_waiting)
        {
            Stopwatch tick = Stopwatch.StartNew();
            var query = PollEntitiesForGlobData(out NativeArray<Entity> entities, out NativeArray<GlobPoint> globData, out uint count);

            ComputeBuffer computeBuffer = new ComputeBuffer((int)count, GlobPoint.STRIDE);

            _forceFieldShader.SetInt("GlobCount", (int)count);
            computeBuffer.SetData<GlobPoint>(globData);
            computeBuffer.SetCounterValue(count);
            _forceFieldShader.SetBuffer(_forceFieldKernel, "InputGlobPoints", computeBuffer);

            _forceFieldShader.Dispatch(_forceFieldKernel, 32, 1, 1);

            _waiting = true;
            _readbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref globData, computeBuffer, (request) =>
            {
                if (request.done)
                {
                    //UnityEngine.Debug.Log("Applying impulses.");
                    ApplyGpuData(entities, globData, count);

                    entities.Dispose();
                    globData.Dispose();
                    computeBuffer.Release();
                }
                else if(request.hasError)
                {
                    UnityEngine.Debug.LogError($"GPU Readback error. {request.ToString()}");
                }

                _waiting = false;
                _lastApplyTime = Time.ElapsedTime;
                _applyPeriod = tick.ElapsedMilliseconds / 1000f;
                UnityEngine.Debug.Log($"Force Field Tick took: {tick.ElapsedMilliseconds} ms. ({1000f/(tick.ElapsedMilliseconds)} Hz)");
            });
        }
    }
}
