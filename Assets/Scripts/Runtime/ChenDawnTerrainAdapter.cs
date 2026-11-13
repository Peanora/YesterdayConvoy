using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    internal static class ChenDawnTerrainAdapter
    {
        internal static void Configure(ManualBattle battle)
        {
            if(battle==null)return;
            bool needed=false;foreach(var defender in battle.Defenders)needed|=defender.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak;
            if(!needed)return;
            var obstacles=new List<Bounds>();var gates=new List<Bounds>();Bounds board=default;bool found=false;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(renderer.gameObject.scene!=SceneManager.GetActiveScene()||!renderer.gameObject.activeInHierarchy)continue;
                string name=renderer.gameObject.name;
                if(!name.StartsWith("Cell ",StringComparison.Ordinal)&&!name.StartsWith("Platform cap ",StringComparison.Ordinal))continue;
                var center=renderer.bounds.center;
                // Authoring gaps in the drawn .985 tile/.94 cap are visual bevels.
                // Gameplay uses complete original one-cell highland footprints.
                var cell=new Bounds(new Vector3(center.x,0,center.z),new Vector3(1,10,1));
                if(!found){board=cell;found=true;}else board.Encapsulate(cell);
                if(name.StartsWith("Platform cap ",StringComparison.Ordinal))obstacles.Add(cell);
            }
            if(!found)throw new InvalidOperationException("Chen S3 requires the saved battlefield Cell / Platform cap meshes.");
            foreach(var lane in battle.Lanes)
            {
                gates.Add(new Bounds(lane.Entry,new Vector3(1,10,1)));
                gates.Add(new Bounds(lane.Exit,new Vector3(1,10,1)));
            }
            battle.ConfigureChenDawnTerrain(obstacles,board,gates);
        }
    }
}
