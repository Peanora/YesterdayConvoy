using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Whole one-unit cells, in the original right-facing effect frame. Soft
    // light/smoke outside the marked edge is not an extra damage radius.
    internal static class DefenderSkillFootprints
    {
        static readonly Vector2Int[] square3=Rows((-1,-1,1),(0,-1,1),(1,-1,1));
        static readonly Vector2Int[] centered21=Rows((-2,-1,1),(-1,-2,2),(0,-2,2),(1,-2,2),(2,-1,1));
        static readonly Vector2Int[] silver=Rows((-1,0,2),(0,0,3),(1,0,2));
        static readonly Vector2Int[] chess=Rows((-2,0,4),(-1,0,4),(0,0,4),(1,0,4),(2,0,4));
        static readonly Vector2Int[] saria=Rows((-3,0,0),(-2,-1,1),(-1,-2,2),(0,-3,3),(1,-2,2),(2,-1,1),(3,0,0));
        static readonly Vector2Int[] mlynar=Rows((-2,0,1),(-1,0,2),(0,0,3),(1,0,2),(2,0,1));
        static readonly Vector2Int[] chen=Rows((-1,0,1),(0,0,3),(1,0,1));
        static readonly Vector2Int[] angelina=Rows((-2,0,2),(-1,0,3),(0,0,4),(1,0,3),(2,0,2));
        static readonly Vector2Int[] clemensia=Rows((0,0,3));
        static Vector2Int[] Rows(params (int row,int first,int last)[] rows)
        {var result=new List<Vector2Int>();foreach(var row in rows)for(int col=row.first;col<=row.last;col++)result.Add(new Vector2Int(col,row.row));return result.ToArray();}
        internal static bool TryGet(DefenderSkillKind kind,out IReadOnlyList<Vector2Int> cells)
        {
            switch(kind)
            {
                case DefenderSkillKind.Mon3trMeltdown:cells=square3;return true;
                case DefenderSkillKind.HoshigumaInferno:case DefenderSkillKind.PramanixSnow:cells=centered21;return true;
                case DefenderSkillKind.SilverAshReign:cells=silver;return true;
                case DefenderSkillKind.WangGambit:cells=chess;return true;
                case DefenderSkillKind.SariaCalcification:cells=saria;return true;
                case DefenderSkillKind.MlynarGlory:cells=mlynar;return true;
                case DefenderSkillKind.ChenDawnstreak:cells=chen;return true;
                case DefenderSkillKind.AngelinaWishes:cells=angelina;return true;
                case DefenderSkillKind.ClemensiaCorrosion:cells=clemensia;return true;
                default:cells=null;return false;
            }
        }
        internal static bool IsMarkedEffect(string id,string raw)=>id=="D25"&&raw.StartsWith("hsgma2_skill_03_range",StringComparison.Ordinal)||
            id=="D26"&&raw=="svash2_skill_03_range_01"||id=="D28"&&raw.StartsWith("wang_skill_03_range",StringComparison.Ordinal)||
            id=="D29"&&raw=="demkni_skill_03_range"||id=="D30"&&(raw=="mlynar_skill_03_buff_03"||raw=="mlynar_skill_03_range_01")||
            id=="D32"&&raw.StartsWith("chen3_skill_03_range_01",StringComparison.Ordinal)||id=="D34"&&raw.StartsWith("clemnt_skill_03_range",StringComparison.Ordinal);
        internal static Vector3 Cardinal(Vector3 direction)=>Mathf.Abs(direction.x)>=Mathf.Abs(direction.z)?new Vector3(direction.x<0?-1:1,0,0):new Vector3(0,0,direction.z<0?-1:1);
        internal static bool Contains(IReadOnlyList<Vector2Int> cells,Vector3 origin,Vector3 facing,Vector3 position)
        {
            facing=Cardinal(facing);var side=new Vector3(-facing.z,0,facing.x);var delta=position-origin;
            float col=Vector3.Dot(delta,facing),row=Vector3.Dot(delta,side);
            foreach(var cell in cells)if(Mathf.Abs(col-cell.x)<=.50001f&&Mathf.Abs(row-cell.y)<=.50001f)return true;
            return false;
        }
        internal static float EnclosingRadius(IReadOnlyList<Vector2Int> cells)
        {float squared=0;foreach(var c in cells)squared=Mathf.Max(squared,Mathf.Pow(Mathf.Abs(c.x)+.5f,2)+Mathf.Pow(Mathf.Abs(c.y)+.5f,2));return Mathf.Sqrt(squared);}
    }

    public sealed partial class ManualBattle
    {
        readonly Dictionary<(string source,string owner),Vector3> defenderRangeFacings=new Dictionary<(string,string),Vector3>();
        Vector3 PickDefenderRangeFacing(DefenderState d)
        {
            var target=FindUnit(d.TargetId);Vector3 point;
            if(CanDefenderTarget(target))point=target.Motion.Position;
            else
            {
                DeployedUnitState nearest=null;
                foreach(var u in units)if(CanDefenderTarget(u)&&(nearest==null||GroundDistance(d.Position,u.Motion.Position)<GroundDistance(d.Position,nearest.Motion.Position)))nearest=u;
                point=nearest!=null?nearest.Motion.Position:Lanes.Count>0?Lanes[0].Entry:d.Position+Vector3.right;
            }
            return DefenderSkillFootprints.Cardinal(point-d.Position);
        }
        void CaptureDefenderSkillFacing(DefenderState d)
        {
            defenderRangeFacings[(d.EntityId,d.EntityId)]=d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak?ChenDawnFacing(d):PickDefenderRangeFacing(d);
            var partner=FindDefender(d.PartnerId);
            if(d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign&&partner!=null&&partner.Alive)defenderRangeFacings[(d.EntityId,partner.EntityId)]=PickDefenderRangeFacing(partner);
        }
        internal Vector3 DefenderSkillFacing(DefenderState d,DefenderState owner=null)
        {
            owner=owner??d;
            if(d.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak&&DefenderSkillActive(d))return ChenDawnFacing(d);
            return defenderRangeFacings.TryGetValue((d.EntityId,owner.EntityId),out var facing)?facing:PickDefenderRangeFacing(owner);
        }
        internal IReadOnlyList<Vector2Int> DefenderSkillCells(DefenderState d)=>DefenderSkillFootprints.TryGet(d.Settings.Skill.Kind,out var cells)?cells:null;
        public bool DefenderSkillContains(DefenderState d,Vector3 position)=>DefenderSkillContainsFrom(d,d,position);
        internal bool DefenderSkillContainsFrom(DefenderState source,DefenderState owner,Vector3 position)
        {
            if(source==null||owner==null)return false;
            if(DefenderSkillFootprints.TryGet(source.Settings.Skill.Kind,out var cells))return DefenderSkillFootprints.Contains(cells,owner.Position,DefenderSkillFacing(source,owner),position);
            float radius=source.Settings.Skill.EffectRadius>0?source.Settings.Skill.EffectRadius:source.Settings.Range;
            return GroundDistance(owner.Position,position)<=radius;
        }
        public bool DefenderRangeContains(DefenderState d,Vector3 position)
        {
            if(d==null)return false;
            if(!DefenderSkillActive(d)&&!IsFifthActive(d))return GroundDistance(d.Position,position)<=d.Settings.Range;
            if(DefenderSkillContains(d,position))return true;
            var partner=FindDefender(d.PartnerId);
            return d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign&&partner!=null&&partner.Alive&&DefenderSkillContainsFrom(d,partner,position);
        }
    }
}
