using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.EarthBuilder.Systems
{
    public class BarnesHutTree
    {
        const float THETA = 0.5f; // This parameter scales the realism of the simulation. 1 = brute force, 0 = rough approximation.
        const float GRAVITATIONAL_CONSTANT = 0.01f;

        BHRegion _root;

        public BHRegion Root
        {
            get
            {
                return _root;
            }
        }

        public BarnesHutTree(float3 min, float3 max)
        {
            _root = new BHRegion(min, max);
        }

        public void AddEntityToTree(Entity entity, float mass, float3 position)
        {
            _root.AddRegionEntity(new BHRegion.RegionEntity()
            {
                entity = entity,
                mass = mass,
                position = position
            });
        }

        public void PreOrderTraverseTree(Action<BHRegion> action, BHRegion startRegion)
        {
            if (startRegion == null) return;

            BHRegion currentRegion = startRegion;
            
            action(currentRegion);

            if (currentRegion.IsExternal) return;
            else if(currentRegion.IsInternal)
            {
                for (int i = 0; i < 8; i++)
                {
                    PreOrderTraverseTree(action, currentRegion.Subregions[i]);
                }
            }
        }

        private void ForceWalkTraversal(BHRegion region, Entity entity, float mass, float3 position, ref float3 resultant)
        {
            if (region == null || region.IsEmpty) return;

            BHRegion currentRegion = region;

            if (currentRegion.IsExternal)
            {
                if(currentRegion.CurrentRegionEntity.Value.entity != entity)
                {
                    // TODO: Add gravitational stuff.
                    var diff = currentRegion.CurrentRegionEntity.Value.position - position;
                    var magnitude = GRAVITATIONAL_CONSTANT * (currentRegion.CurrentRegionEntity.Value.mass * mass) / math.pow(math.pow(diff.x, 2) + math.pow(diff.y, 2) + math.pow(diff.z, 2), 1.5f);
                    resultant += magnitude * math.normalize(diff);
                }
            }
            else 
            {
                var s = currentRegion.Dimension;
                var d = math.length(position - currentRegion.CenterOfMass);

                if(s/d < THETA)
                {
                    // TODO: Add gravitational stuff; use currentRegion.TotalMass here.
                    var diff = currentRegion.CenterOfMass - position;
                    var magnitude = GRAVITATIONAL_CONSTANT * (currentRegion.TotalMass * mass) / math.pow(math.pow(diff.x, 2) + math.pow(diff.y, 2) + math.pow(diff.z, 2), 1.5f);
                    resultant += magnitude * math.normalize(diff);
                }
                else
                {
                    for(int i = 0; i < 8; i++)
                    {
                        ForceWalkTraversal(region.Subregions[i], entity, mass, position, ref resultant);
                    }
                }
            }
        }

        public float3 ResultantOnEntity(Entity entity, float mass, float3 position)
        {
            float3 resultant = float3.zero;

            ForceWalkTraversal(_root, entity, mass, position, ref resultant);

            return resultant;
        }
    }
}
