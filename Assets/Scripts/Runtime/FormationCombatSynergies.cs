using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        FormationFighter ResonanceLeader()
        {
            var u=PairMember(Campaign.ResonanceLeaderInstance);
            return u!=null&&Tagged(u,"同频")?u:Units.FirstOrDefault(f=>f.Enabled&&Tagged(f,"同频"));
        }
        FormationFighter ResonancePartner()
        {
            var lead=ResonanceLeader();var u=PairMember(Campaign.ResonancePartnerInstance);
            return u!=null&&u!=lead&&Tagged(u,"同频")?u:Units.FirstOrDefault(f=>f.Enabled&&f!=lead&&Tagged(f,"同频"));
        }
        bool IsResonanceMember(FormationFighter u)=>u==ResonanceLeader()||u==ResonancePartner();
        void Beat(FormationFighter source,float amount,bool skill)
        {
            int l=Level("同频");if(l<2)return;var leader=ResonanceLeader();var partner=ResonancePartner();
            if(leader?.Alive!=true||partner?.Alive!=true||!IsResonanceMember(source))return;
            resonanceBeats+=amount;
            while(resonanceBeats>=4)
            {
                resonanceBeats-=4;GainSP(leader,l>=5?5:l>=4?3:l>=3?2:1,false,true);
                GiveBuff(leader,"haste",l>=5?.55f:l>=4?.65f:l>=3?.75f:.85f,999);
                if(Key(partner)=="B11")GainSP(leader,1,false,true);
                if(Key(partner)=="M32"&&Clock-State(partner).LastResShield>=2){State(partner).LastResShield=Clock;Shield(partner,leader,leader.MaxHealth*.08f,4,"resonance");}
                if(Campaign.HasAugment("CP3")){GiveBuff(leader,"skill_bonus",.25f,999);State(leader).Stacks["resonant_cast"]=1;}
                BattleEvents.Add($"{Clock:0.00}:同频:{leader.Instance}");
            }
        }
        FormationFighter PromptPartner(FormationFighter source)
        {
            var a=PairMember(Campaign.PromptPairA);var b=PairMember(Campaign.PromptPairB);
            if(a==null||b==null||a==b||!Tagged(a,"提词")||!Tagged(b,"提词"))
            {var pair=Units.Where(u=>u.Enabled&&Tagged(u,"提词")).Take(2).ToArray();if(pair.Length<2)return null;a=pair[0];b=pair[1];}
            return source==a?b:source==b?a:null;
        }
        void PromptDamage(FormationFighter source)
        {
            int l=Level("提词");if(l<2)return;var partner=PromptPartner(source);if(partner?.Alive!=true)return;
            var s=State(source);float cap=l>=6?8:l>=4?6:4;float amount=Mathf.Min(cap-s.PromptReturned,l>=6?1.6f:l>=4?1.2f:.8f);
            if(amount>0){GainSP(partner,amount);s.PromptReturned+=amount;}
        }
        void PromptSupport(FormationFighter source)
        {int l=Level("提词");if(l<2)return;var partner=PromptPartner(source);if(partner?.Alive==true&&State(source).PromptReturned==0){float amount=l>=6?4:l>=4?3:2;GainSP(partner,amount);State(source).PromptReturned=amount;}}
        void SetupPairs()
        {
            int rides=Level("便车")>=6?2:Level("便车")>=2?1:0;if(Campaign.HasAugment("CP4"))rides=Mathf.Min(3,rides+1);
            var used=new System.Collections.Generic.HashSet<int>();
            foreach(var pair in Campaign.RidePairs.Take(rides))TryRide(PairMember(pair.Key),PairMember(pair.Value),used);
            int made=Units.Count(u=>State(u).RidingCarrier!=0);
            foreach(var passenger in Units.Where(u=>u.Enabled&&Tagged(u,"便车")&&State(u).Spec?.Size!="重"))
            {
                if(made>=rides)break;if(used.Contains(passenger.Id))continue;
                var carrier=Units.Where(u=>u.Enabled&&u!=passenger&&!used.Contains(u.Id)&&State(u).Spec?.Size=="重").OrderBy(u=>(u.Position-passenger.Position).sqrMagnitude).FirstOrDefault();
                if(TryRide(passenger,carrier,used))made++;
            }
            foreach(var carrier in Units.Where(u=>State(u).Passenger!=0))
            {
                float bonus=(Level("便车")>=4?.2f:0)+(Campaign.HasAugment("CP4")?.25f:0);carrier.MaxHealth*=1+bonus;carrier.Health=carrier.MaxHealth;
            }
            if(Level("伊比利亚")>=2){var a=Named("B23");var b=Named("B25");if(a!=null&&b!=null){State(b).AttackSpeed+=.15f;State(a).BaseArmor+=8;}}
        }
        bool TryRide(FormationFighter passenger,FormationFighter carrier,System.Collections.Generic.HashSet<int> used)
        {
            if(passenger==null||carrier==null||passenger==carrier||!passenger.Enabled||!carrier.Enabled||!Tagged(passenger,"便车")||State(passenger).Spec?.Size=="重"||State(carrier).Spec?.Size!="重"||used.Contains(passenger.Id)||used.Contains(carrier.Id))return false;
            State(passenger).RidingCarrier=carrier.Id;State(carrier).Passenger=passenger.Id;used.Add(passenger.Id);used.Add(carrier.Id);return true;
        }
        void UpdateRiders()
        {
            foreach(var p in Units.Where(u=>State(u).RidingCarrier!=0))
            {
                var carrier=Units.FirstOrDefault(u=>u.Id==State(p).RidingCarrier);
                if(carrier?.Alive!=true){State(p).RidingCarrier=0;if(carrier!=null)State(carrier).Passenger=0;}
                else if(p.Alive)p.Position=carrier.Position;
            }
        }
        void SetupSea()
        {
            int l=Level("海嗣"),count=l>=5?10:l>=4?7:l>=2?4:0;
            if(Campaign.SeaCellIds.Count>0)
            {
                if(ValidateSeaPlacement(out _))seaCells.AddRange(Campaign.SeaCellIds.Distinct().Select(id=>new Vector2(id%Width,id/Width)));
                return;
            }
            var centre=Units.FirstOrDefault(u=>u.Enabled&&Tagged(u,"海嗣"))?.Position??new Vector2(DeploymentColumns+2,3);
            centre.x=Mathf.Clamp(centre.x+4,DeploymentColumns+1,Width-2);
            var cells=Enumerable.Range(DeploymentColumns+1,Width-DeploymentColumns-2).SelectMany(x=>Enumerable.Range(0,Height).Select(y=>new Vector2(x,y))).OrderBy(p=>(p-centre).sqrMagnitude).ThenBy(p=>p.x).ThenBy(p=>p.y);
            seaCells.AddRange(cells.Take(count));
        }
        public void RefreshSeaPreview()
        { if(Campaign==null||Phase!=FormationPhase.Preparation)return;seaCells.Clear();SetupSea(); }
        public bool ValidateSeaPlacement(out string reason)
        {
            reason=null;if(Campaign==null||Campaign.SeaCellIds.Count==0)return true;
            int l=Level("海嗣"),cap=l>=5?10:l>=4?7:l>=2?4:0;
            var ids=Campaign.SeaCellIds.Distinct().ToArray();
            if(ids.Length>cap||ids.Length==0){reason="溟痕数量超过当前海嗣羁绊上限";return false;}
            foreach(int id in ids)
            {
                int col=id%Width,row=id/Width;
                if(id<0||col<DeploymentColumns+1||col>=Width-1||row<0||row>=Height){reason="溟痕必须放在右侧战场的有效格子";return false;}
            }
            var reached=new System.Collections.Generic.HashSet<int>{ids[0]};bool changed=true;
            while(changed)
            {
                changed=false;
                foreach(int id in ids)if(!reached.Contains(id)&&reached.Any(n=>Math.Abs(n%Width-id%Width)+Math.Abs(n/Width-id/Width)==1)){reached.Add(id);changed=true;}
            }
            if(reached.Count!=ids.Length){reason="溟痕必须上下左右连通";return false;}return true;
        }
        void ExtendSea()
        {
            var next=Enumerable.Range(DeploymentColumns+1,Width-DeploymentColumns-2).SelectMany(x=>Enumerable.Range(0,Height).Select(y=>new Vector2(x,y))).Where(p=>!seaCells.Contains(p)&&seaCells.Any(c=>Vector2.Distance(c,p)<=1.01f)).OrderBy(p=>p.x).ThenBy(p=>p.y).FirstOrDefault();
            if(next.x>=DeploymentColumns)seaCells.Add(next);
        }
        void SpawnGolem()
        {
            if(Level("泥岩")<2)return;var owners=Units.Where(u=>u.Enabled&&State(u).Spec?.Factions.Contains("泥岩")==true).ToArray();if(owners.Length==0)return;
            int starSum=owners.Sum(u=>u.Star),n=synergyCounts["泥岩"],xp=Campaign.MudrockXP;
            int stage=xp>=80&&starSum>=9&&n>=4?5:xp>=50&&starSum>=7&&n>=3?4:xp>=24&&starSum>=5&&n>=3?3:xp>=8&&starSum>=3&&n>=2?2:1;
            var golem=new FormationFighter{Id=nextSummon--,Name="泥岩巨像 "+stage,ArtId="GOLEM",Position=owners[0].Position,Spawn=owners[0].Spawn,Enabled=true,Alive=true,MaxHealth=owners.Average(u=>State(u).Spec.Health*(u.Star>=3?3.24f:u.Star==2?1.8f:1))*(stage+1),Attack=owners.Average(u=>State(u).BaseAttack)*new[]{0,1,1.3f,1.6f,2,2.5f}[stage],Armor=owners.Average(u=>u.Armor),MagicResistance=owners.Average(u=>u.MagicResistance),Interval=3,Range=1.5f,MoveSpeed=.6f};
            golem.Health=golem.MaxHealth;State(golem).ExpiresAt=double.PositiveInfinity;State(golem).Stage=stage;Summons.Add(golem);
            if(stage>=5)foreach(var u in Units.Where(u=>u.Enabled))GiveBuff(u,"golem_shield",.15f,999);
        }
        void SpawnDog(FormationFighter owner)
        {
            if(Summons.Count(d=>State(d).Passenger==owner.Id)>=2)return;
            var dog=new FormationFighter{Id=nextSummon--,Name="卡德霍猎犬",ArtId="P14H",Enabled=true,Alive=true,Position=owner.Position,Spawn=owner.Position,MaxHealth=owner.MaxHealth*.3f,Attack=owner.Attack*.65f,Armor=owner.Armor,MagicResistance=owner.MagicResistance,Interval=1.3f,MoveSpeed=1.2f,Range=.9f};
            dog.Health=dog.MaxHealth;var s=State(dog);s.ExpiresAt=Clock+10;s.Passenger=owner.Id;Summons.Add(dog);
        }
        void RelayReturned(FormationFighter source)
        {
            int l=Level("接棒");if(l<2||!Tagged(source,"接棒"))return;
            var receiver=PairMember(Campaign.RelayReceiverInstance);
            if(receiver==null||!Tagged(receiver,"接棒"))receiver=Units.FirstOrDefault(u=>u.Enabled&&Tagged(u,"接棒"));
            if(receiver==null||receiver==source||!relaySources.Add(source.Id))return;
            State(receiver).HandoffStacks=Math.Min(l>=6?5:l>=4?3:1,State(receiver).HandoffStacks+1);
            BattleEvents.Add($"{Clock:0.00}:接棒:{source.Instance}->{receiver.Instance}:{State(receiver).HandoffStacks}");
        }
        void KillCampaignFighter(FormationFighter f)
        {
            var s=State(f);s.LifeVersion++;f.Alive=false;f.IsMoving=false;f.LastDeathAt=Clock;f.TargetId=-1;f.Shield=0;s.Shields.Clear();
            if(Summons.Contains(f)){Summons.Remove(f);return;}
            int nativeMax=Key(f)=="B13"?2:new[]{"M30","M14","M32","B24","M20","B08"}.Contains(Key(f))?1:0;
            if(s.NativeLives<nativeMax)
            {
                s.NativeLives++;s.NativePending=true;s.NativeReturnAt=Clock+4;s.NativeReturnHealth=Key(f)=="M30"?.5:Key(f)=="M14"||Key(f)=="M32"?1:Key(f)=="B08"?.6:Key(f)=="B13"?.75:.7;
                f.RespawnRemaining=4;f.NativePhaseChangedAt=Clock;f.NativePhaseReturningUntil=Clock+4;
                f.PresentationAction="phase";f.PresentationStartedAt=Clock;f.PresentationDuration=4;return;
            }
            BeginModeDeath(f);
        }
        void BeginModeDeath(FormationFighter f)
        {
            var s=State(f);s.NativePending=false;f.Deaths++;s.EmergencyUsedThisLife=false;
            double wait=ReviveSeconds;
            if(f.Deaths==1&&Campaign.HasAugment("CS4"))wait=Math.Max(4,wait-2);
            if(f.Deaths==1&&Campaign.HasAugment("CP2"))wait=Math.Min(wait,4);
            if(Level("接棒")>=6&&s.HandoffStacks>=3&&!s.RelayQuickUsed){wait=Math.Min(wait,2);s.RelayQuickUsed=true;}
            f.RebornAt=Clock+wait;f.RespawnRemaining=(float)wait;s.BusyUntil=0;
            foreach(string key in new[]{"stun","freeze","silence","cold","heal_down","eblana_fire","wisadel"})s.Buffs.Remove(key);
            if(Key(f)=="M31")foreach(var d in Nearby(f.Position,2,100))CombatDamage(f,d,f.Attack*3*Power(f),1,true,false);
            if(finalDeaths.Add(f.Id))
            {
                foreach(var medic in Units.Where(u=>u.Enabled&&u.Alive&&HasItem(u,"DE")))
                {
                    if(Clock-State(medic).LastHealSP>=5&&Stack(medic,"rescue")<4){State(medic).LastHealSP=Clock;AddStack(medic,"rescue",1,4);foreach(var ally in LowAllies(2))CombatHeal(medic,ally,medic.MaxHealth*.12f,false,false);}
                }
                if(Level("国度")>=2)
                {
                    var partner=Key(f)=="B24"?Named("B15"):Key(f)=="B15"?Named("B24"):null;
                    if(partner?.Alive==true){Shield(f,partner,partner.MaxHealth*.25f,8,"kingdom_death");if(Key(f)=="B24")GainSP(partner,6);}
                }
            }
        }
        void ReturnCampaignFighter(FormationFighter f)
        {
            var s=State(f);bool native=s.NativePending;
            if(native&&Key(f)=="B08"&&!zones.Any(z=>z.Until>Clock&&(z.Kind=="burn"||z.Kind.StartsWith("deep:"))))
            {BeginModeDeath(f);return;}
            s.NativePending=false;f.Alive=true;f.Health=f.MaxHealth*(native?(float)s.NativeReturnHealth:1);f.Position=native?f.Position:f.SpawnCenter;
            f.ActionUntil=Clock+.2;f.NextAttackAt=Math.Max(f.NextAttackAt,Clock+.2);f.TargetId=-1;f.Facing=Vector2.right;f.RespawnRemaining=0;
            if(native)
            {
                s.Stage++;f.PhaseStage=s.Stage;NativePhasePresentation(f);
                if(Key(f)=="B24")GiveBuff(f,"next_trench",.5f,999);
                if(Key(f)=="B08"&&f.Star>=3)
                    foreach(var friend in Units.Where(u=>u!=f&&u.Enabled&&!u.Alive&&Tagged(u,"接棒")).ToArray())
                    {friend.Alive=true;friend.Health=friend.MaxHealth*.3f;friend.Position=friend.SpawnCenter;friend.RespawnRemaining=0;State(friend).NativePending=false;friend.RebornAt=0;/* a derived recall deliberately grants no handoff stack */}
            }
            else if(f.Deaths==1&&Campaign.HasAugment("CP2"))f.SkillCharge=Mathf.Max(f.SkillCharge,f.SkillRequired*.25f);
            RelayReturned(f);
        }
        void NativePhasePresentation(FormationFighter f)
        {
            f.NativePhase=State(f).Stage;f.NativePhaseChangedAt=Clock;f.NativePhaseReturningUntil=0;
            f.PresentationAction="phase";f.PresentationStartedAt=Clock;f.PresentationDuration=.65;
        }
    }
}
