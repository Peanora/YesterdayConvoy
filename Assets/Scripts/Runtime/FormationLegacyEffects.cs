using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    // Read-only migration of the existing, approved native effects. This owns no
    // damage, healing, skill casts or phase transitions. Ordinary play still uses
    // the v0.7 mechanics; a visual clip never creates an original-game mechanic.
    public sealed partial class FormationBattleController
    {
        sealed class LegacyFxView
        {
            internal SpriteRenderer Renderer;
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
        }
        sealed class LegacyPatriotPage {internal Texture2D Texture;internal Sprite Sprite;internal int LastUse;}
        sealed class LegacyComicBurst {internal LineRenderer Ink,Light;}
        [Serializable] sealed class LegacyPatriotCatalog {public int fps;public LegacyPatriotClip[] clips;}
        [Serializable] sealed class LegacyPatriotClip {public string name;public int frames;public string[] pages;public float worldSize,anchorX,anchorY;}
        [Serializable] sealed class LegacyTalulahManifest {public int fps;public LegacyTalulahClip[] clips;}
        [Serializable] sealed class LegacyTalulahClip
        {public string key;public int count,width,height,columns;public float ppu,pivotX,pivotY,duration,hdrRange;public LegacyTalulahPage[] pages;}
        [Serializable] sealed class LegacyTalulahPage {public string file;public int count;}
        sealed class LegacyTalulahFrames {internal LegacyTalulahClip Data;internal Sprite[] Frames;}
        readonly List<LegacyFxView> legacyFxViews=new List<LegacyFxView>();
        readonly Dictionary<string,LegacyPatriotPage> legacyPatriotPages=new Dictionary<string,LegacyPatriotPage>();
        readonly Dictionary<string,LegacyTalulahFrames> legacyTalulahFrames=new Dictionary<string,LegacyTalulahFrames>();
        readonly Dictionary<int,float> legacyPreviousShield=new Dictionary<int,float>();
        readonly Dictionary<int,double> legacyShieldBroken=new Dictionary<int,double>();
        readonly Dictionary<int,double> legacyShieldRefreshed=new Dictionary<int,double>();
        readonly Dictionary<int,Vector3> legacyFlightOrigins=new Dictionary<int,Vector3>();
        readonly Dictionary<Vector2Int,double> legacySeaBorn=new Dictionary<Vector2Int,double>();
        readonly List<MeshRenderer> legacySeaTiles=new List<MeshRenderer>();
        readonly List<LegacyComicBurst> legacyComicBursts=new List<LegacyComicBurst>();
        readonly Dictionary<FormationHit,int> legacyComicFaceIds=new Dictionary<FormationHit,int>();
        readonly HashSet<(int SourceId,int TargetId,double At,string Clip)> guardHitSprites=new HashSet<(int,int,double,string)>();
        LegacyPatriotCatalog legacyPatriotCatalog;
        LegacyTalulahManifest legacyTalulahManifest;
        Material legacyHdrMaterial,legacyPremultMaterial,legacyPatriotMaterial,legacySeaMaterial,legacyComicMaterial;
        Material formationFlameSprayMaterial,formationFlameCoreMaterial;
        Sprite[] formationFlameSprayFrames;
        Mesh legacySeaMesh;
        Sprite guardWarningTile;
        FormationBattleModel legacyObservedModel;
        double legacyLastClock=-1;
        int legacyFxUsed,legacyComicUsed,legacyComicFaceSequence;
        const float LegacyVisualRate=2;
        const string KingdomRangeArt="boss/B15/enemy_hlystp_skill01_range";

        const string GuardPhysicalHit = "common/common_hit_01";
        const string GuardMagicHit = "common/common_magic_hit_01";
        const string GuardFireHit = "common/common_firemagic_hit_01";
        const string GuardHealHit = "common/common_heal_hit_01";
        const string GuardHealCast = "common/common_heal_start_01";
        const string GuardArrowTrail = "common/common_gongjian_trail_01";
        const string GuardMagicTrail = "common/common_magic_trail_01";
        const string GuardLavaTargetCast = "lava_attack_01_hit";
        const string GuardChessPlaced = "wang_passive_01_start";
        const string GuardChessArmed = "wang_passive_01_buff";
        const string GuardChessTriggered = "wang_passive_01_buff_02";

        static readonly Dictionary<string,string[]> FormationGuardEffectKeys=new Dictionary<string,string[]>
        {
            ["D01"]=new[]{GuardPhysicalHit},
            ["D02"]=new[]{GuardPhysicalHit},
            ["D03"]=new[]{GuardPhysicalHit},
            ["D04"]=new[]{GuardPhysicalHit},
            ["D05"]=new[]{"midn_attack_01_hit","midn_skill_01_hit","midn_attack_01_trail","midn_skill_01_trail"},
            ["D06"]=new[]{GuardPhysicalHit},
            ["D07"]=new[]{GuardPhysicalHit,GuardArrowTrail},
            ["D08"]=new[]{GuardMagicHit,GuardMagicTrail,"stward_skill_01_hit","stward_skill_01_trail"},
            ["D09"]=new[]{GuardLavaTargetCast,GuardFireHit},
            ["D10"]=new[]{GuardHealCast,GuardHealHit},
            ["D11"]=new[]{GuardHealHit},
            ["D12"]=new[]{"orchid_attack_01_hit","orchid_attack_01_trail"},
            ["D21"]=new[]{"whitw2_attack_01_start","whitw2_skill_03_start_01","whitw2_skill_03_buff_start","whitw2_skill_03_token_trail_01","whitw2_skill_03_token_hit_01","whitw2_hit_01","whitw2_attack_01_trail","whitw2_skill_03_trail_01"},
            ["D22"]=new[]{"yu_attack_01","yu_skill3_attack_01","yu_skill3_range_01","yu_skill3_range_01_l","yu_skill3_range_01_r","yu_skill3_buff_01","yu_hit_01","yu_skill3_hit_01"},
            ["D23"]=new[]{"monstr_attack_start_01","monstr_attack_hit_01","monstr_skill_03_start_01","monstr_skill_03_buff_01","monstr_skill_03_hit","monstr_attack_trail"},
            ["D24"]=new[]{"angel2_attack_loop_start_01","angel2_skill_3_loop_start_01","angel2_skill_3_loop_hit_01","angel2_attack_loop_hit_01","angel2_attack_loop_trail_01","angel2_skill_3_loop_trail_01"},
            ["D25"]=new[]{"hsgma2_attack_01_start_01","hsgma2_skill_03_buff_01","hsgma2_attack_01_hit_01","hsgma2_skill_03_start_01","hsgma2_skill_03_hit_01"},
            ["D26"]=new[]{"svash2_attack_start_a_01","svash2_attack_start_a_02","svash2_skill_03_combat_start_01_2","svash2_skill_03_combat_start_02","svash2_attack_hit_01"},
            ["D27"]=new[]{"sbell2_skill_03_start_01","sbell2_skill_03_hit"},
            ["D28"]=new[]{GuardChessPlaced,GuardChessArmed,GuardChessTriggered},
            ["D29"]=new[]{"demkni_skill_03_self","demkni_attack_01_hit",GuardHealHit},
            ["D30"]=new[]{"mlynar_skill_03_buff_03","mlynar_skill_03_start_01","mlynar_skill_03_hit_01"},
        };

        const string PramanixEffects = "defender/D27/";
        internal const string PramanixIceRendererPrefix = "Formation freeze / ";
        internal const string PramanixSnowRendererPrefix = "Formation snow / ";
        internal const string PramanixImpactRendererPrefix = "Formation Pramanix impact / ";
        static readonly string[] PramanixBossEffectKeys =
        {
            "sbell2_skill_03_start_01", "sbell2_skill_03_hit",
            "sbell2_skill_02_buff_01", "sbell2_skill_02_start_01", "sbell2_skill_01_hit", "sbell2_buff_01_hit_02",
            "sbell2_ground_01", "sbell2_ground_02", "sbell2_ground_03", "sbell2_revive",
            "buff_frozen", "buff_frozen_to_walk", "sbell2_skill_03_flow"
        };

        IEnumerable<ArtSequence> FormationGuardEffects(string id)
        {
            bool pramanixBoss = id == PramanixBossBodyArt;
            string guardId = pramanixBoss ? "D27" : id;
            if(!FormationGuardEffectKeys.TryGetValue(guardId,out var keys))yield break;
            if(pramanixBoss)keys=PramanixBossEffectKeys;
            foreach(string key in keys)
                if(OriginalCombatVfxArt.TryGet(FormationGuardFxKey(guardId,key),out var clip))yield return clip.Sequence;
        }

        static string FormationGuardFxKey(string id, string clip) =>
            clip.StartsWith("common/", StringComparison.Ordinal) ? clip : "defender/" + id + "/" + clip;

        void PresentFormationGuardEffects()
        {
            if(Campaign==null||!ViewingDefenders)return;
            double now=Model.Clock;
            foreach(var guard in Model.Defenders)
            {
                if(!guard.Alive)continue;
                if(guard.IsSnowBoss)
                {
                    PresentPramanixBoss(guard, now);
                    continue;
                }
                string id=guard.ArtId;
                var effect=Model.GuardEffect(guard);
                if(effect!=null&&now>=effect.StartedAt&&now<=effect.EndsAt)
                {
                    double age=(now-effect.StartedAt)*FormationVisualRate;
                    Vector2 origin=effect.Origin;
                    switch(effect.Kind)
                    {
                        case "firewall":
                            var axis=new Vector2(-effect.Direction.y,effect.Direction.x);
                            string wall=Mathf.Abs(axis.x)>Mathf.Abs(axis.y)?"yu_skill3_range_01":effect.Direction.x>=0?"yu_skill3_range_01_r":"yu_skill3_range_01_l";
                            int children=Mathf.Max(1,Mathf.CeilToInt(effect.Length));
                            for(int i=0;i<children;i++)
                            {
                                var cell=origin+axis*(((i+.5f)/children-.5f)*effect.Length);
                                if(cell.x>=FormationBattleModel.BattleAreaStart&&cell.x<FormationBattleModel.Width&&cell.y>=0&&cell.y<FormationBattleModel.Height)
                                    LegacyOriginal("defender/D22/"+wall,age,cell,11,true,true);
                            }
                            LegacyOriginal("defender/D22/yu_skill3_buff_01",age,guard.Position,18,true,true);
                            break;
                        case "hunt":
                            LegacyOriginal("defender/D21/whitw2_skill_03_buff_start",age,guard.Position,12,true,true);
                            LegacyOriginal("defender/D21/whitw2_skill_03_token_trail_01",age,origin,23,true,explicitPosition:ActorWorldPoint(origin)+Vector3.up*.7f);
                            break;
                        case "snow":
                            LegacyOriginal("defender/D27/sbell2_skill_03_start_01",age,origin,24,flat:true);
                            PresentGuardSnowField(effect);
                            break;
                        case "chess":
                        case "chess_hit":
                            PresentFormationGuardChess(origin,age,effect.Kind=="chess_hit",combat: CombatViewActive);
                            break;
                        case "slash":
                            if(id=="D30")
                            {
                                LegacyOriginal("defender/D30/mlynar_skill_03_buff_03",age,guard.Position,12,true,true);
                                if(now<effect.ImpactAt)PresentGuardSlashWarning(effect,now);
                            }
                            break;
                    }
                }
                if(Model.GuardTransformed(guard))
                    LegacyOriginal("defender/D23/monstr_skill_03_buff_01",now*FormationVisualRate,guard.Position,12,true,true);
                if(guard.Shield>0&&id=="D25")
                    LegacyOriginal("defender/D25/hsgma2_skill_03_buff_01",now*FormationVisualRate,guard.Position,12,true,true);
                if(guard.Shield>0&&id=="D29")
                    LegacyOriginal("defender/D29/demkni_skill_03_self",now*FormationVisualRate,guard.Position,12,true,true);
            }
            foreach(var action in Model.PresentationActions)
            {
                if(action.ActorId<100||action.Cancelled)continue;
                var guard=Model.Defenders.FirstOrDefault(d=>d.Id==action.ActorId);
                if(guard==null)continue;
                if(guard.IsSnowBoss)
                {
                    string effect = action.Animation switch
                    {
                        "skill_2_attack" => "sbell2_skill_02_start_01",
                        "skill_attack" or "skill_attack_b" or "skill_attack_c" => "sbell2_skill_03_start_01",
                        _ => null
                    };
                    if(effect!=null && !(guard.SnowBossTransitionStartedAt>0 && action.At<=guard.SnowBossTransitionStartedAt))
                        LegacyOriginal(PramanixEffects+effect,(now-(action.Animation=="skill_2_attack"?action.ContactAt:action.At))*action.Rate,action.From,22,
                            flat:true,scale:1.45f,gammaOutput:true);
                    continue;
                }
                if(!action.ProjectileOnly)PresentFormationGuardCast(guard, action, now);
                PresentFormationGuardProjectile(guard, action, now);
            }
            guardHitSprites.Clear();
            foreach(var hit in Model.Hits)
            {
                var pramanix = hit.SourceArtId == "D27" ? Model.Defenders.FirstOrDefault(d => d.Id == hit.FromId && d.IsSnowBoss) : null;
                string clip=pramanix == null ? FormationGuardHitClip(hit) : PramanixBossHit(pramanix, hit);
                if(clip==null)continue;
                var stamp=(hit.FromId,hit.ToId,hit.At,clip);
                if(guardHitSprites.Contains(stamp))
                {
                    if(pramanix==null)formationImpactOwners.Add(hit);
                    continue;
                }
                double age=(now-hit.At)*FormationVisualRate;
                if(pramanix==null)
                {
                    var target=FormationFeedbackActor(hit.ToId);
                    var contactPoint=FormationFeedbackContactPoint(target,hit.To,hit.Heal ? .62f : .52f);
                    bool left=Vector3.Dot(ActorWorldPoint(hit.To)-ActorWorldPoint(hit.From),battleCamera.transform.right)<0;
                    if (LegacyOriginal(FormationGuardFxKey(hit.SourceArtId,clip),age,hit.To,35,
                        explicitPosition:contactPoint,flip:left)!=null)
                    {
                        guardHitSprites.Add(stamp);
                        formationImpactOwners.Add(hit);
                    }
                    continue;
                }
                guardHitSprites.Add(stamp);
                if(clip=="sbell2_buff_01_hit_02")
                {
                    LegacyOriginal(PramanixEffects+clip,age,hit.To,-6,gammaOutput:true);
                    continue;
                }
                var victim=FormationFeedbackActor(hit.ToId);
                var contact=FormationFeedbackContactPoint(victim,hit.To,.48f);
                var impact=LegacyOriginal(PramanixEffects+clip,age,hit.To,24,explicitPosition:contact,gammaOutput:true);
                if(impact!=null)impact.name=PramanixImpactRendererPrefix+hit.ToId;
            }
        }

        void PresentFormationGuardCast(FormationDefender guard, FormationPresentationAction action, double now)
        {
            bool ordinary = action.Animation == "attack";
            bool empowered = action.Animation == "skill_attack";
            string clip = guard.ArtId switch
            {
                "D10" when action.Heal => GuardHealCast,
                "D21" when ordinary => "whitw2_attack_01_start",
                "D21" when empowered => "whitw2_skill_03_start_01",
                "D22" when ordinary => "yu_attack_01",
                "D22" when empowered => "yu_skill3_attack_01",
                "D23" when ordinary && action.Heal => "monstr_attack_start_01",
                "D23" when empowered => "monstr_skill_03_start_01",
                "D24" when ordinary => "angel2_attack_loop_start_01",
                "D24" when empowered => "angel2_skill_3_loop_start_01",
                "D25" when ordinary => "hsgma2_attack_01_start_01",
                "D25" when empowered => "hsgma2_skill_03_start_01",
                "D26" when ordinary => "svash2_attack_start_a_01",
                "D26" when empowered => "svash2_skill_03_combat_start_01_2",
                "D30" when empowered => "mlynar_skill_03_start_01",
                _ => null
            };
            if(guard.ArtId == "D09" && action.TargetId != -1)
            {
                // The native cast combines view and local particle alignment in one camera sample.
                // Keep its target-cell pivot and the captured orientation, with contact drawn separately above it.
                LegacyOriginal(FormationGuardFxKey(guard.ArtId, GuardLavaTargetCast),
                    (now-action.LaunchedAt)*action.Rate,action.To,6);
            }
            if(clip==null)return;

            // Hibiscus and Exusiai use the original spell-on event. The remaining retained cast clips start with the body.
            double startedAt = guard.ArtId is "D10" or "D24" ? action.LaunchedAt : action.At;
            bool left = Vector3.Dot(ActorWorldPoint(action.To)-ActorWorldPoint(action.From),battleCamera.transform.right)<0;
            double age = (now-startedAt)*action.Rate;
            var anchor = guard.ArtId == "D24" ? FormationGuardMuzzle(guard, action, left) : LegacyOwnerPoint(guard,action.From);
            LegacyOriginal(FormationGuardFxKey(guard.ArtId,clip),age,action.From,23,explicitPosition:anchor,flip:left);
            if(guard.ArtId=="D26")
                LegacyOriginal(FormationGuardFxKey(guard.ArtId,ordinary?"svash2_attack_start_a_02":"svash2_skill_03_combat_start_02"),
                    age,action.From,24,explicitPosition:anchor,flip:left);
        }

        Vector3 FormationGuardMuzzle(FormationDefender guard, FormationPresentationAction action, bool left)
        {
            // Original front SkeletonBinary bone positions at each retained action's OnAttack marker.
            Vector2 bone = guard.ArtId switch
            {
                "D05" => new Vector2(116.09f, 21.64f),
                "D07" => new Vector2(234.02f, 238.66f),
                "D08" => new Vector2(196.08f, 139.61f),
                "D12" => new Vector2(285.77f, 139.18f),
                "D21" when action.Animation == "skill_attack" => new Vector2(358.50f, 191.20f),
                "D21" => new Vector2(90.84f, 521.78f),
                "D23" => new Vector2(-112, 397.60f),
                "D24" => new Vector2(203.58f, 166.23f),
                _ => Vector2.zero
            };
            var foot = LegacyOwnerPoint(guard, action.From);
            if(!art.TryGetValue(guard.ArtId, out var source))return foot;
            // The front body export sampled .38 atlas pixels per skeleton pixel.
            float worldPerSourcePixel = .38f / source.Ppu;
            return foot + battleCamera.transform.right * (bone.x * worldPerSourcePixel * (left ? -1 : 1)) +
                battleCamera.transform.up * (bone.y * worldPerSourcePixel);
        }

        void PresentFormationGuardProjectile(FormationDefender guard, FormationPresentationAction action, double now)
        {
            if(action.ProjectileMovement == null || now < action.LaunchedAt || now >= action.ContactAt)return;
            var target = FormationFeedbackActor(action.TargetId);
            if(target?.Alive != true || !guard.Alive)return;
            string trail = guard.ArtId switch
            {
                "D05" => action.Skill ? "midn_skill_01_trail" : "midn_attack_01_trail",
                "D07" => GuardArrowTrail,
                "D08" => action.Skill ? "stward_skill_01_trail" : GuardMagicTrail,
                "D12" => "orchid_attack_01_trail",
                "D21" => action.Animation == "skill_attack" ? "whitw2_skill_03_trail_01" : "whitw2_attack_01_trail",
                "D23" => "monstr_attack_trail",
                "D24" => action.Skill ? "angel2_skill_3_loop_trail_01" : "angel2_attack_loop_trail_01",
                _ => null
            };
            if(trail == null)return;
            bool left = Vector3.Dot(ActorWorldPoint(action.To)-ActorWorldPoint(action.From),battleCamera.transform.right)<0;
            var start = FormationGuardMuzzle(guard, action, left);
            var end = FormationFeedbackContactPoint(target,target.Position,action.Heal ? .62f : .52f);
            double elapsed = now - action.LaunchedAt;
            float progress = (float)(elapsed / (action.ContactAt-action.LaunchedAt));
            Vector2 cell = Vector2.Lerp(action.From,target.Position,progress);
            LegacyOriginal(FormationGuardFxKey(guard.ArtId,trail),elapsed*action.Rate,cell,32,loop:guard.ArtId!="D07",
                explicitPosition:Vector3.Lerp(start,end,progress),direction:guard.ArtId=="D08" ? null : end-start);
        }

        static string FormationGuardHitClip(FormationHit hit) => hit.SourceArtId switch
        {
            "D01" or "D02" or "D03" or "D04" or "D06" or "D07" => GuardPhysicalHit,
            "D05" => hit.Skill ? "midn_skill_01_hit" : "midn_attack_01_hit",
            "D08" => hit.Skill ? "stward_skill_01_hit" : GuardMagicHit,
            "D09" => GuardFireHit,
            "D10" or "D11" when hit.Heal => GuardHealHit,
            "D12" => "orchid_attack_01_hit",
            "D21" => hit.Skill ? "whitw2_skill_03_token_hit_01" : "whitw2_hit_01",
            "D22" => hit.Action?.Animation == "skill_attack" ? "yu_skill3_hit_01" : "yu_hit_01",
            "D23" => hit.Heal ? "monstr_attack_hit_01" : "monstr_skill_03_hit",
            "D24" => hit.Skill ? "angel2_skill_3_loop_hit_01" : "angel2_attack_loop_hit_01",
            "D25" => hit.Action?.Animation == "skill_attack" ? "hsgma2_skill_03_hit_01" : "hsgma2_attack_01_hit_01",
            "D26" => "svash2_attack_hit_01",
            "D27" => "sbell2_skill_03_hit",
            "D29" => hit.Heal ? GuardHealHit : "demkni_attack_01_hit",
            "D30" => "mlynar_skill_03_hit_01",
            _ => null
        };

        static string PramanixBossHit(FormationDefender guard, FormationHit hit)
        {
            if(guard.SnowBossTransitionStartedAt>0 && hit.At<=guard.SnowBossTransitionStartedAt)return null;
            return hit.Action?.Animation switch
            {
                "skill_2_attack" => "sbell2_skill_01_hit",
                "SnowBossS2Tick" => "sbell2_buff_01_hit_02",
                "skill_attack" or "skill_attack_b" or "skill_attack_c" => "sbell2_skill_03_hit",
                _ => null
            };
        }

        void PresentPramanixBoss(FormationDefender guard, double now)
        {
            double s2StartedAt = Model.SnowBossS2StartedAt(guard);
            if(s2StartedAt>=0)
                PresentPramanixWind("sbell2_skill_02_buff_01",(now-s2StartedAt)*FormationVisualRate,guard.Position,16,PramanixBossScale);
            var snowCells = Model.SnowBossSnow(guard);
            foreach(var snow in snowCells)
            {
                string texture = snow.Value == 1 ? "sbell2_ground_01" : snow.Value < 5 ? "sbell2_ground_02" : "sbell2_ground_03";
                Vector2 cell = snow.Key;
                // Keep the native snow pattern and show the whole affected tile on the gray industrial deck.
                var tint = new Color(.61f,.78f,.91f,.35f+(snow.Value-1)*.065f);
                var coverage=DrawGuardGroundCell(cell,tint);
                if(coverage!=null)
                {
                    coverage.transform.localScale=Vector3.one*.96f;
                    coverage.sortingOrder=29;
                }
                double age = .8 + (now*FormationVisualRate+cell.x*.17+cell.y*.31)%1.6;
                var clip=OriginalCombatVfxArt.Get(PramanixEffects+texture);
                var snowTile=LegacyDraw(clip.At(age),legacyHdrMaterial,ActorWorldPoint(cell)+Vector3.up*.035f,
                    cell,1,-12,hdr:clip.HdrRange,flat:true,gammaOutput:true);
                if(snowTile!=null)
                {
                    snowTile.sortingOrder=30;
                    snowTile.name=PramanixSnowRendererPrefix+snow.Key.x+","+snow.Key.y;
                }
            }
            var field = Model.GuardEffect(guard);
            if(field != null && field.Kind == "snow")
            {
                PresentPramanixWind("sbell2_skill_03_flow",(now-field.StartedAt)*FormationVisualRate,guard.Position,20,1.45f);
            }
            if(!Model.SnowBossTransitionActive(guard))return;
            double transitionAge = now-guard.SnowBossTransitionStartedAt;
            LegacyOriginal(PramanixEffects+"sbell2_revive",transitionAge*FormationVisualRate,guard.Position,28,
                flat:true,scale:PramanixBossScale);
            // The status particles advance on the battle clock, independently of attack animation speed.
            var selfIce = OriginalCombatVfxArt.Get(PramanixEffects+"buff_frozen");
            var targetIce = OriginalCombatVfxArt.Get(PramanixEffects+"buff_frozen_to_walk");
            PresentPramanixIce(guard,selfIce.At(transitionAge),selfIce.HdrRange,PramanixScoutLabelTop*PramanixBossScale);
            var targetIceFrame = targetIce.At(transitionAge);
            foreach(var fighter in Model.Units.Concat(Model.Summons))
                if(Model.SnowBossFreezeRemaining(fighter)>0)
                    PresentPramanixIce(fighter,targetIceFrame,targetIce.HdrRange,
                        Mathf.Max(ActorPresentationHeight(fighter),Mathf.Max(fighter.Footprint.x,fighter.Footprint.y)));
        }

        void PresentPramanixIce(FormationActor actor, Sprite frame, float hdrRange, float bodyHeight)
        {
            float scale = Mathf.Max(1.15f,bodyHeight*.82f);
            var shell = LegacyDraw(frame,legacyHdrMaterial,FormationDisplayPoint(actor)+Vector3.up*.025f,
                actor.Position,scale,12,hdr:hdrRange);
            if(shell==null)return;
            // Fit the native ice to the occupant while leaving the head above the shell.
            shell.transform.localScale=new Vector3(scale,scale*1.65f,scale);
            shell.name=PramanixIceRendererPrefix+actor.Id;
        }

        void PresentPramanixWind(string effect, double age, Vector2 cell, int order, float scale)
        {
            // The first native sample includes the fade-in. Repeat only the settled section.
            if(age>=3.5)age=1+(age-1)%2.5;
            LegacyOriginal(PramanixEffects+effect,age,cell,order,flat:true,scale:scale,gammaOutput:true);
        }

        SpriteRenderer DrawGuardGroundCell(Vector2 cell,Color color)
        {
            if(guardWarningTile==null)
            {
                var texture=Texture2D.whiteTexture;
                guardWarningTile=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width);
                guardWarningTile.name="Formation defense range tile";
            }
            var marker=LegacyDraw(guardWarningTile,legacyHdrMaterial,ActorWorldPoint(cell)+Vector3.up*.035f,cell,.92f,-8,flat:true);
            if(marker!=null)marker.color=color;
            return marker;
        }

        void PresentGuardSlashWarning(FormationBattleModel.FormationGuardEffect slash,double now)
        {
            var tint=new Color(1f,.26f,.10f,Mathf.Lerp(.17f,.36f,Mathf.Clamp01(1-(float)(slash.ImpactAt-now)/3f)));
            for(int x=Mathf.CeilToInt(FormationBattleModel.BattleAreaStart);x<FormationBattleModel.Width;x++)
            for(int y=0;y<FormationBattleModel.Height;y++)
            {
                var cell=new Vector2(x,y);
                var offset=cell-slash.Origin;
                float along=Vector2.Dot(offset,slash.Direction);
                if(along<0||along>slash.Length||(offset-slash.Direction*along).sqrMagnitude>slash.Radius*slash.Radius)continue;
                DrawGuardGroundCell(cell,tint);
            }
        }

        void PresentGuardSnowField(FormationBattleModel.FormationGuardEffect snow)
        {
            int firstX=Mathf.Max(Mathf.CeilToInt(FormationBattleModel.BattleAreaStart),Mathf.CeilToInt(snow.Origin.x-snow.Radius));
            int lastX=Mathf.Min(FormationBattleModel.Width-1,Mathf.FloorToInt(snow.Origin.x+snow.Radius));
            int firstY=Mathf.Max(0,Mathf.CeilToInt(snow.Origin.y-snow.Radius));
            int lastY=Mathf.Min(FormationBattleModel.Height-1,Mathf.FloorToInt(snow.Origin.y+snow.Radius));
            for(int x=firstX;x<=lastX;x++)
            for(int y=firstY;y<=lastY;y++)
            {
                var cell=new Vector2(x,y);
                if((cell-snow.Origin).sqrMagnitude<=snow.Radius*snow.Radius)
                    DrawGuardGroundCell(cell,new Color(.58f,.80f,1f,.24f));
            }
        }

        void PresentFormationGuardChess(Vector2 cell,double age,bool hitting,bool combat)
        {
            if(!art.TryGetValue("D28Chess",out var chess)||!FormationArtAvailable("D28Chess"))return;
            var clip=chess.Clips[hitting?"attack":"idle"];
            int frame=(int)(age*clip.Fps);
            var position=ActorWorldPoint(cell);
            LegacyDraw(clip.Frames[hitting?Mathf.Clamp(frame,0,clip.Count-1):frame%clip.Count],
                actorMaterial,position,cell,1,6);
            if(hitting)
                LegacyOriginal(FormationGuardFxKey("D28",GuardChessTriggered),age,cell,12,flat:true);
            else
            {
                LegacyOriginal(FormationGuardFxKey("D28",GuardChessArmed),age,cell,12,true,true);
                if(combat)
                    LegacyOriginal(FormationGuardFxKey("D28",GuardChessPlaced),age,cell,13,flat:true);
            }
        }

        // Only source sequences used by the actual owned role enter the bounded
        // streaming preload. Existing Resources packages remain Resources-owned.
        internal IEnumerable<ArtSequence> FormationLegacySequences(string artId)
        {
            foreach(var sequence in FormationSynergyEffectSequences())yield return sequence;
            if(artId=="B15")yield return BossRosterFxArt.Get(KingdomRangeArt).Sequence;
            if(artId=="WISADEL_APRIL")
                foreach(var sequence in WisadelAprilFxArt.Sequences(Campaign?.WisadelSkill??3))yield return sequence;
            string card=FormationUnitCatalog.Find(artId)?.CardId??("CARD_"+artId);
            var keys=MonsterOriginalVfx.RequiredKeysForCards(new[]{card});
            foreach(var sequence in OriginalCombatVfxArt.SequencesFor(keys))yield return sequence;
            if(artId=="M20")yield return OriginalCombatVfxArt.Get("common/common_ignite_defense").Sequence;
            if(artId=="M11")
                foreach(string key in new[]{"common/common_gongjian_trail_01","common/common_hit_01"})yield return OriginalCombatVfxArt.Get(key).Sequence;
            if(artId=="M32")yield return OriginalCombatVfxArt.Get("common/common_ignite_support").Sequence;
            if(artId=="M38")
                foreach(string key in new[]{"common/common_magic_trail_01_m","common/common_magic_hit_01_m"})yield return OriginalCombatVfxArt.Get(key).Sequence;
            if(artId=="TALULAH_TRIAL")
                foreach(string key in new[]{"attack-B-caster","attack-B-hit","ambient-B-foot","movement-B-foot","ambient-B-head","ambient-B-wing",
                    "first_transition_start","first_transition_end","final_transition_start","final_transition_end","ignite-A-caster","ignite-A-target",
                    "ignite-B-caster","ignite-B-target","explode-A-caster","explode-A-target","explode-B-caster","explode-B-target",
                    "target-burn-loop","original-fire-stream","original-fire-impact"})
                    yield return BossRosterSupplementArt.Talulah(key).Sequence;
        }
        internal bool FormationHasLegacyImpact(string artId,bool skill)
        {
            if(artId=="WISADEL_APRIL")return true;
            if(artId=="TALULAH_TRIAL"||artId=="M25"||artId=="M28"||artId=="M32"||artId=="M20")return true;
            switch(artId)
            {
                case "M11":case "M12":case "M14":case "M16":case "M26":case "M42":case "M30":case "M31":case "M33":case "M34":case "M36":case "M38":return true;
                default:return false;
            }
        }
        float LegacyScale(FormationActor actor)
        {var source=FormationUnitArt.For(actor.ArtId);return ActorPresentationHeight(actor)/Mathf.Max(.5f,source?.Height??1.4f);}
        static int LegacyOrder(Vector2 cell,int offset)=>5000-Mathf.RoundToInt(cell.y*50)+offset;
        Vector3 LegacyOwnerPoint(FormationActor actor,Vector2 snapshot)
        {return ActorWorldPoint(snapshot)+(FormationDisplayPoint(actor)-ActorWorldPoint(actor.Position));}
        void EnsureLegacyMaterials()
        {
            if(legacyHdrMaterial==null)
            {
                var shader=Resources.Load<Shader>("TalulahNative");
                if(shader==null)throw new InvalidOperationException("Missing existing native HDR shader.");
                legacyHdrMaterial=new Material(shader){name="Formation / restored native HDR effects"};
            }
        }
        SpriteRenderer LegacyDraw(Sprite sprite,Material material,Vector3 position,Vector2 ground,float scale,int order,bool flip=false,float hdr=1,float frame=-1,bool flat=false,float roll=0,bool gammaOutput=false)
        {
            if(sprite==null||battleCamera==null||!FormationPointVisibleInView(ground))return null;
            if(legacyFxUsed==legacyFxViews.Count)
            {
                var renderer=new GameObject("Restored original formation effect",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(actorRoot,false);legacyFxViews.Add(new LegacyFxView{Renderer=renderer});
            }
            var view=legacyFxViews[legacyFxUsed++];var r=view.Renderer;
            r.name="Formation original / "+sprite.name;r.enabled=true;r.sprite=sprite;r.sharedMaterial=material;r.flipX=flip;r.color=Color.white;
            var rotation=battleCamera.transform.rotation*Quaternion.Euler(0,0,roll);var stretch=Vector3.one*scale;
            if(flat)
            {
                var forward=Vector3.ProjectOnPlane(battleCamera.transform.up,Vector3.up).normalized;
                float projection=Mathf.Max(.001f,Mathf.Abs(Vector3.Dot(battleCamera.transform.up,forward)));
                rotation=Quaternion.LookRotation(-Vector3.up,forward)*Quaternion.Euler(0,0,roll);stretch.y/=projection;
            }
            r.transform.SetPositionAndRotation(position,rotation);r.transform.localScale=stretch;r.sortingOrder=LegacyOrder(ground,order);
            view.Properties.Clear();view.Properties.SetFloat("_HdrRange",hdr);
            if(frame>=0)view.Properties.SetFloat("_Frame",frame);
            if(gammaOutput)view.Properties.SetFloat("_GammaOutput",1);
            r.SetPropertyBlock(view.Properties);
            return r;
        }
        SpriteRenderer LegacyOriginal(string raw,double age,Vector2 cell,int order=24,bool loop=false,bool flat=false,float scale=1,Vector3? explicitPosition=null,Vector3? direction=null,bool gammaOutput=false,bool flip=false)
        {
            if(age<0||!FormationPointVisibleInView(cell))return null;
            string key=raw.Contains("/")?raw:"monster/"+raw;
            if(!OriginalCombatVfxArt.TryGet(key,out var clip)||!clip.Sequence.Ready)return null;
            if(loop&&!clip.Loop)return null; // Never invent looping from a one-shot capture.
            float roll=0;
            if(direction.HasValue&&clip.Directional)
            {
                var vector=direction.Value;float x=Vector3.Dot(vector,battleCamera.transform.right),y=Vector3.Dot(vector,battleCamera.transform.up);
                if(x*x+y*y>1e-8)roll=Mathf.Atan2(y,x)*Mathf.Rad2Deg-clip.ForwardDegrees;
            }
            return LegacyDraw(clip.At(age,loop),legacyHdrMaterial,explicitPosition??(ActorWorldPoint(cell)+Vector3.up*.025f),cell,scale,order,flip:flip,hdr:clip.HdrRange,flat:flat,roll:roll,gammaOutput:gammaOutput);
        }
        void LegacyOriginalBeam(string key,double age,Vector2 from,Vector2 to,float lift=1.05f,bool loop=true,Vector3? explicitStart=null,Vector3? explicitEnd=null)
        {
            if(age<0||!FormationPointVisibleInView(from)||!OriginalCombatVfxArt.TryGet("monster/"+key,out var clip)||!clip.Sequence.Ready||loop&&!clip.Loop)return;
            var start=explicitStart??(ActorWorldPoint(from)+battleCamera.transform.up*lift);var end=explicitEnd??(ActorWorldPoint(to)+battleCamera.transform.up*.8f);
            Vector3 delta=end-start;float x=Vector3.Dot(delta,battleCamera.transform.right),y=Vector3.Dot(delta,battleCamera.transform.up);
            var sprite=clip.At(age,loop);if(sprite==null)return;
            float angle=Mathf.Atan2(y,x)*Mathf.Rad2Deg-clip.ForwardDegrees;
            var renderer=LegacyDraw(sprite,legacyHdrMaterial,start,from,1,28,hdr:clip.HdrRange,roll:angle);
            if(renderer!=null)renderer.transform.localScale=new Vector3(new Vector2(x,y).magnitude/3.005432f,1,1);
        }

        void PresentFormationFlameSpray(FormationPresentationAction action,FormationActor actor)
        {
            if(action.Cancelled||!actor.Alive||Model.Clock<action.ContactAt||Model.Clock>=action.EndAt||!FormationPointVisibleInView(actor.Position))return;
            var art=FormationUnitArt.For(actor.ArtId);
            double sprayAt=Math.Max(action.ContactAt,action.At+(art?.Duration("extra_skill_front_start")??0)/action.Rate);
            double sprayUntil=action.EndAt-(art?.Duration("extra_skill_front_end")??0)/action.Rate;
            double elapsed=Model.Clock-sprayAt;
            if(elapsed<0||Model.Clock>=sprayUntil)return;

            var camera=battleCamera.transform;
            Vector3 from=FormationFeedbackContactPoint(actor,actor.Position,.48f);
            Vector3 to=FormationFeedbackContactPoint(FormationFeedbackActor(action.TargetId),action.To,.52f);
            float toward=Vector3.Dot(to-from,camera.right);
            float muzzleOffset=Mathf.Min(ActorPresentationHeight(actor)*.43f,Mathf.Abs(toward)*.68f);
            from+=camera.right*(Mathf.Sign(toward)*muzzleOffset);
            Vector3 delta=to-from;
            var screenDelta=new Vector2(Vector3.Dot(delta,camera.right),Vector3.Dot(delta,camera.up));
            float span=screenDelta.magnitude;
            if(span<.05f)return;

            double age=elapsed*action.Rate;
            LegacyOriginalBeam("enemy_duspfr_2_attack_01_front_start2",age,actor.Position,action.To,explicitStart:from,explicitEnd:to);

            // The packed native sequence has a short muzzle bloom. The existing CC0
            // flame frames extend it into a readable, continuous jet along the cast aim.
            if(formationFlameSprayMaterial==null)
            {
                var card=Array.Find(formationCards,candidate=>candidate!=null&&candidate.CardId=="CARD_TALULAH_TRIAL");
                var sourceFrames=card?.ArticulatedArt?.FireFrames;
                var shader=Shader.Find("Sprites/Default");
                var coreShader=Resources.Load<Shader>("TalulahAttackAdditive");
                if(shader==null||coreShader==null||sourceFrames==null||sourceFrames.Count==0)return;
                formationFlameSprayMaterial=new Material(shader){name="Formation / Flamerazer fire jet"};
                formationFlameCoreMaterial=new Material(coreShader){name="Formation / Flamerazer luminous core"};
                formationFlameSprayFrames=new Sprite[sourceFrames.Count];
                for(int i=0;i<sourceFrames.Count;i++)
                {
                    var source=sourceFrames[i];
                    if(source==null)continue;
                    // The lower third holds the bright kernel. The longer crop read
                    // as dark smoke in the actual battle camera.
                    Rect painted=source.rect;
                    painted.height=Mathf.Ceil(painted.height*.32f);
                    var cropped=Sprite.Create(source.texture,painted,new Vector2(.5f,.018f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
                    cropped.name="Flamerazer painted flame / "+i;
                    cropped.hideFlags=HideFlags.DontSave;
                    formationFlameSprayFrames[i]=cropped;
                }
            }

            float reach=Mathf.SmoothStep(.2f,1,(float)(elapsed/.10));
            float opacity=Mathf.Clamp01((float)((sprayUntil-Model.Clock)/.13));
            float heading=Mathf.Atan2(screenDelta.y,screenDelta.x)*Mathf.Rad2Deg;
            float ripple=Mathf.Sin((float)age*18f);
            int frameIndex=(int)(age*28),count=formationFlameSprayFrames.Length;
            Vector3 depthBias=-camera.forward*.16f;
            var contactCell=Vector2.Lerp(actor.Position,action.To,.45f);
            if(reach>.43f)
            {
                Vector3 tailStart=from+delta*.34f+depthBias+camera.up*(ripple*.055f);
                DrawFormationFlameTongue(formationFlameSprayFrames[(frameIndex+11)%count],tailStart,contactCell,heading,
                    span*(reach-.30f),1.25f,opacity*.87f,"Formation Flamerazer / outer flame");
            }
            DrawFormationFlameTongue(formationFlameSprayFrames[frameIndex%count],from+depthBias,contactCell,heading,
                span*Mathf.Min(reach,.89f),1.04f+ripple*.08f,opacity*.96f,"Formation Flamerazer / fire jet");
            DrawFormationFlameTongue(formationFlameSprayFrames[(frameIndex+6)%count],from+delta*.14f+depthBias,contactCell,heading,
                span*(reach-.13f),.72f,opacity,"Formation Flamerazer / luminous core",formationFlameCoreMaterial,28);
        }

        void DrawFormationFlameTongue(Sprite frame,Vector3 start,Vector2 cell,float heading,float length,float width,float opacity,string name,
            Material material=null,int order=26)
        {
            var jet=LegacyDraw(frame,material??formationFlameSprayMaterial,start,cell,1,order,roll:heading-90);
            if(jet==null)return;
            jet.name=name;
            jet.color=new Color(1f,.96f,.89f,opacity);
            Vector2 size=frame.bounds.size;
            jet.transform.localScale=new Vector3(width/Mathf.Max(.01f,size.x),length/Mathf.Max(.01f,size.y),1);
        }
        void LegacyPremult(Sprite sprite,Vector2 cell,float scale,int order=24,bool flip=false,Vector3? point=null)
        {
            if(legacyPremultMaterial==null)
            {
                var source=Resources.Load<Material>("MudrockVfx/OriginalEffect");
                if(source==null)throw new InvalidOperationException("Missing existing original premultiplied material.");
                legacyPremultMaterial=new Material(source){name="Formation / restored original premultiplied FX"};
            }
            LegacyDraw(sprite,legacyPremultMaterial,point??ActorWorldPoint(cell),cell,scale,order,flip);
        }
        void LegacyMudrock(string key,double age,Vector2 cell,float scale,int order=24,bool loop=false,bool flip=false,Vector3? point=null)
        {var sprite=MudrockVfxArt.Load(key)?.At(age,loop);LegacyPremult(sprite,cell,scale,order,flip,point);}
        void LegacyMudrockBoth(string key,double age,Vector2 cell,float scale,bool loop=false,bool flip=false,Vector3? ownerPoint=null)
        {LegacyMudrock(key+"_ground",age,cell,scale,-4,loop,flip);LegacyMudrock(key+"_over",age,cell,scale,12,loop,flip,ownerPoint);}

        LegacyTalulahFrames LegacyTalulahCore(string key)
        {
            if(legacyTalulahFrames.TryGetValue(key,out var cached))return cached;
            if(legacyTalulahManifest==null)
            {
                var text=Resources.Load<TextAsset>("TalulahNative/manifest");
                if(text==null)throw new InvalidDataException("Missing approved Talulah native core.");
                legacyTalulahManifest=JsonUtility.FromJson<LegacyTalulahManifest>(text.text);
            }
            var d=legacyTalulahManifest.clips.First(c=>c.key==key);var frames=new List<Sprite>();
            foreach(var page in d.pages)
            {
                var texture=Resources.Load<Texture2D>("TalulahNative/"+Path.GetFileNameWithoutExtension(page.file));
                if(texture==null)throw new InvalidDataException("Missing Talulah native page "+page.file);
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(texture,new Rect(i%d.columns*d.width,texture.height-(i/d.columns+1)*d.height,d.width,d.height),new Vector2(d.pivotX,d.pivotY),d.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="TalulahNative/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=d.count)throw new InvalidDataException("Incomplete Talulah native core "+key);
            cached=new LegacyTalulahFrames{Data=d,Frames=frames.ToArray()};legacyTalulahFrames[key]=cached;return cached;
        }
        void LegacyTalulah(string key,double age,Vector2 cell,FormationActor actor,int order=12,bool loop=false,Vector3? position=null,bool? facingLeft=null)
        {
            if(age<0||!FormationPointVisibleInView(cell))return;Sprite frame;float hdr;
            if(key=="attack-A-caster"||key=="attack-A-hit"||key=="ambient-A-foot"||key=="movement-A-foot"||key=="firecast-A-hand")
            {
                var clip=LegacyTalulahCore(key);if(!loop&&age>=clip.Data.duration)return;
                if(loop)age%=clip.Data.duration;frame=clip.Frames[Mathf.Clamp((int)(age*legacyTalulahManifest.fps),0,clip.Frames.Length-1)];hdr=clip.Data.hdrRange;
            }
            else
            {
                var clip=BossRosterSupplementArt.Talulah(key);if(!clip.Sequence.Ready)return;
                // These five existing phase ambient samples are source loops although the
                // older supplemental adapter listed only the later orb/burn loop keys.
                bool sourceLoop=loop&&(clip.Loop||key.StartsWith("ambient-",StringComparison.Ordinal)||key.StartsWith("movement-",StringComparison.Ordinal));
                if(sourceLoop)age%=clip.Duration;
                frame=clip.At(age,false);hdr=clip.HdrRange;
            }
            LegacyDraw(frame,legacyHdrMaterial,position??(ActorWorldPoint(cell)+battleCamera.transform.up*.12f),cell,LegacyScale(actor),order,facingLeft??actor.Facing.x<0,hdr);
        }
        void LegacyPatriot(string suffix,double age,Vector2 cell,FormationActor actor,int order=12,bool loop=false,Vector3? point=null)
        {
            if(age<0||!FormationPointVisibleInView(cell))return;
            if(legacyPatriotCatalog==null)
            {
                var text=Resources.Load<TextAsset>("PatriotVfx/catalog");
                if(text==null)throw new InvalidDataException("Missing approved Patriot VFX catalog.");
                legacyPatriotCatalog=JsonUtility.FromJson<LegacyPatriotCatalog>(text.text);
            }
            var clip=legacyPatriotCatalog.clips.FirstOrDefault(c=>c.name=="patrt_"+suffix);if(clip==null)return;
            double duration=clip.frames/(double)legacyPatriotCatalog.fps;if(loop)age%=duration;
            int index=(int)(age*legacyPatriotCatalog.fps);if(index>=clip.frames)return;
            string pageName=clip.pages[index/16];
            if(!legacyPatriotPages.TryGetValue(pageName,out var page))
            {
                var texture=Resources.Load<Texture2D>("PatriotVfx/"+pageName);if(texture==null)throw new InvalidDataException("Missing Patriot page "+pageName);
                var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(clip.anchorX,clip.anchorY),texture.width,0,SpriteMeshType.FullRect);
                sprite.name="Patriot/"+suffix+"/"+pageName;sprite.hideFlags=HideFlags.DontSave;
                page=new LegacyPatriotPage{Texture=texture,Sprite=sprite};legacyPatriotPages[pageName]=page;
            }
            page.LastUse=Time.frameCount;
            if(legacyPatriotMaterial==null)legacyPatriotMaterial=new Material(Resources.Load<Shader>("PatriotVfx/PatriotPortable"));
            float scale=LegacyScale(actor);var position=point??LegacyOwnerPoint(actor,cell);
            float lift=Mathf.Max(4,clip.worldSize*clip.anchorY*Mathf.Max(0,battleCamera.transform.up.y)/Mathf.Max(.1f,-battleCamera.transform.forward.y)+.1f);
            LegacyDraw(page.Sprite,legacyPatriotMaterial,position-battleCamera.transform.forward*(lift*scale),cell,clip.worldSize*scale,order,actor.Facing.x<0,frame:index%16);
        }

        void PresentLegacyAction(FormationPresentationAction action)
        {
            var actor=Model.Units.FirstOrDefault(u=>u.Id==action.ActorId);if(actor==null||!actor.Enabled)return;
            if(actor.ArtId=="TALULAH_TRIAL"&&action.Cancelled)return;
            double age=(Model.Clock-action.At)*action.Rate;if(age<0)return;
            string id=actor.ArtId;float scale=LegacyScale(actor);bool flip=actor.Facing.x<0;
            var origin=LegacyOwnerPoint(actor,action.From);string key=null;
            if(id=="WISADEL_APRIL"){PresentOriginalWisadelAction(action,actor);return;}
            if(action.ProjectileOnly)return;
            if(id=="TALULAH_TRIAL")
            {
                bool reborn=action.NativePhase>0;
                bool facingLeft=action.To.x<action.From.x;
                if(action.Skill)
                {
                    bool ignite=Campaign.TalulahCost<=3;
                    string caster=ignite?(reborn?"ignite-B-caster":"ignite-A-caster"):
                        (reborn?"explode-B-caster":"explode-A-caster");
                    LegacyTalulah(caster,age,action.From,actor,position:origin+battleCamera.transform.up*.12f,facingLeft:facingLeft);
                    if(!ignite&&action.TargetId>=0)
                    {
                        // DragonFireExplode runs both original emission lists on ON_CAST_START.
                        var target=FormationFeedbackActor(action.TargetId);
                        var point=FormationFeedbackContactPoint(target,action.To,.48f);
                        LegacyTalulah(reborn?"explode-B-target":"explode-A-target",age,action.To,actor,35,position:point,facingLeft:facingLeft);
                    }
                }
                else LegacyTalulah(reborn?"attack-B-caster":"attack-A-caster",age,action.From,actor,position:origin+battleCamera.transform.up*.12f,facingLeft:facingLeft);
                return;
            }
            if(id=="M20")
            {
                if(!action.Skill)LegacyPatriot(actor.NativePhase>0?"attack_02_start":"attack_01_start",age,action.From,actor,
                    point:origin+battleCamera.transform.up*(actor.NativePhase>0?1.1f:.92f)*scale);
                else LegacyOriginal("common/common_ignite_defense",age,action.From,14,scale:scale,explicitPosition:origin);
                return;
            }
            if(id=="M25")
            {
                var hand=MephistoVfxArt.Hand("Attack",(int)(age*30));
                var position=origin+battleCamera.transform.right*(flip?-hand.x:hand.x)*scale+battleCamera.transform.up*hand.y*scale;
                LegacyPremult(MephistoVfxArt.Load("attack_01_start").At(age),action.From,.779296875f*scale,12,flip,position);return;
            }
            if(id=="M32")
            {
                // The current support skill grants shields; do not fabricate the old
                // damaging gaze or a pillar collapse from this cast.
                if(action.Skill)
                {LegacyOriginal("common/common_ignite_support",age,action.From,14,scale:scale,explicitPosition:origin);return;}
                string phase=actor.NativePhase>0?"C2":"C1";
                var hand=MandragoraVfxArt.Mount(phase+"_Attack"+(actor.NativePhase>0?"_1":""),age,false);
                var position=origin+battleCamera.transform.right*(flip?-hand.x:hand.x)*scale+battleCamera.transform.up*hand.y*scale;
                string prefix=actor.NativePhase>0?"c2_attack_01":"c1_attack";
                LegacyPremult(MandragoraVfxArt.Load(prefix+"_start_01").At(age),action.From,.95f*scale,12,flip,position);
                LegacyPremult(MandragoraVfxArt.Load(prefix+"_start_02").At(age),action.From,.95f*scale,13,flip,position);return;
            }
            if(id=="M28")
            {LegacyMudrock("attackStart_over",age,action.From,scale,12,false,flip,origin);return;}
            switch(id)
            {
                case "M12":key=action.Skill?"snmage_skill_01_start":"snmage_attack_01_start";break;
                case "M14":key=action.Skill?"firstar_skill_01_buff":actor.NativePhase>0?"firstar_attack_02_start":"firstar_attack_01_start";break;
                case "M26":case "M42":key="enemy_mdsnpr_attack_01_start";break;
                case "M30":key="enemy_duskls_attack_start";break;
                case "M36":key=action.Skill?"enemy_dsmant_skill_01_start":"enemy_dsmant_attack_01_start";break;
                case "M34":if(action.Skill)key="enemy_dslntc_skill_begin_buff";break;
            }
            if(key!=null)LegacyOriginal(key,age,action.From,12,flat:true,scale:scale);
            if(id=="M36"&&action.Skill&&!action.Cancelled&&actor.Alive&&
                Math.Abs(actor.LastSkillAt-action.At)<.0001&&Math.Abs(actor.LastActionAt-action.At)<.0001&&Model.Clock<actor.ActionUntil)
            {
                // The original two caster layers and target layer already exist.
                // Read the current skill window; do not restore the retired bubble
                // ability's five-second invulnerability or damage scheduling.
                LegacyOriginal("enemy_dsmant_skill_01_buff",age,actor.Position,26,true,true,scale);
                LegacyOriginal("enemy_dsmant_skill_01_buff_02",age,actor.Position,27,true,true,scale);
                if(Model.Clock>=action.ContactAt)
                    LegacyOriginal("enemy_dsmant_skill_01_buff_03",(Model.Clock-action.ContactAt)*action.Rate,action.To,28,true,true,scale);
            }
            if(action.Skill&&id=="M31")PresentFormationFlameSpray(action,actor);
            var firstApplied=action.Skill&&id=="M38"?Model.Hits.Where(h=>h.FromId==actor.Id&&h.Skill&&h.At>=action.At).OrderBy(h=>h.At).FirstOrDefault():null;
            if(action.Skill&&id=="M38"&&firstApplied!=null&&Model.Clock<firstApplied.At+3)
            {
                double appliedAge=(Model.Clock-firstApplied.At)*LegacyVisualRate;
                LegacyOriginal("dmage_skill_01_start",appliedAge,actor.Position,22,scale:scale,explicitPosition:FormationDisplayPoint(actor));
                LegacyOriginalBeam("dmage_skill_01_line",appliedAge,actor.Position,firstApplied.To,1.1f*scale,
                    explicitStart:FormationDisplayPoint(actor)+battleCamera.transform.up*1.1f*scale);
                LegacyOriginal("dmage_skill_01_buff",appliedAge,firstApplied.To,32,true,true,scale);
            }
            // Recorded launch/contact fields own projectile flight. Never draw a
            // diagnostic line, or keep a projectile after its actual contact.
            string trail=null;
            switch(id)
            {
                case "M11":if(!action.Skill)trail="common/common_gongjian_trail_01";break;
                case "M12":trail="snmage_attack_01_trail";break;case "M14":if(!action.Skill)trail="firstar_attack_01_trail";break;
                case "M16":trail="sotisp_attack_01_trail";break;case "M26":case "M42":trail="enemy_mdsnpr_attack_01_trail";break;
                case "M36":if(!action.Skill)trail="enemy_dsmant_attack_01_trail";break;
                case "M38":if(!action.Skill)trail="common/common_magic_trail_01_m";break;
            }
            if(!action.Cancelled&&trail!=null&&Model.Clock>=action.LaunchedAt&&action.ContactAt>action.LaunchedAt&&Model.Clock<action.ContactAt)
            {
                var arrowTarget = id == "M11" ? Model.Defenders.FirstOrDefault(d => d.Id == action.TargetId) : null;
                if(!legacyFlightOrigins.TryGetValue(action.Sequence,out var start))
                {
                    start = id == "M11" ? FormationFeedbackContactPoint(actor, action.From, .55f) : origin + battleCamera.transform.up * .75f * scale;
                    legacyFlightOrigins[action.Sequence] = start;
                }
                var end = id == "M11" ? FormationFeedbackContactPoint(arrowTarget, action.To, .52f) : ActorWorldPoint(action.To) + battleCamera.transform.up * .8f;
                float progress=Mathf.Clamp01((float)((Model.Clock-action.LaunchedAt)/(action.ContactAt-action.LaunchedAt)));
                var cell=Vector2.Lerp(action.From,action.To,progress);
                bool loop=OriginalCombatVfxArt.TryGet(trail.Contains("/")?trail:"monster/"+trail,out var projectile)&&projectile.Loop;
                LegacyOriginal(trail,(Model.Clock-action.LaunchedAt)*action.Rate,cell,20,loop,scale:scale,
                    explicitPosition:Vector3.Lerp(start,end,progress),direction:end-start);
            }
            else if(!action.Cancelled&&trail!=null&&action.ContactAt<=action.LaunchedAt&&Model.Clock>=action.ContactAt&&Model.Clock<action.ContactAt+.18)
            {
                // Current instant-hit v0.7 actions have no independent flight interval.
                // Show the native projectile's contact remnant at the real endpoint;
                // do not fabricate a flight during the actor's windup.
                LegacyOriginal(trail,(Model.Clock-action.ContactAt)*action.Rate,action.To,20,scale:scale,
                    explicitPosition:ActorWorldPoint(action.To)+battleCamera.transform.up*.8f,direction:ActorWorldPoint(action.To)-origin);
            }
        }

        void PresentLegacyHit(FormationHit hit)
        {
            var actor=Model.Units.FirstOrDefault(u=>u.Id==hit.FromId);if(actor==null||!FormationHasLegacyImpact(actor.ArtId,hit.Skill))return;
            double age=(Model.Clock-hit.At)*LegacyVisualRate;if(age<0)return;string id=actor.ArtId;float scale=LegacyScale(actor);
            if(id=="WISADEL_APRIL"&&!hit.Heal)
            {
                if(UsesApprovedWisadelOrdinaryImpact(hit))
                {
                    if(!hit.Secondary)PresentLegacyWisadelHit(hit,actor,Model.Clock-hit.At);
                }
                else if(WisadelAprilFxArt.Available)PresentOriginalWisadelHit(hit,actor,age);
                else PresentLegacyWisadelHit(hit,actor,age);
                return;
            }
            if(id=="TALULAH_TRIAL")
            {
                PresentFormationTalulahHit(hit,actor);
                return;
            }
            if(id=="M25"&&hit.Heal)
            {LegacyPremult(MephistoVfxArt.Load("attack_01_hit").At(age),hit.To,.779296875f*scale,24);return;}
            if(id=="M32"&&!hit.Heal)
            {LegacyPremult(MandragoraVfxArt.Load(actor.NativePhase>0?"c2_attack_01_hit":"c1_attack_hit").At(age),hit.To,.8f*scale,50,point:ActorWorldPoint(hit.To)+battleCamera.transform.up*.85f*scale);return;}
            if(id=="M28"&&!hit.Heal)
            {LegacyMudrockBoth(hit.Skill?"attack3":"attack1",age,hit.To,scale,flip:actor.Facing.x<0);return;}
            if(id=="M20"&&!hit.Heal)
            {LegacyPatriot(actor.NativePhase>0?"attack_02_hit":"attack_01_hit",age,hit.To,actor,50,point:ActorWorldPoint(hit.To)+battleCamera.transform.up*.7f*scale);return;}
            if(hit.Heal)return;
            string key=null;
            switch(id)
            {
                case "M11":key="common/common_hit_01";break;
                case "M12":key="snmage_attack_01_hit";break;
                // The real snow zone exclusively owns its area layer. A single
                // AoE must not spawn another full range sprite on every victim.
                case "M14":if(!hit.Skill)key="firstar_attack_01_hit";break;
                case "M16":key="sotisp_attack_01_hit";break;case "M26":case "M42":key="enemy_mdsnpr_attack_01_hit";break;
                case "M30":key="enemy_duskls_attack_hit";break;case "M31":key=Math.Abs(hit.At-actor.LastDeathAt)<.05?"enemy_duspfr_2_die_01_hit":"enemy_duspfr_2_attack_01_hit";break;
                case "M34":key="enemy_dslntc_attack_01_hit";break;case "M36":key=hit.Skill?"enemy_dsmant_skill_01_hit":"enemy_dsmant_attack_01_hit";break;
                case "M38":if(!hit.Skill)key="common/common_magic_hit_01_m";break;
                case "M33":break; // Counter caster pulses come from their real LastReflect timestamp below.
            }
            if(key!=null)
            {
                bool ground=id=="M14"&&hit.Skill||id=="M36"&&hit.Skill||id=="M31"&&Math.Abs(hit.At-actor.LastDeathAt)<.05;
                Vector3? point = ground ? (Vector3?)null : ActorWorldPoint(hit.To) + battleCamera.transform.up * .8f * scale;
                if (id == "M11") point = FormationFeedbackContactPoint(Model.Defenders.FirstOrDefault(d => d.Id == hit.ToId), hit.To, .52f);
                LegacyOriginal(key,age,hit.To,35,flat:ground,scale:scale,explicitPosition:point);
            }
        }

        // The old approved April-skin presentation used these exact 16-point
        // black/yellow geometric bursts plus the six user-GIF expression crops.
        // PrepareImpactFace only selected a crop; PresentShot enabled it after a
        // real empowered impact. Reuse that material here, never invent S1/S2 art.
        void PresentLegacyWisadelHit(FormationHit hit,FormationFighter actor,double age)
        {
            double duration=hit.Skill?.55:.18;
            if(age<0||age>=duration||!FormationPointVisibleInView(hit.To))return;
            if(legacyComicMaterial==null)legacyComicMaterial=new Material(Shader.Find("Sprites/Default")){name="Formation / approved April comic burst"};
            float scale=LegacyScale(actor),progress=(float)(age/duration),alpha=1-progress;
            float radius=(float)((hit.Skill?.28:.05)+age*(hit.Skill?1.3:.8))*scale;
            if(hit.Skill&&Campaign!=null&&Campaign.WisadelSkill==3)radius=ViviCombat.SkillSplashRadius*Mathf.Lerp(.55f,1,progress)*scale;
            var center=ApprovedWisadelComicImpactPoint(hit.To);
            if(legacyComicUsed==legacyComicBursts.Count)
            {
                var burst=new LegacyComicBurst();
                foreach(bool ink in new[]{true,false})
                {
                    var line=new GameObject(ink?"April comic / approved black ink":"April comic / approved yellow burst",typeof(LineRenderer)).GetComponent<LineRenderer>();
                    line.transform.SetParent(actorRoot,false);line.sharedMaterial=legacyComicMaterial;line.useWorldSpace=true;line.loop=true;line.positionCount=16;
                    if(ink)burst.Ink=line;else burst.Light=line;
                }
                legacyComicBursts.Add(burst);
            }
            var view=legacyComicBursts[legacyComicUsed++];int order=LegacyOrder(hit.To,50);
            foreach(bool ink in new[]{true,false})
            {
                var line=ink?view.Ink:view.Light;line.enabled=true;line.sortingOrder=order+(ink?0:1);
                line.widthMultiplier=(hit.Skill?(ink?.13f:.075f):(ink?.06f:.025f))*scale;
                var color=ink?new Color(.035f,.025f,.025f,alpha):new Color(1,.83f,.34f,alpha);line.startColor=line.endColor=color;
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8,r=radius*(i%2==0?1.25f:.64f);
                    line.SetPosition(i,center+(battleCamera.transform.right*Mathf.Cos(angle)+battleCamera.transform.up*Mathf.Sin(angle))*r);
                }
            }
            if(!hit.Skill)return;
            var card=formationCards.FirstOrDefault(c=>c!=null&&c.CardId=="CARD_WISADEL_APRIL");
            var faces=card?.ComicImpactFaces;
            if(faces==null||faces.Count!=6||faces.Any(face=>face==null))throw new InvalidDataException("The approved April skin needs its six existing GIF expression sprites.");
            if(!legacyComicFaceIds.TryGetValue(hit,out int faceIndex))legacyComicFaceIds[hit]=faceIndex=legacyComicFaceSequence++%faces.Count;
            int before=legacyFxUsed;
            LegacyDraw(faces[faceIndex],legacyComicMaterial,center,hit.To,(.84f+(float)age*.56f)*scale,52);
            if(legacyFxUsed>before)legacyFxViews[legacyFxUsed-1].Renderer.color=new Color(1,1,1,alpha);
        }

        void PresentLegacyOwner(FormationFighter actor)
        {
            if(actor.ArtId=="WISADEL_APRIL"){PresentOriginalWisadelOwner(actor);return;}
            double now=Model.Clock;float scale=LegacyScale(actor);string id=actor.ArtId;bool returning=actor.NativePhaseReturningUntil>now;
            double ambientAge=(Model.Phase==FormationPhase.Preparation?Time.unscaledTimeAsDouble:now)*LegacyVisualRate;
            double phaseAge=(now-actor.NativePhaseChangedAt)*LegacyVisualRate;
            if(id=="TALULAH_TRIAL")
            {
                if(actor.Alive)
                {
                    LegacyTalulah(actor.NativePhase>0?"ambient-B-foot":"ambient-A-foot",ambientAge,actor.Position,actor,-4,true);
                    if(actor.IsMoving)LegacyTalulah(actor.NativePhase>0?"movement-B-foot":"movement-A-foot",ambientAge,actor.Position,actor,-3,true);
                    if(actor.NativePhase>0){LegacyTalulah("ambient-B-head",ambientAge,actor.Position,actor,7,true,FormationDisplayPoint(actor));LegacyTalulah("ambient-B-wing",ambientAge,actor.Position,actor,-5,true,FormationDisplayPoint(actor));}
                }
                if(returning)LegacyTalulah("first_transition_start",phaseAge,actor.Position,actor,6,position:FormationDisplayPoint(actor));
                else if(actor.NativePhase>0)LegacyTalulah("first_transition_end",phaseAge,actor.Position,actor,6,position:FormationDisplayPoint(actor));
            }
            if(id=="M14"&&(returning||actor.NativePhase>0))
                foreach(string key in new[]{"firstar_buff_01_start","firstar_buff_02_start","firstar_buff_03_start"})LegacyOriginal(key,phaseAge,actor.Position,24,flat:true,scale:scale);
            if(id=="M20")
            {
                if(returning){LegacyPatriot("buff_02_start",phaseAge,actor.Position,actor);LegacyPatriot("buff_02_start_02",phaseAge-.2,actor.Position,actor);}
                else if(actor.NativePhase>0)
                    foreach(string key in new[]{"buff_04_start","buff_04_start_02","buff_04_start_03","buff_04_start_04"})LegacyPatriot(key,phaseAge,actor.Position,actor);
                if(!actor.Alive&&!returning)LegacyPatriot("die_01_start",(now-actor.LastDeathAt)*LegacyVisualRate,actor.Position,actor);
            }
            if(id=="M32"&&returning)
            {
                foreach(string key in new[]{"reborn_start_01","reborn_loop_01","reborn_loop_02"})LegacyPremult(MandragoraVfxArt.Load(key).At(phaseAge,key.Contains("loop")),actor.Position,.95f*scale,12);
                LegacyPremult(MandragoraVfxArt.Load("reborn_end_01").At(phaseAge-4.5),actor.Position,.95f*scale,12);
                LegacyPremult(MandragoraVfxArt.Load("reborn_end_02").At(phaseAge-4.5),actor.Position,.95f*scale,13);
            }
            if(id=="M30"&&returning)
            {
                double reformAge=FormationUnitArt.For(id).Duration("extra_revive")-(actor.NativePhaseReturningUntil-now)*LegacyVisualRate;
                LegacyOriginal("enemy_duskls_revive_2",phaseAge,actor.Position,22,flat:true,scale:scale);
                if(reformAge<0)LegacyOriginal("enemy_duskls_idle_2",phaseAge,actor.Position,20,true,true,scale);
                else
                {
                    LegacyOriginal("enemy_duskls_revive_3",reformAge,actor.Position,23,flat:true,scale:scale);
                    LegacyOriginal("enemy_duskls_revive_4",reformAge,actor.Position,24,flat:true,scale:scale);
                }
            }
            if(id=="M30"&&actor.Alive&&!returning&&Model.PresentationStack(actor,"fireautos")>0)
                LegacyOriginal("enemy_duskls_weapon_buff",(now-actor.LastSkillAt)*LegacyVisualRate,actor.Position,18,true,true,scale);
            if(id=="M31"&&!actor.Alive&&!returning)LegacyOriginal("enemy_duspfr_2_die_01",(now-actor.LastDeathAt)*LegacyVisualRate,actor.Position,30,flat:true,scale:scale);
            if(id=="M33")
            {
                double reflect=(now-Model.LegacyReflectAt(actor))*LegacyVisualRate;
                LegacyOriginal("enemy_dsurch_01_start",reflect,actor.Position,24,flat:true,scale:scale);
                LegacyOriginal("enemy_dsurch_01_start_02",reflect,actor.Position,25,flat:true,scale:scale);
            }
            if(id=="M34"&&actor.Alive&&Model.LegacyAwake(actor))LegacyOriginal("enemy_dslntc_buff_01",now*LegacyVisualRate,actor.Position,22,true,true,scale);
            if(id=="M28")
            {
                float previous=legacyPreviousShield.TryGetValue(actor.Id,out var old)?old:actor.Shield;
                if(previous>0&&actor.Shield<=0)legacyShieldBroken[actor.Id]=now;
                if(actor.Shield>previous+.001f)legacyShieldRefreshed[actor.Id]=now;
                legacyPreviousShield[actor.Id]=actor.Shield;
                if(actor.Alive&&actor.Shield>0)LegacyMudrockBoth("shield1",now*LegacyVisualRate,actor.Position,scale,true,actor.Facing.x<0,FormationDisplayPoint(actor));
                if(legacyShieldBroken.TryGetValue(actor.Id,out var broken))LegacyMudrockBoth("shieldBreak",(now-broken)*LegacyVisualRate,actor.Position,scale,flip:actor.Facing.x<0,ownerPoint:FormationDisplayPoint(actor));
                if(legacyShieldRefreshed.TryGetValue(actor.Id,out var refreshed))LegacyMudrock("shieldRefresh_over",(now-refreshed)*LegacyVisualRate,actor.Position,scale,12,false,actor.Facing.x<0,FormationDisplayPoint(actor));
            }
            double healUntil=Model.LegacyBuffUntil(actor,"heal_received");
            if(actor.Alive&&healUntil>0)
            {
                bool live=healUntil>now;double age=(now-(live?healUntil-4:healUntil))*LegacyVisualRate;
                LegacyPremult(MephistoVfxArt.Load(live?"buff_01_start":"buff_01_end").At(age,live),actor.Position,.779296875f*scale,20,point:FormationDisplayPoint(actor)+battleCamera.transform.up*.8f*scale);
            }
            if(actor.Alive&&Model.LegacyMandragoraShield(actor))
            {
                LegacyPremult(MandragoraVfxArt.Load("c1_skill_02_buff_01").At(now*LegacyVisualRate,true),actor.Position,.95f*scale,-4,point:FormationDisplayPoint(actor));
            }
        }

        internal void PresentFormationLegacyEffects()
        {
            if(Model==null||actorRoot==null||battleCamera==null)return;
            if(legacyObservedModel!=Model||Model.Clock<legacyLastClock)
            {legacyObservedModel=Model;legacyPreviousShield.Clear();legacyShieldBroken.Clear();legacyShieldRefreshed.Clear();legacyFlightOrigins.Clear();legacyComicFaceIds.Clear();legacyComicFaceSequence=0;legacySeaBorn.Clear();ResetFormationSynergyEffects();}
            legacyLastClock=Model.Clock;
            EnsureLegacyMaterials();legacyFxUsed=0;legacyComicUsed=0;wisadelOrdinaryTrailsUsed=0;
            foreach(var action in Model.PresentationActions)PresentLegacyAction(action);
            foreach(var actor in Model.Units.Where(u=>u.Enabled))PresentLegacyOwner(actor);
            foreach(var hit in Model.Hits)PresentLegacyHit(hit);
            PresentFormationGuardEffects();
            foreach(var defender in Model.Defenders)
            {
                double at=Model.LegacyNerveBurstAt(defender);
                if(at>=0)LegacyOriginal("common/common_nerve_injury_01_hit",(Model.Clock-at)*LegacyVisualRate,defender.Position,38);
            }
            // Fire/snow domains are real v0.7 state, not guessed from damage counters.
            foreach(var zone in Model.LegacyZones())
            {
                var source=Model.Units.FirstOrDefault(u=>u.Id==zone.SourceId);if(source==null)continue;
                double age=(Model.Clock-zone.At)*LegacyVisualRate;
                if(source.ArtId=="TALULAH_TRIAL"&&zone.Kind=="burn")
                {
                    if(Campaign.TalulahCost==5)PresentFormationTalulahRain(zone,source);
                    else LegacyTalulah("target-burn-loop",age,zone.Center,source,-2,true);
                }
                else if(source.ArtId=="B15"&&zone.Kind=="kingdom")PresentFormationKingdomTiles(zone.Cells,age);
                else if(source.ArtId=="M14"&&zone.Kind=="snow")LegacyOriginal("firstar_skill_01_range",age,zone.Center,24,flat:true,scale:LegacyScale(source));
            }
            PresentFormationSynergyEffects();
            for(int i=legacyFxUsed;i<legacyFxViews.Count;i++)legacyFxViews[i].Renderer.enabled=false;
            for(int i=legacyComicUsed;i<legacyComicBursts.Count;i++)legacyComicBursts[i].Ink.enabled=legacyComicBursts[i].Light.enabled=false;
            for(int i=wisadelOrdinaryTrailsUsed;i<wisadelOrdinaryTrails.Count;i++)wisadelOrdinaryTrails[i].enabled=false;
            foreach(var old in legacyComicFaceIds.Keys.Where(h=>Model.Clock-h.At>.55/LegacyVisualRate+.1).ToArray())legacyComicFaceIds.Remove(old);
            PresentLegacySea();
            foreach(string key in legacyPatriotPages.Where(p=>Time.frameCount-p.Value.LastUse>8).Select(p=>p.Key).ToArray())
            {
                var page=legacyPatriotPages[key];foreach(var view in legacyFxViews)if(view.Renderer.sprite==page.Sprite)view.Renderer.sprite=null;
                Destroy(page.Sprite);Resources.UnloadAsset(page.Texture);legacyPatriotPages.Remove(key);
            }
        }

        void PresentFormationKingdomTiles(IReadOnlyList<Vector2Int> cells,double age)
        {
            if(cells==null)return;
            var clip=BossRosterFxArt.Get(KingdomRangeArt);
            if(!clip.Sequence.Ready)return;
            var frame=clip.At(age,true);
            // The captured source range is a single tile. Fit its transparent
            // canvas within one board cell and draw it on the actual zone tiles.
            foreach(var cell in cells)
                LegacyDraw(frame,legacyHdrMaterial,ActorWorldPoint(cell)+Vector3.up*.02f,cell,.60f,-5,hdr:clip.HdrRange,flat:true);
        }

        void PresentLegacySea()
        {
            bool active=Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting;
            int used=0;
            if(active&&Model.SeaCells.Count>0)
            {
                if(legacySeaMaterial==null)
                {
                    legacySeaMaterial=new Material(Resources.Load<Shader>("MarineTide")){name="Formation / original Nethersea brand"};
                    legacySeaMaterial.SetTexture("_MainTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_01"));
                    legacySeaMaterial.SetTexture("_DetailTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_04"));
                    legacySeaMaterial.SetTexture("_VeinTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_03"));
                    legacySeaMaterial.SetTexture("_FlowTex",Resources.Load<Texture2D>("MarineTide/flow_43_2"));
                    legacySeaMesh=new Mesh{name="Formation original sea surface cell"};legacySeaMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
                    legacySeaMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};legacySeaMesh.triangles=new[]{0,2,1,0,3,2};legacySeaMesh.RecalculateBounds();
                }
                var cells=new HashSet<Vector2Int>(Model.SeaCells.Select(Vector2Int.RoundToInt));
                foreach(var cell in cells)
                {
                    if(!FormationPointVisibleInView(cell))continue;
                    if(used==legacySeaTiles.Count)
                    {
                        var renderer=new GameObject("Formation original Nethersea cell",typeof(MeshRenderer),typeof(MeshFilter)).GetComponent<MeshRenderer>();
                        renderer.transform.SetParent(actorRoot,false);renderer.sharedMaterial=legacySeaMaterial;renderer.GetComponent<MeshFilter>().sharedMesh=legacySeaMesh;
                        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;legacySeaTiles.Add(renderer);
                    }
                    var tile=legacySeaTiles[used++];tile.enabled=true;tile.transform.position=ActorWorldPoint(cell)+Vector3.up*.025f;
                    if(!legacySeaBorn.TryGetValue(cell,out var born))legacySeaBorn[cell]=born=Model.Clock;
                    var properties=new MaterialPropertyBlock();float age=(float)((Model.Clock-born)*LegacyVisualRate);
                    properties.SetFloat("_MarineTime",(float)(Model.Clock*LegacyVisualRate));properties.SetFloat("_Growth",Mathf.SmoothStep(0,1,age/.7f));
                    properties.SetFloat("_Active",1);properties.SetFloat("_Highlight",0);properties.SetFloat("_Seed",0);properties.SetFloat("_BornAge",age);
                    properties.SetVector("_Edges",new Vector4(cells.Contains(cell+Vector2Int.left)?0:1,cells.Contains(cell+Vector2Int.right)?0:1,cells.Contains(cell+Vector2Int.down)?0:1,cells.Contains(cell+Vector2Int.up)?0:1));
                    tile.SetPropertyBlock(properties);
                }
            }
            for(int i=used;i<legacySeaTiles.Count;i++)legacySeaTiles[i].enabled=false;
        }
        internal void DisposeFormationLegacyEffects()
        {
            if(formationFlameSprayMaterial!=null)Destroy(formationFlameSprayMaterial);
            if(formationFlameCoreMaterial!=null)Destroy(formationFlameCoreMaterial);
            if(formationFlameSprayFrames!=null)
                foreach(var frame in formationFlameSprayFrames)if(frame!=null)Destroy(frame);
            formationFlameSprayMaterial=formationFlameCoreMaterial=null;formationFlameSprayFrames=null;
            ResetFormationSynergyEffects();
            foreach(var view in legacyFxViews)if(view.Renderer!=null){view.Renderer.enabled=false;Destroy(view.Renderer.gameObject);}legacyFxViews.Clear();
            foreach(var page in legacyPatriotPages.Values){Destroy(page.Sprite);Resources.UnloadAsset(page.Texture);}legacyPatriotPages.Clear();
            foreach(var clip in legacyTalulahFrames.Values)foreach(var sprite in clip.Frames)Destroy(sprite);legacyTalulahFrames.Clear();
            foreach(var tile in legacySeaTiles)if(tile!=null){tile.enabled=false;Destroy(tile.gameObject);}legacySeaTiles.Clear();
            foreach(var burst in legacyComicBursts)
            {if(burst.Ink!=null){burst.Ink.enabled=false;Destroy(burst.Ink.gameObject);}if(burst.Light!=null){burst.Light.enabled=false;Destroy(burst.Light.gameObject);}}
            legacyComicBursts.Clear();legacyComicFaceIds.Clear();legacyComicFaceSequence=0;legacyComicUsed=0;
            foreach(var trail in wisadelOrdinaryTrails){trail.enabled=false;Destroy(trail.gameObject);}
            wisadelOrdinaryTrails.Clear();wisadelOrdinaryTrailsUsed=0;
            if(legacyHdrMaterial!=null)Destroy(legacyHdrMaterial);if(legacyPremultMaterial!=null)Destroy(legacyPremultMaterial);if(legacyPatriotMaterial!=null)Destroy(legacyPatriotMaterial);
            if(legacySeaMaterial!=null)Destroy(legacySeaMaterial);if(legacySeaMesh!=null)Destroy(legacySeaMesh);
            if(legacyComicMaterial!=null)Destroy(legacyComicMaterial);legacyComicMaterial=null;
            if(guardWarningTile!=null)Destroy(guardWarningTile);guardWarningTile=null;
            legacyHdrMaterial=legacyPremultMaterial=legacyPatriotMaterial=legacySeaMaterial=null;legacySeaMesh=null;
            legacyPreviousShield.Clear();legacyShieldBroken.Clear();legacyShieldRefreshed.Clear();legacyFlightOrigins.Clear();legacySeaBorn.Clear();legacyObservedModel=null;legacyLastClock=-1;
        }
    }

    public sealed partial class FormationBattleModel
    {
        internal sealed class LegacyZoneSnapshot
        {
            internal int SourceId;
            internal string Kind;
            internal Vector2 Center;
            internal double At,Until;
            internal float Radius;
            internal IReadOnlyList<Vector2Int> Cells;
        }
        internal IEnumerable<LegacyZoneSnapshot> LegacyZones()
        {
            foreach(var zone in zones)if(zone.Until>Clock)yield return new LegacyZoneSnapshot
            {
                SourceId=zone.Source.Id,Kind=zone.Kind,Center=zone.Center,At=zone.StartedAt,
                Until=zone.Until,Radius=zone.Radius,Cells=zone.Cells
            };
        }
        internal double LegacyReflectAt(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)?state.LastReflect:double.NegativeInfinity;
        internal bool LegacyAwake(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)&&state.Stacks.TryGetValue("awake",out var value)&&value>0;
        internal double LegacyBuffUntil(FormationActor actor,string key)=>combat.TryGetValue(actor.Id,out var state)&&state.Buffs.TryGetValue(key,out var buff)?buff.Until:double.NegativeInfinity;
        internal double LegacyNerveBurstAt(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)&&state.Buffs.TryGetValue("nerve_lock",out var buff)?buff.Until-4:double.NegativeInfinity;
        internal bool LegacyMandragoraShield(FormationActor actor)=>combat.TryGetValue(actor.Id,out var state)&&state.Shields.Any(s=>s.Until>Clock&&s.Amount>0&&s.Key=="mandragora");
    }
}
