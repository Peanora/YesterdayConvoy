using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool ArtLoading {get;private set;}
        public float ArtLoadingProgress {get;private set;}
        public string ArtLoadingMessage {get;private set;}
        int artGeneration;
        bool artCompleting;
        // Plans can coexist while entry/loading/pinning are being resolved. Associate
        // native requests with the actual plan, never a shared "last plan" snapshot.
        static readonly ConditionalWeakTable<List<ArtSequence>,string[]> nativeArtPlans=new ConditionalWeakTable<List<ArtSequence>,string[]>();
        void CancelArtPreparation(){if(artCompleting)return;artGeneration++;ArtLoading=false;}
        static IEnumerable<ArtSequence> CardSequences(CardDefinition card,bool idleOnly=false)
        {
            if(card==null)yield break;
            var boss=BossRosterArt.For(card.CardId);
            if(boss!=null&&BossRosterArt.IsCoreCard(card.CardId)&&!idleOnly){foreach(var clip in BossCoreArt.SequencesFor(card.CardId))yield return clip;yield break;}
            if(boss!=null){foreach(var clip in idleOnly?boss.SequencesFor(new[]{"idle"}):boss.AllSequences)yield return clip;yield break;}
            var local=LocalMonsterArt.For(card.CardId);
            var sequence=local?.Sequences??OriginalSlugArt.For(card.CardId)?.Sequences??OriginalTalulahArt.For(card)?.Sequences;
            if(sequence==null)yield break;
            foreach(var clip in sequence)if(!idleOnly||clip.Key=="idle")yield return clip;
        }
        static List<ArtSequence> Plan(IEnumerable<DefenderPlacement> defenders,IEnumerable<CardDefinition> cards,bool tactical,bool talulahSecondPhase=false)
        {
            defenders=defenders.ToArray();cards=cards.ToArray();
            var clips=new List<ArtSequence>();foreach(var card in cards)
            {
                clips.AddRange(CardSequences(card));
                if(talulahSecondPhase&&card!=null&&card.CardId==OriginalTalulahArt.CardId)
                {
                    var original=OriginalTalulahArt.For(card);
                    if(original==null)throw new InvalidOperationException("Talulah original art is required for the second phase.");
                    if(BossRosterArt.For(card.CardId)==null)clips.AddRange(original.SecondPhaseSequences);
                    clips.AddRange(TalulahAttackVfx.SecondPhaseSequences);
                }
            }
            foreach(var defender in defenders)
            {
                if(string.IsNullOrEmpty(defender.artId))continue;
                clips.AddRange(DefenderArt.Load(defender.artId).Sequences);
                if(tactical)clips.AddRange(DefenderArt.Load(defender.artId+"B").Sequences);
                if(defender.skill?.kind==DefenderSkillKind.WangGambit)clips.AddRange(DefenderArt.Load("D28Chess").Sequences);
            }
            var defenderEffects=DefenderVfxArt.RequiredKeysForArtIds(defenders.Select(d=>d.artId)).Distinct().ToArray();
            var nativeEffects=defenderEffects.Where(OriginalNativeFxCatalog.Supports).ToArray();
            clips.AddRange(OriginalCombatVfxArt.SequencesFor(defenderEffects.Where(key=>!OriginalNativeFxCatalog.Supports(key))));
            clips.AddRange(CommonCombatVfxDirector.SequencesFor());
            foreach(string id in cards.Where(c=>c!=null).Select(c=>c.CardId).Distinct())
            {
                string[] auxiliary=id=="CARD_B12"?new[]{"AUX_P12I"}:id=="CARD_B14"?new[]{"AUX_P14","AUX_P14H"}:id=="CARD_B15"?new[]{"AUX_P15C"}:id=="CARD_B24"?new[]{"AUX_P24D"}:id=="CARD_B25"?new[]{"AUX_P25M"}:id=="CARD_B26"?new[]{"AUX_P26"}:ViviCombat.IsVivi(id)?new[]{"AUX_P20"}:Array.Empty<string>();
                foreach(string name in auxiliary)clips.AddRange((BossRosterArt.For(name)??throw new InvalidOperationException("Required original auxiliary model missing: "+name)).AllSequences);
            }
            clips.AddRange(BossRosterFxArt.SequencesFor(cards.Where(c=>c!=null).Select(c=>c.CardId)));
            clips.AddRange(OriginalCombatVfxArt.SequencesFor(MonsterOriginalVfx.RequiredKeysForCards(cards.Where(c=>c!=null).Select(c=>c.CardId))));
            var plan=clips.GroupBy(s=>s.CacheKey).Select(g=>g.First()).ToList();
            if(nativeEffects.Length>0)nativeArtPlans.Add(plan,nativeEffects);
            return plan;
        }
        List<ArtSequence> CurrentBattlePlan()
        {
            var cards=defenderTrial==null?new[]{definition.Card}:new[]{defenderTrial.battle.Card}.Concat((defenderTrial.rosterCards??Array.Empty<ManualBattleDefinition>()).Select(d=>d.Card));
            if(CampaignActive)cards=cards.Concat(new[]{"CARD_M07","CARD_M08"}.Where(shopDefinitions.ContainsKey).Select(id=>shopDefinitions[id].Card)).Concat(shopLineup.Count(id=>CampaignBalance.MudrockCrew(id)||PreparedMudrockTransferIds.Contains(id))>=2?new[]{GolemCard}:Array.Empty<CardDefinition>());
            var snapshot=cards.ToArray();
            var plan=Plan(defenderTrial?.defenders??Array.Empty<DefenderPlacement>(),snapshot,TacticalMapActive,NeedsTalulahSecondPhase(snapshot));
            if(defenderTrial==null)AddWolfTrialTarget(plan,definition.Card);
            return plan;
        }
        bool NeedsTalulahSecondPhase(IEnumerable<CardDefinition> cards)
        {
            if(!cards.Any(card=>card!=null&&card.CardId==OriginalTalulahArt.CardId))return false;
            if(CampaignActive)return ShopLineupPiece(OriginalTalulahArt.CardId)?.Star==3;
            if(Battle!=null&&Battle.EngagementEnabled&&Battle.CombatSettings.Effects.TalulahSecondPhase)return true;
            if(definition?.Card.CardId==OriginalTalulahArt.CardId&&definition.Engagement.Snapshot().Effects.TalulahSecondPhase)return true;
            return defenderTrial?.rosterCards?.Any(data=>data.Card.CardId==OriginalTalulahArt.CardId&&data.Engagement.Snapshot().Effects.TalulahSecondPhase)==true;
        }
        void RefreshArtPins()
        {
            var keys=CurrentBattlePlan().Select(c=>c.CacheKey).ToList();keys.Add("portraits");
            if(shop!=null)
            {
                var displayed=shop.Bench.Select(p=>p.Id);
                if(shopVisible)displayed=displayed.Concat(shop.Offers.Where(id=>id!=null));
                foreach(string id in displayed.Distinct())if(shopDefinitions.TryGetValue(id,out var data))keys.AddRange(CardSequences(data.Card,true).Select(s=>s.CacheKey));
            }
            ArtMemoryCache.Pin(keys);
            if(isActiveAndEnabled)StartCoroutine(TrimAfterPresentation());
        }
        IEnumerator TrimAfterPresentation(){yield return null;if(!ArtLoading)ArtMemoryCache.Trim();}
        IEnumerator Prepare(List<ArtSequence> plan,Action complete,string title)
        {
            if(ArtLoading)yield break;
            ArtLoading=true;ArtLoadingProgress=0;ArtLoadingMessage=title;
            int generation=++artGeneration;
            nativeArtPlans.TryGetValue(plan,out var nativeEffects);
            int stages=plan.Count+(nativeEffects!=null&&nativeEffects.Length>0?1:0);
            var timer=System.Diagnostics.Stopwatch.StartNew();int prepared=ArtMemoryCache.PreparedLoads,raw=ArtMemoryCache.RuntimePngLoads;
            try
            {
                yield return null; // Present loading feedback before uploading the first atlas.
                if(generation!=artGeneration)yield break;
                for(int i=0;i<plan.Count;i++)
                {
                    var clip=plan[i];ArtLoadingMessage=title+"  "+(i+1)+" / "+plan.Count;
                    var warm=clip.Warm();try{while(warm.MoveNext()){yield return warm.Current;if(generation!=artGeneration)yield break;}}finally{(warm as IDisposable)?.Dispose();}
                    ArtLoadingProgress=(i+1f)/Math.Max(1,stages);
                }
                if(nativeEffects!=null&&nativeEffects.Length>0)
                {
                    ArtLoadingMessage=title+" · 准备战斗特效";
                    var nativeWarm=OriginalNativeFxCatalog.Prepare(nativeEffects);
                    try{while(nativeWarm.MoveNext()){yield return nativeWarm.Current;if(generation!=artGeneration)yield break;}}
                    finally{(nativeWarm as IDisposable)?.Dispose();}
                    if(generation!=artGeneration)yield break;
                }
                ArtLoadingProgress=1;
                artCompleting=true;try{complete?.Invoke();}finally{artCompleting=false;}
                SnowstormArt.WarmFor(Battle?.SnowTier??0);
                RefreshArtPins();yield return null;if(generation!=artGeneration)yield break;ArtMemoryCache.Trim();
                Debug.Log("ART_PREPARE: "+title+"; ms="+timer.ElapsedMilliseconds+"; prepared_pages="+(ArtMemoryCache.PreparedLoads-prepared)+"; png_fallback="+(ArtMemoryCache.RuntimePngLoads-raw)+"; resident="+ArtMemoryCache.ResidentBytes+"; budget="+ArtMemoryCache.BudgetBytes);
            }
            finally{if(generation==artGeneration){ArtLoading=false;ArtLoadingProgress=1;}}
        }
        void QueueArt(List<ArtSequence> plan,Action complete,string title)
        {if(!ArtLoading)StartCoroutine(Prepare(plan,complete,title));}
        public IEnumerator PrepareBattleShop(bool lateGame=true,int seed=0)
        {
            var cards=monsterTests.Where(o=>o.Playable&&new[]{"CARD_M08","CARD_M02","CARD_M17","CARD_M18","CARD_M05"}.Contains(o.Id)).Select(o=>o.Definition.Card);
            if(!lateGame)cards=monsterTests.Where(o=>o.Playable&&o.Definition.Card.RecruitCost==1).Select(o=>o.Definition.Card);
            return Prepare(Plan(BattleShopStages.Defenders(1,lateGame),cards,true),()=>StartBattleShopLoop(seed,lateGame),"准备本关角色");
        }
        static void AddWolfTrialTarget(List<ArtSequence> plan,CardDefinition card)
        {
            if(card.CardId!=SilverWolfCombat.CardId)return;
            foreach(var clip in OriginalSlugArt.For(OriginalSlugArt.CardId).Sequences)
                if(clip.Key=="idle" || clip.Key=="attack" || clip.Key=="death")plan.Add(clip);
        }
        void PrepareMonsterEntry(MonsterTestOption option)
        {
            var plan=Plan(Array.Empty<DefenderPlacement>(),new[]{option.Definition.Card},false,option.Definition.Engagement.Snapshot().Effects.TalulahSecondPhase);
            AddWolfTrialTarget(plan,option.Definition.Card);
            QueueArt(plan,()=>TryEnterMonsterTest(option.Id),"准备怪物动作");
        }
        void PrepareDefenderEntry(string id)
        {
            var data=Resources.Load<DefenderTrialDefinition>("DefenderTrials/"+id);
            if(data==null)return;
            QueueArt(Plan(data.defenders,new[]{data.battle.Card}.Concat((data.rosterCards??Array.Empty<ManualBattleDefinition>()).Select(d=>d.Card)),data.Tactical),()=>TryEnterDefenderSkillTrial(id),"准备守军动作");
        }
        void PrepareNextLoopBattle()
        {
            var cards=shopLineup.Concat(CampaignActive?new[]{"CARD_M07","CARD_M08"}:Array.Empty<string>()).Distinct().Select(id=>shopDefinitions[id].Card)
                .Concat(CampaignActive&&shopLineup.Count(id=>CampaignBalance.MudrockCrew(id)||PreparedMudrockTransferIds.Contains(id))>=2?new[]{GolemCard}:Array.Empty<CardDefinition>()).ToArray();
            QueueArt(Plan(shopMapCopy.defenders,cards,true,NeedsTalulahSecondPhase(cards)),()=>{
                if(StartLoopNextBattle()&&JourneyActive&&Loop.RewardStage)ClaimCampaignSupply();
            },"准备下一关");
        }
        void WarmVisibleShop()
        {
            if(ArtLoading||shop==null||!shopVisible)return;
            var plan=shop.Bench.Select(p=>p.Id).Concat(shop.Offers.Where(id=>id!=null)).Distinct().Where(shopDefinitions.ContainsKey).SelectMany(id=>CardSequences(shopDefinitions[id].Card,true)).ToList();
            if(plan.Any(s=>!s.Ready))QueueArt(plan,null,"准备商店待机");
        }
        void DrawArtProgress()
        {
            EnsureStyles();var prior=GUI.matrix;GUI.matrix=TacticalViewport.Current.Matrix;
            Fill(new Rect(0,0,1280,720),new Color(.025f,.04f,.055f));
            GUI.Label(new Rect(240,283,800,46),ArtLoadingMessage,textStyle);
            Fill(new Rect(240,352,800,5),new Color(.15f,.2f,.24f));Fill(new Rect(240,352,800*ArtLoadingProgress,5),new Color(.40f,.78f,.82f));
            GUI.Label(new Rect(240,378,800,34),"正在准备角色资源",smallStyle);GUI.matrix=prior;
        }
    }
}
