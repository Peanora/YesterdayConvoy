using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        const string SynergyActivationSoundResource="SynergyActivation/bond-original";
        const float SynergyActivationSoundAt=1f/30f;
        readonly SynergyActivationFeedback synergyActivationFeedback=new SynergyActivationFeedback();
        ShopTrialModel synergyActivationShop;
        AudioSource synergyActivationSource;
        bool synergyActivationSoundStarted,synergyActivationSoundPaused;
        int synergyActivationSoundPlayCount;

        bool SynergyActivationCanPrepare=>CampaignActive&&Loop.CanShop&&Battle!=null&&Battle.Phase==BattlePhase.Ready;
        bool SynergyActivationWaitingForArrival
        {
            get
            {
                var batch=synergyActivationFeedback.Active;if(batch==null)return false;
                // Purchases join the real model before their existing .63s arrival finishes.
                // Wait for that presentation's reveal; clicking a reserve card has no such wait.
                return shopFlows.Any(flow=>batch.IsAddedMember(flow.Id)&&Time.unscaledTime-flow.At<flow.Reveal);
            }
        }
        bool SynergyActivationBlocked=>ArtLoading||Battle==null||Battle.IsPaused||CampaignUiModalOpen||loopMenuOpen||SynergyActivationWaitingForArrival;

        void RefreshSynergyActivationFeedback()
        {
            if(!SynergyActivationCanPrepare)return;
            // SyncShopDeck recreates Battle even for an unchanged lineup. Shop identity lasts
            // across those rebuilds and ordinary stages, but changes for a new challenge.
            if(!ReferenceEquals(synergyActivationShop,shop))
            {
                ClearSynergyActivationFeedback();synergyActivationShop=shop;
            }
            if(!synergyActivationFeedback.Observe(CurrentSynergies))return;
            StopSynergyActivationSound();
        }

        void TickSynergyActivationFeedback(float delta)
        {
            if(!SynergyActivationCanPrepare)
            {
                if(synergyActivationFeedback.Pending)ClearSynergyActivationFeedback(false);
                UpdateSynergyActivationSound();return;
            }
            synergyActivationFeedback.Advance(delta,SynergyActivationBlocked);
            UpdateSynergyActivationSound();
        }

        void UpdateSynergyActivationSound()
        {
            var batch=synergyActivationFeedback.Active;
            if(batch==null||!SynergyActivationCanPrepare){StopSynergyActivationSound();return;}
            if(synergyActivationSource!=null)synergyActivationSource.mute=Muted;
            if(SynergyActivationBlocked)
            {
                if(synergyActivationSource!=null&&synergyActivationSource.isPlaying)
                {synergyActivationSource.Pause();synergyActivationSoundPaused=true;}
                return;
            }
            float age=batch.Age-SynergyActivationSoundAt;
            if(age<0)return;
            if(synergyActivationSource==null)
            {
                var clip=Resources.Load<AudioClip>(SynergyActivationSoundResource);
                if(clip==null)return;
                synergyActivationSource=gameObject.AddComponent<AudioSource>();
                synergyActivationSource.playOnAwake=false;synergyActivationSource.loop=false;
                synergyActivationSource.spatialBlend=0;synergyActivationSource.volume=.65f;
                synergyActivationSource.pitch=1;synergyActivationSource.clip=clip;
            }
            var source=synergyActivationSource;source.mute=Muted;
            if(age>=source.clip.length){source.Stop();synergyActivationSoundPaused=false;return;}
            int sample=Mathf.Clamp(Mathf.RoundToInt(age*source.clip.frequency),0,source.clip.samples-1);
            if(!synergyActivationSoundStarted)
            {
                synergyActivationSoundStarted=true;source.timeSamples=sample;source.Play();
                synergyActivationSoundPlayCount++;
            }
            if(synergyActivationSoundPaused){source.UnPause();synergyActivationSoundPaused=false;}
            if(source.isPlaying&&Math.Abs(source.timeSamples-sample)>source.clip.frequency*.045f)source.timeSamples=sample;
        }

        void StopSynergyActivationSound()
        {
            if(synergyActivationSource!=null)synergyActivationSource.Stop();
            synergyActivationSoundStarted=synergyActivationSoundPaused=false;
        }

        void ClearSynergyActivationFeedback(bool resetObserved=true)
        {
            synergyActivationFeedback.Clear(resetObserved);StopSynergyActivationSound();
        }

        void DrawSynergyActivationFeedback()
        {
            if(!SynergyActivationCanPrepare||CampaignUiModalOpen||loopMenuOpen||SynergyActivationWaitingForArrival)return;
            var batch=synergyActivationFeedback.Active;if(batch==null)return;
            for(int index=0;index<shopLineup.Count;index++)
                SynergyActivationPresentation.DrawCard(ApprovedUi.LineupSlot(index),shopLineup[index],batch);
        }

        void DisableSynergyActivationFeedback()
        {
            ClearSynergyActivationFeedback();synergyActivationShop=null;
        }
    }
}
