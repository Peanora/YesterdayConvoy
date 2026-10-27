using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    [Serializable]
    public sealed class DefenderPlacement
    {
        public string id, displayName, artId;
        public Vector3 position;
        public float health=180, attack=12, armor=4, range=2.5f, interval=2, windup=.4f, actionDuration=1;
        public int blocks;
        public DefenderRole role;
        public CombatDamageType damageType;
        public float splashRadius;
        public float healingRange;
        public DefenderSettings Snapshot() => new DefenderSettings(id, displayName, artId, position,
            health, attack, armor, range, interval, blocks, role, damageType, windup, actionDuration, splashRadius, healingRange:healingRange);
    }

    [CreateAssetMenu(menuName="Yesterday Convoy/Defender Trial")]
    public sealed class DefenderTrialDefinition : ScriptableObject
    {
        public ManualBattleDefinition battle;
        public Vector3[] route;
        public DefenderPlacement[] defenders;
        public Material[] surfaces;
        public string mapId, mapTitle;
        public bool autumnRuins;
        public TacticalLane[] lanes;
        public Vector2Int[] raised;
        public Texture2D floorTexture, foliageTexture;
        public ManualBattleDefinition[] rosterCards;
        public bool Tactical => lanes!=null && lanes.Length>0;
        public DeploymentLane[] LaneSnapshot()
        {
            if(!Tactical) return null;
            var result=new DeploymentLane[lanes.Length];
            for(int i=0;i<result.Length;i++) result[i]=new DeploymentLane(lanes[i].id,lanes[i].exitId,lanes[i].points);
            return result;
        }
        public DefenderSettings[] Snapshot()
        {
            if (battle==null || defenders==null || defenders.Length!=8) throw new InvalidOperationException("Eight defenders required.");
            var ids=new HashSet<string>(); var result=new DefenderSettings[defenders.Length];
            for(int i=0;i<result.Length;i++)
            {
                result[i]=defenders[i].Snapshot();
                if(!ids.Add(result[i].Id)) throw new InvalidOperationException("Duplicate defender ID.");
                if(string.IsNullOrWhiteSpace(result[i].ArtId)) throw new InvalidOperationException("Missing defender art.");
            }
            _=new RouteMotion(route, battle.Snapshot().Speed);
            return result;
        }
    }
    [Serializable]
    public sealed class TacticalLane
    {
        public string id,exitId;
        public Vector3[] points;
    }
}
