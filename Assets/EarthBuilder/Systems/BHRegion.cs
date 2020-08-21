using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.EarthBuilder.Systems
{
    // TODO: Consider creating a pool.
    public class BHRegion
    {
        public struct RegionEntity
        {
            public Entity entity;
            public float mass;
            public float3 position;
        }

        public float TotalMass;
        public float3 Moment;

        // Defines rectangular region.
        public float3 Max;
        public float3 Min;

        RegionEntity? _currentRegionEntity;
        BHRegion[] _subRegions;

        public float Dimension
        {
            get
            {
                return math.length(Max - Min);
            }
        }

        public RegionEntity? CurrentRegionEntity
        {
            get
            {
                return _currentRegionEntity;
            }
        }

        public BHRegion[] Subregions
        {
            get
            {
                return _subRegions;
            }
        }

        /// <summary>
        /// Defines a rectangular region.
        /// </summary>
        /// <param name="max"></param>
        /// <param name="min"></param>
        public BHRegion(float3 min, float3 max)
        {
            TotalMass = 0f;
            Moment = new float3(0, 0, 0);
            _currentRegionEntity = null;
            _subRegions = null;
            Min = min;
            Max = max;
        }

        public bool IsEmpty
        {
            get
            {
                return (_currentRegionEntity == null && _subRegions == null);
            }
        }

        public bool IsInternal
        {
            get
            {
                return _currentRegionEntity == null && _subRegions != null;
            }
        }

        public bool IsExternal
        {
            get
            {
                return _subRegions == null && _currentRegionEntity != null;
            }
        }

        public float3 CenterOfMass
        {
            get
            {
                return Moment / TotalMass;
            }
        }
        
        private void Subdivide()
        {
            _subRegions = new BHRegion[8];
            
            var midpoint = (Max + Min) / 2f;
            float3 dimension = (Max - Min) / 2f;

            float3 rightShift = new float3(dimension.x, 0f, 0f);
            float3 upShift = new float3(0f, dimension.y, 0f);
            float3 forwardShift = new float3(0f, 0f, dimension.z);

            _subRegions[0] = new BHRegion(midpoint, Max);
            _subRegions[1] = new BHRegion(midpoint - rightShift, Max - rightShift);
            _subRegions[2] = new BHRegion(midpoint - rightShift - forwardShift, Max - rightShift - forwardShift);
            _subRegions[3] = new BHRegion(midpoint - forwardShift, Max - forwardShift);

            _subRegions[4] = new BHRegion(midpoint - upShift, Max - upShift);
            _subRegions[5] = new BHRegion(midpoint - upShift - rightShift, Max - upShift - rightShift);
            _subRegions[6] = new BHRegion(midpoint - upShift - rightShift - forwardShift, Max - upShift - rightShift - forwardShift);
            _subRegions[7] = new BHRegion(midpoint - upShift - forwardShift, Max - upShift - forwardShift);
        }

        public bool ContainsPoint(float3 point)
        {
            return !(point.x >= Max.x || point.y >= Max.y || point.z >= Max.z 
                || point.x <= Min.x || point.y <= Min.y || point.z <= Min.z);
        }

        private void FindAndPlaceInSubregion(RegionEntity regionEntity)
        {
            foreach(var region in _subRegions)
            {
                if(region.ContainsPoint(regionEntity.position))
                {
                    region.AddRegionEntity(regionEntity);
                    break;
                }
            }
        }

        public void AddRegionEntity(RegionEntity newRegionEntity)
        {
            if(IsEmpty)
            {
                // if this node is empty, place the entity here.
                _currentRegionEntity = newRegionEntity;
                // NOTE: Do i need to update total mass and center of mass here??
                TotalMass = newRegionEntity.mass;
                Moment = newRegionEntity.mass * newRegionEntity.position;
            }
            else if(IsInternal)
            {
                // If this node is an internal node, update the center of mass and total mass.
                TotalMass += newRegionEntity.mass;
                Moment += newRegionEntity.mass * newRegionEntity.position;

                FindAndPlaceInSubregion(newRegionEntity);
            }
            else if(IsExternal)
            {
                // If this node is an external node and contains an entity, f, then there are
                // two entities in the same region. Subdivide the region further by creating
                // 8 nodes. Then recursively insert both e and f in to their appropriate octants.
                // Since e and f may still end up in the same octant, there may be several subdivisions
                // during a single insertion. Finally, update the total mass and center of mass of this node.
                Subdivide();

                FindAndPlaceInSubregion(newRegionEntity);
                FindAndPlaceInSubregion(_currentRegionEntity.Value);

                _currentRegionEntity = null;

                TotalMass += newRegionEntity.mass;
                Moment += newRegionEntity.mass * newRegionEntity.position;
            }

        }
    }
}
