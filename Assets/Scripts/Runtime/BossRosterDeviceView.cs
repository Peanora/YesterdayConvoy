using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Renders actual simulated devices and source auxiliary creatures. No logical object is created here.</summary>
    public sealed class BossRosterDeviceView:MonoBehaviour
    {
        sealed class Entry
        {
            internal BossRosterObject State;internal double RemovedAt=double.PositiveInfinity;
            internal string BodyState;internal double StateAt;
            internal GameObject Root;internal MeshRenderer Mesh;internal SpriteRenderer Sprite;internal double Shot=double.NegativeInfinity;
            internal readonly List<Flight> Flights=new List<Flight>();
        }
        sealed class Flight {internal long Activation;internal double At,Impact;internal Vector3 Start,End;internal bool Buffed;}
        sealed class Fact {internal BossRosterFxArt.Clip Clip;internal double At,Until;internal Vector3 Position;internal bool Loop;internal string TargetId;}
        readonly HashSet<long> factsSeen=new HashSet<long>();readonly HashSet<(long,string)> impacts=new HashSet<(long,string)>();
        readonly List<Fact> facts=new List<Fact>();
        readonly Dictionary<string,double> pulses=new Dictionary<string,double>();
        readonly Dictionary<string,double> camouflage=new Dictionary<string,double>();
        void AddFact(BossRosterFxArt.Clip clip,double at,Vector3 position,bool loop=false,double until=double.PositiveInfinity,string target=null)
        {facts.Add(new Fact{Clip=clip,At=at,Until=loop?until:Math.Min(until,at+clip.Duration),Position=position,Loop=loop,TargetId=target});}
        public void NotifyFact(long id,long activation,int eventRun,string card,string kind,string skill,int phase,double at,double end,Vector3 origin,Vector3 target,string targetId,bool applied,int hitIndex=0)
        {
            var battle=source?.Invoke();if(battle==null||battle.RunId!=eventRun)return;
            if(observed!=battle||run!=eventRun){Clear();observed=battle;run=eventRun;}
            if(!factsSeen.Add(id))return;if(kind=="Impact"&&applied)impacts.Add((activation,skill));
            if(kind=="StateStart"&&skill=="coldWave"&&!string.IsNullOrEmpty(targetId))pulses[targetId]=at;
            if(card=="CARD_TALULAH_TRIAL")
            {
                string suffix=phase>=2?"B":"A";
                if(kind=="SkillStart"&&(skill=="ignite"||skill=="detonate"||skill=="energy"))
                    AddFact(BossRosterSupplementArt.Talulah(skill=="energy"?"summon-B-caster":(skill=="ignite"?"ignite-":"explode-")+suffix+"-caster"),at,origin);
                if(kind=="Impact"&&applied&&skill=="detonate")AddFact(BossRosterSupplementArt.Talulah("explode-"+suffix+"-target"),at,target);
                if(kind=="StateStart"&&skill=="breath")
                {
                    facts.RemoveAll(f=>f.TargetId==targetId&&f.Loop);
                    AddFact(BossRosterSupplementArt.Talulah("ignite-"+suffix+"-target"),at,target);
                    AddFact(BossRosterSupplementArt.Talulah("target-burn-loop"),at,target,true,end,targetId);
                }
            }
            if(card=="CARD_WISADEL_APRIL"&&kind=="Impact"&&applied&&skill=="soul")
                foreach(var key in new[]{"token_wisdel_attack_01_hit_sale#14","token_wisdel_attack_01_hit_02_sale#14"})AddFact(BossRosterFxArt.Get(BossRosterFxArt.Key("B20",key)),at,target);
            if(card=="CARD_B14"&&kind=="Impact"&&applied&&(skill=="attachBite"||skill=="hound"))
                AddFact(BossRosterFxArt.Get(BossRosterFxArt.Key("B14",skill=="hound"?"enemy_shsdgo_attack_hit_01":"enemy_shsdgo_skill_01_hit_0"+(Math.Abs(hitIndex)%3+1))),at,target);
            if(card=="CARD_B14"&&kind=="Impact"&&applied&&(skill=="pain"||skill=="painRoar"||skill=="painBlink"))AddFact(BossRosterFxArt.Get(BossRosterFxArt.Key("B14","enemy_cadejo_attack_01_hit_01")),at,target);

        }
        Func<ManualBattle> source;Func<double> presentation;ManualBattle observed;int run=-1;
        readonly Dictionary<string,double> removals=new Dictionary<string,double>();
        readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();BossRosterCannonArt cannon;BossRosterFxLayer fx;Material actor,crystalMaterial;
        public int VisibleDevices {get;private set;}
        public double PendingUntil {get;private set;}=double.NegativeInfinity;
        public void Bind(Func<ManualBattle> battle,Func<double> clock=null){Clear();source=battle;presentation=clock;}
        public void NotifyRemoved(int eventRun,string id,double at){var battle=source?.Invoke();if(eventRun!=(battle?.RunId??-1)||string.IsNullOrEmpty(id))return;if(observed!=battle||run!=eventRun){Clear();observed=battle;run=eventRun;}removals[id]=at;if(entries.TryGetValue(id,out var entry))entry.RemovedAt=at;}
        internal static IEnumerable<ArtSequence> SequencesFor(IEnumerable<string> cards)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var card in cards)
            {
                if(card=="CARD_B23"){var sequence=BossRosterCannonArt.TextureSequence;if(seen.Add(sequence.CacheKey))yield return sequence;}
                foreach(var sequence in BossRosterSupplementArt.SequencesFor(card))if(seen.Add(sequence.CacheKey))yield return sequence;
                if(card=="CARD_B14")foreach(var sequence in BossRosterArt.For("P14H").AllSequences)if(seen.Add(sequence.CacheKey))yield return sequence;
                string auxiliary=card=="CARD_B14"?"P14":card=="CARD_B26"?"P26":card=="CARD_WISADEL_APRIL"?"P20":card=="CARD_B12"?"P12I":card=="CARD_B15"?"P15C":card=="CARD_B24"?"P24D":card=="CARD_B25"?"P25M":null;
                var art=auxiliary==null?null:BossRosterArt.For(auxiliary);if(art==null)continue;
                foreach(var sequence in art.AllSequences)if(seen.Add(sequence.CacheKey))yield return sequence;
            }
        }
        void Show(string task,string raw,double at,double now,Vector3 position,Camera camera,bool repeat=false,Vector3 direction=default,bool orient=false)
        {
            if(!BossRosterFxArt.TryGet(BossRosterFxArt.Key(task,raw),out var clip)||now<at)return;
            fx.Show(clip,now-at,position,camera,false,repeat&&clip.Loop,direction,orient);
            if(!repeat&&at<=observed.Clock+1e-6)PendingUntil=Math.Max(PendingUntil,at+clip.Duration);
        }
        void Cannon(BossRosterObject value,Entry entry,double now,Camera camera)
        {
            if(cannon==null)cannon=new BossRosterCannonArt();
            if(entry.Mesh==null){entry.Root=new GameObject("Original salt cannon / "+value.Id,typeof(MeshFilter),typeof(MeshRenderer));entry.Root.transform.SetParent(transform,false);entry.Root.GetComponent<MeshFilter>().sharedMesh=cannon.Mesh;entry.Mesh=entry.Root.GetComponent<MeshRenderer>();entry.Mesh.sharedMaterial=cannon.Material;}
            bool alive=value.Alive&&double.IsInfinity(entry.RemovedAt);double destroyedAt=Math.Min(value.DestroyedAt,entry.RemovedAt);
            entry.Root.transform.position=value.Position;entry.Mesh.enabled=alive;
            Vector3 direction=Vector3.ProjectOnPlane(value.TargetPosition-value.Position,Vector3.up);
            if(direction.sqrMagnitude>.001f)entry.Root.transform.rotation=Quaternion.FromToRotation(Vector3.right,direction.normalized); // Explicit static-mesh aiming adaptation.
            if(alive)
            {
                VisibleDevices++;Show("B23","trap_cnnon_start",value.SpawnAt,now,value.Position,camera);
                Show("B23","trap_cnnon_idle",value.SpawnAt,now,value.Position,camera,true);
                if(value.BuffUntil>observed.Clock)Show("B23","trap_cnnon_buff",value.SpawnAt,now,value.Position,camera,true);
            }
            else if(!double.IsInfinity(destroyedAt))Show("B23","trap_cnnon_destroyed",destroyedAt,now,value.Position,camera);
            if(value.ShotAt>entry.Shot&&!double.IsInfinity(value.ShotAt))
            {entry.Shot=value.ShotAt;entry.Flights.Add(new Flight{Activation=value.ActivationId,At=value.ShotAt,Impact=value.ImpactAt,Start=value.Position,End=value.TargetPosition,Buffed=value.BuffUntil>value.ShotAt});}
            for(int i=entry.Flights.Count-1;i>=0;i--)
            {
                var flight=entry.Flights[i];if(now>flight.Impact+4){entry.Flights.RemoveAt(i);continue;}
                if(now<flight.Impact)
                {float t=Mathf.Clamp01((float)((now-flight.At)/Math.Max(.001,flight.Impact-flight.At)));Show("B23",flight.Buffed?"trap_cnnon_trail_2":"trap_cnnon_trail_1",flight.At,now,Vector3.Lerp(flight.Start,flight.End,t),camera,true,flight.End-flight.Start,true);}
                else if(observed.Clock+1e-6>=flight.Impact&&impacts.Contains((flight.Activation,value.Kind)))
                {Show("B23",flight.Buffed?"trap_cnnon_explode_2":"trap_cnnon_explode_1",flight.Impact,now,flight.End,camera);Show("B23","trap_cnnon_explode_hit",flight.Impact,now,flight.End,camera);}
            }
        }
        void Creature(BossRosterObject value,Entry entry,string task,double now,Camera camera)
        {
            var art=BossRosterArt.For(task);if(art==null)return;
            if(entry.Sprite==null)
            {
                entry.Root=new GameObject("Original auxiliary / "+value.Id,typeof(SpriteRenderer));entry.Root.transform.SetParent(transform,false);entry.Sprite=entry.Root.GetComponent<SpriteRenderer>();
                if(actor==null)actor=new Material(Resources.Load<Shader>("TacticalActor"));entry.Sprite.sharedMaterial=actor;
            }
            string raw=value.Animation;double at=value.AnimationAt;bool loop=value.AnimationLoop;
            bool alive=value.Alive&&double.IsInfinity(entry.RemovedAt);double destroyedAt=Math.Min(value.DestroyedAt,entry.RemovedAt);
            if(!alive){raw=!string.IsNullOrEmpty(value.Animation)&&value.Animation.StartsWith("Die",StringComparison.Ordinal)&&art.HasClip(value.Animation)?value.Animation:art.HasClip("death")?"death":art.HasClip("Die_1")?"Die_1":"Die";at=destroyedAt;loop=false;}
            else if(string.IsNullOrEmpty(raw)||!value.AnimationLoop&&now>=value.AnimationUntil)
            {
                raw=art.HasClip("Start")&&now-value.SpawnAt<art["Start"].Duration?"Start":value.Velocity.sqrMagnitude>.0001f&&art.HasClip("Move")?"Move":"Idle";
                at=value.SpawnAt;loop=raw!="Start";
            }
            if(alive&&raw=="Idle"&&value.Velocity.sqrMagnitude>.0001f&&art.HasClip("Move")){raw="Move";at=value.SpawnAt;loop=true;}
            if(alive&&raw=="Idle"&&art.HasClip("Start")&&now<value.SpawnAt+art["Start"].Duration){raw="Start";at=value.SpawnAt;loop=false;}
            entry.Root.transform.SetPositionAndRotation(value.Position,camera.transform.rotation);entry.Sprite.flipX=value.Velocity.x<0;
            entry.Sprite.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,value.Position)+8;
            if(task=="P14")
            {
                Show("B14","enemy_cadesd_start_01",value.SpawnAt,now,value.Position,camera);
                if(raw=="Skill_2_Begin"||raw=="Skill_2_Loop")
                {Show("B14","enemy_cadesd_skill_02_start_01",at,now,value.Position,camera,true);Show("B14","enemy_cadesd_skill_02_start_02",at,now,value.Position,camera,true);}
                if(raw=="Skill_1")Show("B14","enemy_cadejo_skill_01_start_01",at,now,value.Position,camera);
            }            if(task=="P14H"&&alive)
            {
                if(raw=="Skill_Begin")Show("B14","enemy_shsdgo_skill_01_start",at,now,value.Position,camera);
                if(raw=="Skill_Loop")
                {
                    var attached=observed.FindDefender(value.TargetId);var position=attached!=null?attached.Position:value.TargetPosition;
                    Show("B14","enemy_shsdgo_skill_01_trap_loop_hit",at,now,position,camera,true);
                    Show("B14","enemy_shsdgo_buff_01_hit_01",at,now,position,camera,true);
                }
            }
            if(task=="P20"&&alive)
            {
                Show("B20","token_wisdel_birth_01_start_sale#14",value.SpawnAt,now,value.Position,camera);
                Show("B20","token_wisdel_buff_01_sale#14",value.SpawnAt,now,value.Position,camera,true);
                if(value.AttackStartedAt<=observed.Clock&&!double.IsInfinity(value.AttackStartedAt)&&!double.IsNaN(value.AttackStartedAt))Show("B20","token_wisdel_attack_01_start_sale#14",value.AttackStartedAt,now,value.Position,camera);
            }
            if(alive&&(task=="P20"||task=="P14"))AuxiliaryAttack(value,entry,task,now,camera);
            if(art.HasZeroDurationState(raw)){entry.Sprite.enabled=false;return;}
            if(!art.HasClip(raw)){entry.Sprite.enabled=false;return;}
            var clip=art[raw];double age=now-at;entry.Sprite.enabled=!clip.TransparentWait&&age>=0&&(loop||age<clip.Duration);
            if(entry.Sprite.enabled){entry.Sprite.sprite=clip.At(age,loop,out _);VisibleDevices++;}
            if(!loop&&at<=observed.Clock+1e-6)PendingUntil=Math.Max(PendingUntil,at+clip.Duration);
        }

        void AuxiliaryAttack(BossRosterObject value,Entry entry,string task,double now,Camera camera)
        {
            if(task=="P14"&&!double.IsInfinity(value.AttackStartedAt))Show("B14","enemy_cadejo_attack_01_start_01",value.AttackStartedAt,now,value.Position,camera);
            if(value.ShotAt>entry.Shot&&!double.IsInfinity(value.ShotAt))
            {entry.Shot=value.ShotAt;entry.Flights.Add(new Flight{Activation=value.ActivationId,At=value.ShotAt,Impact=value.ImpactAt,Start=value.Position,End=value.TargetPosition});}
            for(int i=entry.Flights.Count-1;i>=0;i--)
            {
                var flight=entry.Flights[i];if(now>=flight.Impact+4){entry.Flights.RemoveAt(i);continue;}
                if(task=="P20"&&now>=flight.At&&now<flight.Impact&&BossRosterFxArt.TryGet(BossRosterFxArt.Key("B20","token_wisdel_attack_01_line_sale#14"),out var line))
                {float t=Mathf.Clamp01((float)((now-flight.At)/Math.Max(.001,flight.Impact-flight.At)));fx.ShowBeam(line,now-flight.At,flight.Start,Vector3.Lerp(flight.Start,flight.End,t),camera,3);}
            }
        }
        void SupplementalObject(BossRosterObject value,Entry entry,double now,Camera camera)
        {
            bool alive=value.Alive&&double.IsInfinity(entry.RemovedAt);double death=Math.Min(value.DestroyedAt,entry.RemovedAt);
            void Draw(BossRosterFxArt.Clip clip,double at,bool loop=false,float forward=0)
            {
                if(now<at)return;fx.Show(clip,now-at,value.Position,camera,false,loop,value.Velocity,true,24,forward);
                if(!loop&&at<=observed.Clock)PendingUntil=Math.Max(PendingUntil,at+clip.Duration);
            }
            if(value.Kind=="energy")
            {
                if(alive)
                {
                    VisibleDevices++;Draw(BossRosterSupplementArt.Talulah("orb-body-idle"),value.SpawnAt,true);
                    foreach(var key in new[]{"orb-map_fire_crystal_birth_01_start","orb-map_fire_crystal_birth_01_buff"})Draw(BossRosterSupplementArt.Talulah(key),value.SpawnAt);
                    Draw(BossRosterSupplementArt.Talulah("orb-map_fire_crystal_buff_01_start"),value.SpawnAt,true);
                }
                else if(!double.IsInfinity(death))foreach(var key in new[]{"orb-body-die","orb-map_fire_crystal_die_01_start","orb-map_fire_crystal_skill_01_start"})Draw(BossRosterSupplementArt.Talulah(key),death);
            }
            else if(value.Kind=="energyFireball")
            {
                if(alive){VisibleDevices++;Draw(BossRosterSupplementArt.Talulah("original-energy-stream"),value.SpawnAt,true,180);}
                else if(!double.IsInfinity(death))Draw(BossRosterSupplementArt.Talulah("original-fire-impact"),death);
            }
            else if(value.Kind=="blackIce"&&alive)
            {
                VisibleDevices++;Draw(BossRosterSupplementArt.Frost("firstar_skill_02_range_02"),value.SpawnAt,true);
                Draw(BossRosterSupplementArt.Frost("firstar_skill_02_range"),value.SpawnAt);
            }
            else if(value.Kind=="iceCrystal")
            {
                // Source SummonFrosts is a level branch with no crystal mesh. Explicit gameplay structure adaptation.
                if(entry.Mesh==null)
                {
                    entry.Root=GameObject.CreatePrimitive(PrimitiveType.Cube);entry.Root.name="Cold-wave crystal / explicit structure adaptation";entry.Root.transform.SetParent(transform,false);
                    var collider=entry.Root.GetComponent<Collider>();if(collider!=null)Destroy(collider);entry.Mesh=entry.Root.GetComponent<MeshRenderer>();
                    if(crystalMaterial==null){crystalMaterial=new Material(Resources.Load<Shader>("BossRosterCannonStructure"));crystalMaterial.SetColor("_Color",new Color(.34f,.74f,.83f,1));}
                    entry.Mesh.sharedMaterial=crystalMaterial;entry.Root.transform.localScale=new Vector3(.32f,.75f,.32f);
                }
                entry.Mesh.enabled=alive;entry.Root.transform.SetPositionAndRotation(value.Position+Vector3.up*.5f,Quaternion.Euler(0,45,18));if(alive)VisibleDevices++;
                if(pulses.TryGetValue(value.Id,out double pulse))Draw(BossRosterSupplementArt.Frost("firstar_skill_01_range"),pulse);
            }
        }
        void OriginalDeviceBody(BossRosterObject value,Entry entry,double now,Camera camera)
        {
            bool alive=value.Alive&&double.IsInfinity(entry.RemovedAt);double death=Math.Min(value.DestroyedAt,entry.RemovedAt);
            void Body(string task,string key,double at,bool loop=false,bool hold=false)
            {
                if(!BossRosterFxArt.TryGet(BossRosterFxArt.Key(task,key),out var clip)||now<at)return;
                double age=now-at;if(hold)age=Math.Min(age,clip.Duration-1e-5);
                if(fx.Show(clip,age,value.Position,camera,false,loop&&clip.Loop,Vector3.zero,false,8))VisibleDevices++;
                if(!alive&&at<=observed.Clock)PendingUntil=Math.Max(PendingUntil,at+clip.Duration);
            }
            if(value.Kind=="oldfire"&&alive)Body("B08","trap_220_ftbone__Appear__structure",value.SpawnAt,hold:true);
            else if(value.Kind=="camera"&&alive)
            {
                string state=value.BuffUntil>observed.Clock?"ON":"OFF";
                if(entry.BodyState!=state){bool first=entry.BodyState==null;entry.BodyState=state;entry.StateAt=first?value.SpawnAt:observed.Clock;}
                Body("B09","trap_247_crfilm__"+state+"__structure",entry.StateAt,hold:true);
            }
            else if(value.Kind=="node")
            {
                if(alive&&now-value.SpawnAt<.8333335)Body("B11","trap_134_condtr__trap_006_antidr_start",value.SpawnAt);
                else if(alive)Body("B11","trap_134_condtr__trap_006_antidr_working",value.SpawnAt+.8333334327,true);
                else if(!double.IsInfinity(death))Body("B11","trap_134_condtr__trap_006_dead",death);
            }
            else if(value.Kind=="waymark"&&alive)Body("B12","enemy_2095_skzamf_01",value.SpawnAt,true);
        }
        void Present(BossRosterObject value,Entry entry,double now,Camera camera)
        {
            if(value.Kind=="oldfire"||value.Kind=="camera"||value.Kind=="node"||value.Kind=="waymark")OriginalDeviceBody(value,entry,now,camera);
            else if(value.Kind=="cannon")Cannon(value,entry,now,camera);
            else if(value.Kind=="energy"||value.Kind=="energyFireball"||value.Kind=="blackIce"||value.Kind=="iceCrystal")SupplementalObject(value,entry,now,camera);
            else Creature(value,entry,value.Kind=="pain"?"P14":value.Kind=="hound"?"P14H":value.Kind=="beam"?"P26":value.Kind=="idea"?"P12I":value.Kind=="core"?"P15C":value.Kind=="deyin"?"P24D":value.Kind=="matrix"?"P25M":"P20",now,camera);
        }
        void FrostLifecycle(ManualBattle battle,double now,Camera camera)
        {
            foreach(var unit in battle.Units)
            {
                if(unit.CardId!="CARD_M14")continue;
                if(unit.IsReviving)
                {
                    double at=unit.ReviveAt-5;
                    foreach(var key in new[]{"frstar2_buff_01_start","frstar2_buff_02_start","frstar2_buff_03_start","frstar2_buff_04_start","frstar2_buff_05_start"})Show("B18",key,at,now,unit.Motion.Position,camera,true);
                }
                if(unit.Resolution==UnitResolution.Dead)Show("B18","frstar2_die_01_start",unit.DeathTime,now,unit.Motion.Position,camera);
            }
        }
        void LateUpdate()
        {
            var battle=source?.Invoke();if(battle!=observed||run!=(battle?.RunId??-1)){Clear();observed=battle;run=battle?.RunId??-1;}
            if(battle==null||Camera.main==null)return;if(fx==null)fx=new BossRosterFxLayer(transform);fx.Begin();VisibleDevices=0;PendingUntil=double.NegativeInfinity;
            double now=presentation?.Invoke()??battle.Clock;
            foreach(var entry in entries.Values)if(entry.Root!=null)foreach(var renderer in entry.Root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            var seen=new HashSet<string>();
            foreach(var value in battle.BossRosterObjects)
            {
                if(value.Kind!="cannon"&&value.Kind!="pain"&&value.Kind!="hound"&&value.Kind!="beam"&&value.Kind!="soul"&&value.Kind!="energy"&&value.Kind!="energyFireball"&&value.Kind!="blackIce"&&value.Kind!="iceCrystal"&&value.Kind!="idea"&&value.Kind!="core"&&value.Kind!="deyin"&&value.Kind!="matrix"&&value.Kind!="oldfire"&&value.Kind!="camera"&&value.Kind!="node"&&value.Kind!="waymark")continue;
                if(!entries.TryGetValue(value.Id,out var entry)){entry=new Entry();entries.Add(value.Id,entry);}
                entry.State=value;seen.Add(value.Id);if(removals.TryGetValue(value.Id,out double removed))entry.RemovedAt=removed;
                Present(value,entry,now,Camera.main);
            }
            foreach(var pair in entries)if(!seen.Contains(pair.Key)&&pair.Value.State!=null)
            {
                var entry=pair.Value;var value=entry.State;double death=Math.Min(value.DestroyedAt,entry.RemovedAt);if(double.IsInfinity(death)||now>=death+4)continue;
                Present(value,entry,now,Camera.main);
            }
            for(int i=facts.Count-1;i>=0;i--)
            {
                var fact=facts[i];if(now>=fact.Until){facts.RemoveAt(i);continue;}
                if(fact.TargetId!=null){var d=battle.FindDefender(fact.TargetId);if(d==null||!d.Alive){facts.RemoveAt(i);continue;}fact.Position=d.Position;}
                fx.Show(fact.Clip,now-fact.At,fact.Position,Camera.main,false,fact.Loop,Vector3.zero,false);
                if(!fact.Loop&&fact.At<=battle.Clock)PendingUntil=Math.Max(PendingUntil,fact.Until);
            }
            FrostLifecycle(battle,now,Camera.main);
            foreach(var unit in battle.Units)if(unit.Active&&unit.CardId=="CARD_WISADEL_APRIL")
            {
                bool active=battle.BossCoreCamouflaged(unit);if(!active){camouflage.Remove(unit.EntityId);continue;}
                if(!camouflage.TryGetValue(unit.EntityId,out double since)){since=now;camouflage.Add(unit.EntityId,since);}
                Show("B20","wisdel_camouflage_buff_01_sale#14",since,now,unit.Motion.Position,Camera.main,true);
            }
            fx.End();
        }
        public void Clear()
        {
            foreach(var entry in entries.Values)if(entry.Root!=null){foreach(var r in entry.Root.GetComponentsInChildren<Renderer>())r.enabled=false;Destroy(entry.Root);}
            entries.Clear();removals.Clear();facts.Clear();factsSeen.Clear();impacts.Clear();pulses.Clear();camouflage.Clear();fx?.Clear();cannon?.Dispose();cannon=null;VisibleDevices=0;PendingUntil=double.NegativeInfinity;observed=null;run=-1;
        }
        void OnDisable()=>Clear();
        void OnDestroy(){Clear();fx?.Dispose();cannon?.Dispose();if(actor!=null)Destroy(actor);if(crystalMaterial!=null)Destroy(crystalMaterial);}
    }
}
