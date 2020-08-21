using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.EarthBuilder.Systems
{
    public class BarnesHutTree
    {
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
    }
}
