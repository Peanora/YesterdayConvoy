using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        void CancelArtPreparation(){if(artCompleting)return;artGeneration++;ArtLoading=false;}
        static IEnumerable<ArtSequence> CardSequences(CardDefinition card,bool idleOnly=false)
        {
            if(card==null)yield break;
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
                    clips.AddRange(original.SecondPhaseSequences);clips.AddRange(TalulahAttackVfx.SecondPhaseSequences);
                }
            }
            foreach(var defender in defenders)
            {
                if(string.IsNullOrEmpty(defender.artId))continue;
                clips.AddRange(DefenderArt.Load(defender.artId).Sequences);
                if(tactical)clips.AddRange(DefenderArt.Load(defender.artId+"B").Sequences);
                if(defender.skill?.kind==DefenderSkillKind.WangGambit)clips.AddRange(DefenderArt.Load("D28Chess").Sequences);
            }
            clips.AddRange(OriginalCombatVfxArt.SequencesFor(DefenderVfxArt.RequiredKeysForArtIds(defenders.Select(d=>d.artId))));
            clips.AddRange(CommonCombatVfxDirector.SequencesFor());
            clips.AddRange(OriginalCombatVfxArt.SequencesFor(MonsterOriginalVfx.RequiredKeysForCards(cards.Where(c=>c!=null).Select(c=>c.CardId))));
            return clips.GroupBy(s=>s.CacheKey).Select(g=>g.First()).ToList();
        }
        List<ArtSequence> CurrentBattlePlan()
        {
            var cards=defenderTrial==null?new[]{definition.Card}:new[]{defenderTrial.battle.Card}.Concat((defenderTrial.rosterCards??Array.Empty<ManualBattleDefinition>()).Select(d=>d.Card));
            if(CampaignActive)cards=cards.Concat(new[]{"CARD_M07","CARD_M08"}.Where(shopDefinitions.ContainsKey).Select(id=>shopDefinitions[id].Card)).Concat(shopLineup.Count(id=>CampaignBalance.MudrockCrew(id)||PreparedMudrockTransferIds.Contains(id))>=2?new[]{GolemCard}:Array.Empty<CardDefinition>());
            var snapshot=cards.ToArray();
            return Plan(defenderTrial?.defenders??Array.Empty<DefenderPlacement>(),snapshot,TacticalMapActive,NeedsTalulahSecondPhase(snapshot));
        }
        bool NeedsTalulahSecondPhase(IEnumerable<CardDefinition> cards)
        {
            if(!cards.Any(card=>card!=null&&card.CardId==OriginalTalulahArt.CardId))return false;
            if(CampaignActive)return shopLineup.Contains(OriginalTalulahArt.CardId)&&shop.Bench.Any(p=>p.Id==OriginalTalulahArt.CardId&&p.Star==3);
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
            var timer=System.Diagnostics.Stopwatch.StartNew();int prepared=ArtMemoryCache.PreparedLoads,raw=ArtMemoryCache.RuntimePngLoads;
            try
            {
                yield return null; // Present loading feedback before uploading the first atlas.
                if(generation!=artGeneration)yield break;
                for(int i=0;i<plan.Count;i++)
                {
                    var clip=plan[i];ArtLoadingMessage=title+"  "+(i+1)+" / "+plan.Count;
                    var warm=clip.Warm();try{while(warm.MoveNext()){yield return warm.Current;if(generation!=artGeneration)yield break;}}finally{(warm as IDisposable)?.Dispose();}
                    ArtLoadingProgress=(i+1f)/plan.Count;
                }
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
        void PrepareMonsterEntry(MonsterTestOption option)=>QueueArt(Plan(Array.Empty<DefenderPlacement>(),new[]{option.Definition.Card},false,option.Definition.Engagement.Snapshot().Effects.TalulahSecondPhase),()=>TryEnterMonsterTest(option.Id),"准备怪物动作");
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
            QueueArt(Plan(shopMapCopy.defenders,cards,true,NeedsTalulahSecondPhase(cards)),()=>StartLoopNextBattle(),"准备下一关");
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
