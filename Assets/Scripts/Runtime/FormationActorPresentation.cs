using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        readonly Dictionary<string,FormationUnitArt> formationMonsterArt=new Dictionary<string,FormationUnitArt>();
        readonly HashSet<string> warmedFormationArt=new HashSet<string>();
        readonly HashSet<string> warmingFormationArt=new HashSet<string>();
        public bool RosterArtReady=>Model!=null&&RequiredFormationArtIds().All(FormationArtAvailable);

        bool FormationArtAvailable(string id)
        {
            if(!warmedFormationArt.Contains(id))return false;
            if(!id.StartsWith("D",StringComparison.Ordinal))return formationMonsterArt.TryGetValue(id,out var source)&&source.IsReady;
            return art.TryGetValue(id,out var defender)&&defender.Clips.Values.Where(c=>c.Key=="idle"||c.Key=="walk"||c.Key.StartsWith("attack",StringComparison.Ordinal)||c.Key=="death"||c.Key=="heal").All(c=>c.Ready);
        }

        IEnumerable<string> RequiredFormationArtIds()
        {
            if(Model==null)yield break;
            foreach(var id in Model.Units.Where(u=>u.Enabled).Select(u=>u.ArtId).Concat(Model.Defenders.Select(d=>d.ArtId)).Distinct())yield return id;
        }
        void EnsureFormationArt()
        {
            var required=RequiredFormationArtIds().ToList();
            var dragged=Shop?.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);
            if(pointerHeld&&dragged!=null)required.Add(FormationUnitCatalog.Find(dragged.Id).ArtId);
            foreach(string id in required.Distinct())
                if(!FormationArtAvailable(id)&&!warmingFormationArt.Contains(id))StartCoroutine(WarmFormationArt(id));
            pinned.Clear();
            foreach(string id in required.Distinct())
            {
                string root=id.StartsWith("D",StringComparison.Ordinal)?"DefenderArt/"+id:FormationUnitCatalog.Find(id)?.ArtRoot;
                if(!string.IsNullOrEmpty(root))pinned.Add(root);
            }
            ArtMemoryCache.Pin(pinned);
        }
        IEnumerator WarmFormationArt(string id)
        {
            if(FormationArtAvailable(id)||!warmingFormationArt.Add(id))yield break;
            loading="准备原有动作  "+id;
            if(id.StartsWith("D",StringComparison.Ordinal))
            {
                string root="DefenderArt/"+id;var source=ArtCatalog.Read(root);art[id]=source;
                if(!pinned.Contains(root))pinned.Add(root);ArtMemoryCache.Pin(pinned);
                foreach(var clip in source.Clips.Values)
                    if(clip.Key=="idle"||clip.Key=="walk"||clip.Key.StartsWith("attack",StringComparison.Ordinal)||clip.Key=="death"||clip.Key=="heal")yield return clip.Warm();
            }
            else
            {
                var source=FormationUnitArt.For(id);
                if(source==null)throw new InvalidOperationException("Missing formation art: "+id);
                formationMonsterArt[id]=source;
                var definition=FormationUnitCatalog.Find(id);
                if(!string.IsNullOrEmpty(definition.ArtRoot)&&!pinned.Contains(definition.ArtRoot))pinned.Add(definition.ArtRoot);
                ArtMemoryCache.Pin(pinned);yield return source.Warm();
            }
            warmedFormationArt.Add(id);warmingFormationArt.Remove(id);
        }
        public float ActorPresentationHeight(FormationActor actor)=>actor is FormationFighter?
            FormationUnitCatalog.Find(actor.ArtId)?.VisualHeight??1.12f:1.12f;

        Rect FormationActorScreenRect(FormationFighter actor)
        {
            if(bodies.TryGetValue(actor.Id,out var body)&&body!=null&&body.sprite!=null)
            {
                var bounds=body.sprite.bounds;float left=float.MaxValue,right=float.MinValue,top=float.MaxValue,bottom=float.MinValue;
                foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})
                {
                    var world=body.transform.TransformPoint(new Vector3(body.flipX?-x:x,y,0));
                    var point=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(world));
                    left=Mathf.Min(left,point.x);right=Mathf.Max(right,point.x);top=Mathf.Min(top,point.y);bottom=Mathf.Max(bottom,point.y);
                }
                return Rect.MinMaxRect(left-5,top-5,right+5,bottom+6);
            }
            var foot=CellScreen(actor.SpawnCenter);return new Rect(foot.x-28,foot.y-75,56,82);
        }
        void PresentFormationActor(FormationActor actor,bool defender)
        {
            if(!bodies.TryGetValue(actor.Id,out var body))
            {
                body=new GameObject(actor.Name,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                body.transform.SetParent(actorRoot,false);body.sharedMaterial=actorMaterial;bodies[actor.Id]=body;
                ActorGrounding.Attach(body,ActorPresentationHeight(actor));
            }
            double now=Model.Clock,deathAge=now-actor.LastDeathAt;
            body.enabled=FormationActorVisibleInView(actor)&&FormationArtAvailable(actor.ArtId)&&(actor.Alive||deathAge>=0&&deathAge<.65);
            if(!body.enabled)return;
            bool acting=actor.Alive&&now<actor.ActionUntil&&actor.LastActionAt>=0;
            string key=!actor.Alive?"death":actor.IsMoving?"walk":acting?"attack":"idle";
            double age=key=="death"?deathAge:key=="attack"?now-actor.LastActionAt:
                Model.Phase==FormationPhase.Preparation?Time.unscaledTimeAsDouble:now;
            float sourceHeight;
            if(!defender)
            {
                var source=formationMonsterArt[actor.ArtId];sourceHeight=source.Height;
                if(!source.HasAction(key))
                {
                    if(key=="death"){body.enabled=false;return;}
                    key="idle";
                }
                if(key=="attack")age=Math.Min(.999999,Math.Max(0,age/Math.Max(.001,actor.ActionUntil-actor.LastActionAt)))*source.Duration(key);
                body.sprite=source.Frame(key,age,key!="attack"&&key!="death");
            }
            else
            {
                var source=art[actor.ArtId];sourceHeight=source.Height;
                if(!source.Clips.TryGetValue(key,out var clip))clip=source.Clips["idle"];
                if(key=="attack")
                {
                    double progress=Math.Min(.999999,Math.Max(0,age/Math.Max(.001,actor.ActionUntil-actor.LastActionAt)));
                    source.Clips.TryGetValue("attack_start",out var begin);source.Clips.TryGetValue("attack_end",out var end);
                    double at=progress*(clip.Duration+(begin?.Duration??0)+(end?.Duration??0));
                    if(begin!=null&&at<begin.Duration){clip=begin;age=at;}
                    else if(at<(begin?.Duration??0)+clip.Duration)age=at-(begin?.Duration??0);
                    else {age=at-(begin?.Duration??0)-clip.Duration;clip=end??clip;}
                }
                int frame=(int)(Math.Max(0,age)*(clip.Count/clip.Duration));
                frame=key=="death"||key=="attack"?Mathf.Clamp(frame,0,clip.Count-1):frame%clip.Count;body.sprite=clip.Frames[frame];
            }
            body.enabled=body.sprite!=null;if(!body.enabled)return;
            body.flipX=actor.Facing.x<-.01f;body.transform.position=ActorWorldPoint(actor);body.transform.rotation=battleCamera.transform.rotation;
            float scale=ActorPresentationHeight(actor)/Mathf.Max(.5f,sourceHeight>0?sourceHeight:1.4f);
            // This approved comic package has a single standing pose. A small presentation-only
            // breath keeps preparation live without inventing a missing frame animation.
            float breath=!defender&&actor.ArtId=="WISADEL_APRIL"&&Model.Phase==FormationPhase.Preparation?1+Mathf.Sin(Time.unscaledTime*2.6f)*.007f:1;
            body.transform.localScale=new Vector3(scale,scale*breath,scale);
            body.sortingOrder=5000-Mathf.RoundToInt(actor.Position.y*50);
            bool skill=now-actor.LastSkillAt<.45;
            Color color=now-actor.LastHitAt<.13?new Color(1,.5f,.42f):skill?new Color(1,.89f,.58f):Color.white;
            if(actor is FormationFighter fighter&&fighter.Instance==shopDragInstance&&DragPreviewVisible)color.a=.25f;
            body.color=color;
        }
    }
}
