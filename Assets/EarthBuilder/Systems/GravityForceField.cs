using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Assets.EarthBuilder.Systems
{
    // TODO: Generalize this for other quantities like water charge (so long as it is performant)
    public struct GravityForceField
    {
        float3 _min;
        float3 _max;

        int _dimension;
        NativeArray<float3> _acceleration; // this is a 3d grid of force vectors

        // only cubic dimensions for now
        public GravityForceField(int dimension, float3 min, float3 max)
        {
            _dimension = dimension;
            _acceleration = new NativeArray<float3>(dimension * dimension * dimension, Allocator.Persistent);
            _min = min;
            _max = max;

            Clear();
        }

        public void ForAllFieldPositions(Action<GravityForceField, int> action)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int3 position = new int3(0, 0, 0);

            for(position.z = 0; position.z < _dimension; position.z++)
            {
                for (position.y = 0; position.y < _dimension; position.y++)
                {
                    for (position.x = 0; position.x < _dimension; position.x++)
                    {
                        action(this, AtIndex(position));
                    }
                }
            }

            UnityEngine.Debug.Log($"{action.Method.Name} took {watch.ElapsedMilliseconds} ms");
        }

        public void Clear()
        {
            var accelerations = _acceleration;
            ForAllFieldPositions((f, i) => { accelerations[i] = new float3(0f, 0f, 0f); });
        }

        private float3 WorldPositionAtIndex(int index)
        {
            float3 dimensionScale = (_max - _min) / _dimension;

            CoordinateFromIndex(index, out int3 forceFieldPosition);

            return dimensionScale * forceFieldPosition + _min ;
        }

        private int AtIndex(int3 forceFieldPosition)
        {
            return forceFieldPosition.x + forceFieldPosition.y * _dimension + forceFieldPosition.z * _dimension * _dimension;
        }

        public void AddMassPointGravityAtPosition(float mass, float3 position)
        {
            const float GRAVITY_SCALE_FACTOR = 1f;
            var accelerations = this._acceleration;
            
            ForAllFieldPositions((field, i) =>
            {
                var diff = position - field.WorldPositionAtIndex(i);
                field._acceleration[i] += GRAVITY_SCALE_FACTOR * (mass / Mathf.Pow(Mathf.Pow(diff.x, 2) + Mathf.Pow(diff.y, 2) + Mathf.Pow(diff.z, 2), 1.5f)) * math.normalize(diff);
            });
        }

        private void CoordinateFromIndex(int index, out int3 forceFieldPosition)
        {
            forceFieldPosition.x = index / (_dimension * _dimension);
            forceFieldPosition.y = (index / _dimension) % _dimension;
            forceFieldPosition.z = index % _dimension;
        }

        private int3 ForceFieldPositionFromWorldPosition(float3 worldPosition)
        {
            float3 dimensionScale = (_max - _min) / _dimension;
            var result = math.floor((worldPosition - _min) / dimensionScale);
            //UnityEngine.Debug.Log($"S: {dimensionScale}; WP: {worldPosition}: Result: {result}");
            
            // todo
            return new int3((int) result.x, (int) result.y, (int) result.z);
        }

        public float3 WorldPositionFromIndex(int index)
        {
            CoordinateFromIndex(index, out int3 fieldPosition);
            float3 dimensionScale = (_max - _min) / _dimension;
            return fieldPosition * dimensionScale + _min;
        }

        // returns an interpolated force value from a position in space.
        public float3 WeightAtPosition(float mass, float3 worldPosition)
        {
            var index = AtIndex(ForceFieldPositionFromWorldPosition(worldPosition));
            if(index < 0 || index >= _dimension*_dimension*_dimension)
            {
                return float3.zero;
            }

            // need to interpolate here.
            // https://www.grc.nasa.gov/WWW/winddocs/utilities/b4wind_guide/trilinear.html
           // https://en.wikipedia.org/wiki/Trilinear_interpolation

            /*    
            x1 = x(i  ,j  ,k  )
            x2 = x(i+1,j  ,k  )
            x3 = x(i  ,j+1,k  )
            x4 = x(i+1,j+1,k  )
            x5 = x(i  ,j  ,k+1)
            x6 = x(i+1,j  ,k+1)
            x7 = x(i  ,j+1,k+1)
            x8 = x(i+1,j+1,k+1) */
            /// TODO: Check that our index doesn't overflow.

            //var c = ForceFieldPositionFromWorldPosition(worldPosition);
            //var x1 = AtIndex(new int3(c.x, c.y, c.z));
            //var x2 = AtIndex(new int3(c.x + 1, c.y, c.z));
            //var x3 = AtIndex(new int3(c.x, c.y + 1, c.z));
            //var x4 = AtIndex(new int3(c.x + 1, c.y + 1, c.z));
            //var x5 = AtIndex(new int3(c.x, c.y, c.z + 1));
            //var x6 = AtIndex(new int3(c.x + 1, c.y, c.z + 1));
            //var x7 = AtIndex(new int3(c.x, c.y + 1, c.z + 1));
            //var x8 = AtIndex(new int3(c.x + 1, c.y + 1, c.z + 1));


            return mass *_acceleration[index];
        }

        public float3 AccelerationAtIndex(int index)
        {
            if (index < 0 || index >= _dimension * _dimension * _dimension)
            {
                return float3.zero;
            }

            // need to interpolate here.

            return _acceleration[index];
        }
    }
}
