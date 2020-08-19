using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GlobPointBehavior : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    public float Mass;
    [SerializeField]
    public float WaterCharge;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<GlobPointSystem.GlobPoint>(entity);
        dstManager.SetComponentData(entity, new GlobPointSystem.GlobPoint()
        {
            Mass = Mass,
            WaterCharge = WaterCharge,
            Position = float3.zero,
            Resultant = float3.zero
        });
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
