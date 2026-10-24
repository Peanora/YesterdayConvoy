using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        readonly HashSet<Vector2Int> rubbleTiles=new HashSet<Vector2Int>();
        public IEnumerable<Vector2Int> RubbleTiles=>rubbleTiles;
        void CollapsePillar(FacilityState f,Vector3 origin)
        {
            if(f.Collapsed)return;
            f.Collapsed=true;Activate(f);
            Vector3 delta=f.Settings.Position-origin;
            f.FallDirection=Mathf.Abs(delta.x)>Mathf.Abs(delta.z)?new Vector3(delta.x>=0?1:-1,0,0):new Vector3(0,0,delta.z>=0?1:-1);
            for(int i=1;i<=2;i++)
            {
                Vector3 position=f.Settings.Position+f.FallDirection*i;var cell=Cell(position);
                // Only declared deployable ground receives rubble; fixed routes stop at it (no rerouting).
                foreach(var ground in sealCandidates)if(Cell(ground)==cell){rubbleTiles.Add(cell);break;}
                foreach(var d in defenders)if(d.Alive&&Cell(d.Position)==cell)TrueDamage(d,d.Health);
                foreach(var u in units)
                    if(u.Active&&Cell(u.Motion.Position)==cell)
                    {TrueDamage(u,f.Settings.Power);u.PillarStunnedUntil=Clock+3;if(CombatSettings.Effects.Flamerazer)EndSpray(u,true);}
                foreach(var other in facilities)
                    if(other.Settings.Kind==FacilityKind.Pillar&&!other.Collapsed&&Cell(other.Settings.Position)==cell)CollapsePillar(other,f.Settings.Position);
            }
        }
        bool PillarObstructs(DeployedUnitState u,float step)
        {
            Vector3 next=u.Motion.Position+u.Motion.Direction*(u.Motion.Speed*step);
            foreach(var cell in rubbleTiles)
                if(Mathf.Abs(next.x-cell.x)<.65f&&Mathf.Abs(next.z-cell.y)<.65f)return true;
            return false;
        }
    }
}
