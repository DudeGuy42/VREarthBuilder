using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Rendering;
using Unity.Physics;
using Unity.Mathematics;


public class GlobxelSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject _earthPrefab;

    [SerializeField]
    GameObject _waterPrefab;

    // TODO: These are part of the new SpawnGlobxelNew methods. I will update eventually.
    //[SerializeField]
    //Mesh _defaultMesh;

    //[SerializeField]
    //UnityEngine.Material _defaultMaterial;

    float _spawnPeriod = 1f / 1000f;
    float _lastSpawnTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // This requires that the prefabs have an entity converter on them. 
    void SpawnGlobxelConvert(GameObject prefab, Vector3 position)
    {
        Instantiate(prefab, position, Quaternion.identity);
    }

    // TODO: Fails with AABB errors. I dont know what I am doing in most of these lines though.
    // Consult Unity documentation for how to create entities to get this right - it will matter later.
    //void SpawnGlobxelNew(Vector3 position)
    //{
    //    // TODO : Create entity here with data.
    //    Debug.Log($"TODO: Spawn at {position}");
    //    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    //    var baseGlobxelArchetype = em.CreateArchetype(
    //        typeof(Translation),
    //        typeof(Rotation),
    //        typeof(RenderMesh),
    //        typeof(RenderBounds),
    //        typeof(LocalToWorld),
    //        typeof(PhysicsMass),
    //        typeof(PhysicsVelocity),
    //        typeof(PhysicsCollider)
    //        );

    //    var newEntity = em.CreateEntity(baseGlobxelArchetype);

    //    // TODO: what is shared component data vs component data?

    //    em.AddComponentData(newEntity, new Translation
    //    {
    //        Value = position
    //    });

    //    em.AddSharedComponentData(newEntity, new RenderMesh
    //    {
    //        mesh = _defaultMesh,
    //        material = _defaultMaterial
    //    });

    //    em.AddComponentData(newEntity, new RenderBounds
    //    {
    //        Value = new AABB
    //        {
    //            Center = float3.zero,
    //            Extents = new float3(1f, 1f, 1f)
    //        }
    //    });

    //    em.AddComponentData(newEntity, new PhysicsVelocity
    //    {
    //        Angular = new float3(0f, 0f, 0f),
    //        Linear = new float3(1f, 0f, 0f)
    //    });


    //    if (em.HasComponent<PhysicsMass>(newEntity))
    //    {
    //        var mass = em.GetComponentData<PhysicsMass>(newEntity);
    //        mass.InverseMass = 1f;
    //        em.SetComponentData<PhysicsMass>(newEntity, mass);
    //    }


    //    em.AddComponentData(newEntity, new PhysicsCollider
    //    {
    //        Value = Unity.Physics.BoxCollider.Create(new BoxGeometry()
    //        {
    //            Size = new float3(1, 1, 1),
    //            Center = new float3(0.5f, 0.5f, 0.5f),
    //            Orientation = quaternion.identity,
    //            BevelRadius = 0
    //        })
    //    });
    //}

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            if(Time.time - _lastSpawnTime >= _spawnPeriod)
            {
                SpawnGlobxelConvert(_earthPrefab, Camera.main.transform.position + 10f*Camera.main.transform.forward);
                _lastSpawnTime = Time.time;
            }
        }
        else if(Input.GetKey(KeyCode.Space))
        {
            if (Time.time - _lastSpawnTime >= _spawnPeriod)
            {
                SpawnGlobxelConvert(_waterPrefab, Camera.main.transform.position + 10f * Camera.main.transform.forward);
                _lastSpawnTime = Time.time;
            }
        }
    }
}
