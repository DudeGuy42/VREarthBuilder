using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Rendering;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;

public class GlobxelSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject _earthPrefab;

    [SerializeField]
    GameObject _waterPrefab;

    float _earthSpawnPeriod = 1f / 25f;
    float _lastEarthSpawnTime = 0f;

    float _waterSpawnPeriod = 1f / 25f;
    float _lastWaterSpawnTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Started globxel spawner.");
    }

    // This requires that the prefabs have an entity converter on them. 
    void SpawnGlobxelConvert(GameObject prefab, Vector3 position)
    {
        Instantiate(prefab, position, Quaternion.identity);
    }

    private void OnApplicationQuit()
    {
        
    }

    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var randomAngle = UnityEngine.Random.insideUnitCircle.normalized;
            var randomAngle3 = UnityEngine.Random.onUnitSphere;

            if (Time.time - _lastEarthSpawnTime >= _earthSpawnPeriod)
            {
                SpawnGlobxelConvert(_earthPrefab, Camera.main.transform.position + 5f*Camera.main.transform.forward + 2f * randomAngle3);
                _lastEarthSpawnTime = Time.time;
            }
        }
        else if(Input.GetKey(KeyCode.Space))
        {
            var randomAngle = UnityEngine.Random.insideUnitCircle.normalized;
            var randomAngle3 = new Vector3(randomAngle.x, randomAngle.y, 0f);

            if (Time.time - _lastWaterSpawnTime >= _waterSpawnPeriod)
            {
                SpawnGlobxelConvert(_waterPrefab, Camera.main.transform.position + 5f * Camera.main.transform.forward + 1f * randomAngle3);
                _lastWaterSpawnTime = Time.time;
            }
        }
    }
}
