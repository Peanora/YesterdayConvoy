using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        const float ShopArrivalDuration=.48f;
        readonly Dictionary<int,SpriteRenderer> shopBenchModels=new Dictionary<int,SpriteRenderer>();
        readonly List<ShopArrival> shopArrivals=new List<ShopArrival>();
        GameObject shopModelStage;
        Camera shopModelCamera;
        RenderTexture shopModelTarget;
        float shopVisibilityFrom,shopVisibilityAt=-100,shopVisibilityDuration;
        sealed class ShopArrival
        {
            internal int Instance;
            internal string Id;
            internal float At;
            internal Vector2 From;
            internal bool NewPiece;
            internal SpriteRenderer Model;
        }
        public float ShopPanelVisibility
        {
            get
            {
                if(!ShopTrialActive)return 0;
                float t=shopVisibilityDuration<=0?1:Mathf.Clamp01((Time.unscaledTime-shopVisibilityAt)/shopVisibilityDuration);
                return Mathf.Lerp(shopVisibilityFrom,shopVisible?1:0,t*t*(3-2*t));
            }
        }
        public bool ShopPanelTransitioning=>ShopTrialActive&&Time.unscaledTime-shopVisibilityAt<shopVisibilityDuration;
        public int ShopArrivalCount=>shopArrivals.Count;
        public SpriteRenderer ShopBenchModel(int instance)=>shopBenchModels.TryGetValue(instance,out var model)?model:null;
        void AnimateShopVisibility(bool visible)
        {
            if(shopVisible==visible)return;
            shopVisibilityFrom=ShopPanelVisibility;shopVisible=visible;shopVisibilityAt=Time.unscaledTime;
            shopVisibilityDuration=(visible?.24f:.18f)*Mathf.Abs((visible?1:0)-shopVisibilityFrom);
        }
        void ResetShopPresentation()
        {
            ClearShopBenchModels();shopVisibilityFrom=shopVisible?1:0;shopVisibilityAt=-100;shopVisibilityDuration=0;
        }
        void EnsureShopModelStage()
        {
            if(shopModelStage!=null)return;
            shopModelStage=new GameObject("Shop bench animated battle models");shopModelStage.transform.SetParent(transform,false);shopModelStage.transform.position=new Vector3(0,-2000,0);
            shopModelTarget=new RenderTexture(1920,1080,16,RenderTextureFormat.ARGB32){name="Shop models transparent overlay",filterMode=FilterMode.Bilinear};shopModelTarget.Create();
            shopModelCamera=new GameObject("Shop model camera").AddComponent<Camera>();shopModelCamera.transform.SetParent(shopModelStage.transform,false);
            shopModelCamera.transform.localPosition=new Vector3(6.4f,3.6f,-10);shopModelCamera.orthographic=true;shopModelCamera.orthographicSize=3.6f;
            shopModelCamera.nearClipPlane=.1f;shopModelCamera.farClipPlane=20;shopModelCamera.cullingMask=1<<31;
            shopModelCamera.clearFlags=CameraClearFlags.SolidColor;shopModelCamera.backgroundColor=Color.clear;shopModelCamera.allowHDR=false;shopModelCamera.allowMSAA=false;
            shopModelCamera.targetTexture=shopModelTarget;shopModelCamera.enabled=false;
        }
        SpriteRenderer NewShopModel(string name)
        {
            var go=new GameObject(name,typeof(SpriteRenderer));go.layer=31;go.transform.SetParent(shopModelStage.transform,false);
            return go.GetComponent<SpriteRenderer>();
        }
        static Sprite ShopIdleFrame(string id,double time)
        {
            var local=LocalMonsterArt.For(id);if(local!=null)return local["idle"].At(time,true,out _);
            var slug=OriginalSlugArt.For(id);return slug==null?null:slug["idle"].At(time,true,out _);
        }
        static float ShopModelScale(string id)
        {
            float height=LocalMonsterArt.For(id)?.Height??1;
            return .42f/1.855f*Mathf.Min(1,2.05f/height);
        }
        static Vector2 ShopModelFoot(int slot)=>new Vector2(ShopBenchRect(slot).center.x,693);
        static Vector3 ShopModelPosition(Vector2 gui)=>new Vector3(gui.x*.01f,(720-gui.y)*.01f,0);
        void SyncShopBenchModels()
        {
            if(!ShopTrialActive)return;
            EnsureShopModelStage();
            foreach(int instance in shopBenchModels.Keys.Where(id=>!shop.Bench.Any(p=>p.Instance==id)).ToArray())
            {
                shopBenchModels[instance].gameObject.SetActive(false);Destroy(shopBenchModels[instance].gameObject);shopBenchModels.Remove(instance);
            }
            foreach(var piece in shop.Bench)if(!shopBenchModels.ContainsKey(piece.Instance))shopBenchModels.Add(piece.Instance,NewShopModel("Bench "+piece.Id+" #"+piece.Instance));
            UpdateShopBenchModels();
        }
        void BeginShopArrival(int slot,ShopTrialModel.Result result,bool newPiece)
        {
            shopArrivals.Add(new ShopArrival{Instance=result.Instance,Id=result.Id,At=Time.unscaledTime,From=shopVisible?new Vector2(ShopOfferRect(slot).x+64,ShopOfferRect(slot).y+126):new Vector2(1191,54),NewPiece=newPiece,Model=NewShopModel("Purchased "+result.Id)});
        }
        static Vector2 ShopArrivalPoint(ShopArrival arrival,Vector2 destination,float progress)
        {
            float t=1-Mathf.Pow(1-progress,2),u=1-t;
            return u*u*u*arrival.From+3*u*u*t*(arrival.From+new Vector2(0,150))+3*u*t*t*(destination-new Vector2(0,125))+t*t*t*destination;
        }
        void LateUpdate(){if(ShopTrialActive){if(shopModelStage==null)SyncShopBenchModels();else UpdateShopBenchModels();}}
        void UpdateShopBenchModels()
        {
            for(int i=shopArrivals.Count-1;i>=0;i--)
                if(Time.unscaledTime-shopArrivals[i].At>=ShopArrivalDuration||!shop.Bench.Any(p=>p.Instance==shopArrivals[i].Instance))
                {shopArrivals[i].Model.gameObject.SetActive(false);Destroy(shopArrivals[i].Model.gameObject);shopArrivals.RemoveAt(i);}
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];if(!shopBenchModels.TryGetValue(piece.Instance,out var model))continue;
                model.sprite=ShopIdleFrame(piece.Id,Time.unscaledTime+piece.Instance*.13);
                float burst=ShopBenchPromotionRemaining(piece.Instance),scale=ShopModelScale(piece.Id)*(1+.12f*Mathf.Sin((1-burst)*Mathf.PI)*burst);
                model.transform.localScale=Vector3.one*scale;model.transform.localPosition=ShopModelPosition(ShopModelFoot(i));model.sortingOrder=10;
                if(shopMoving&&shopDrag==piece.Instance){model.transform.localPosition=ShopModelPosition(shopPointer+new Vector2(0,-8));model.transform.localScale*=1.15f;model.sortingOrder=110;}
                float alpha=shopArrivals.Any(a=>a.Instance==piece.Instance&&a.NewPiece)?0:1;
                model.color=piece.Star==3&&burst>0?new Color(1,1,.65f+.35f*(1-burst),alpha):new Color(1,1,1,alpha);
            }
            foreach(var arrival in shopArrivals)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==arrival.Instance);float t=Mathf.Clamp01((Time.unscaledTime-arrival.At)/ShopArrivalDuration);
                arrival.Model.sprite=ShopIdleFrame(arrival.Id,Time.unscaledTime);arrival.Model.sortingOrder=100;
                arrival.Model.transform.localPosition=ShopModelPosition(ShopArrivalPoint(arrival,ShopModelFoot(slot),t));
                arrival.Model.transform.localScale=Vector3.one*ShopModelScale(arrival.Id)*(1.15f-.15f*t);
                arrival.Model.color=new Color(.75f+.25f*t,1,1,1);
            }
        }
        void ClearShopBenchModels()
        {
            if(shopModelStage!=null){shopModelStage.SetActive(false);Destroy(shopModelStage);shopModelStage=null;}
            if(shopModelTarget!=null){shopModelTarget.Release();Destroy(shopModelTarget);shopModelTarget=null;}
            shopModelCamera=null;shopBenchModels.Clear();shopArrivals.Clear();
        }
        void DrawShopBench(GUIStyle center)
        {
            Fill(new Rect(0,632,1280,88),new Color(.035f,.06f,.075f,.98f));
            Fill(new Rect(8,632,1264,1),new Color(.3f,.44f,.48f));
            GUI.Label(new Rect(18,631,1240,18),$"备战区  {shop.Bench.Count}/10    编队 {shopLineup.Count}/{shop.Level}    点击编队 · 拖到红门部署 · 向下出售",new GUIStyle(center){fontSize=10,alignment=TextAnchor.MiddleLeft,normal={textColor=new Color(.57f,.72f,.77f)}});
            for(int i=0;i<10;i++)
            {
                var r=ShopBenchRect(i);var piece=i<shop.Bench.Count?shop.Bench[i]:null;
                bool active=piece!=null&&shopLineup.Contains(piece.Id)&&shop.Bench.Where(p=>p.Id==piece.Id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First().Instance==piece.Instance;
                Color color=piece!=null&&piece.Star>1?ShopRankColor(piece.Star,.65f):new Color(.24f,.39f,.44f);
                for(int line=0;line<9;line++){float width=Mathf.Lerp(76,106,line/8f);Fill(new Rect(r.center.x-width/2,686+line,width,1),new Color(.10f,.16f,.19f));}
                ShopLine(new Vector2(r.center.x-38,686),new Vector2(r.center.x+38,686),color,1);
                ShopLine(new Vector2(r.center.x-53,695),new Vector2(r.center.x+53,695),active?new Color(.45f,.88f,.92f):color,active?2:1);
                if(piece==null){GUI.Label(new Rect(r.x,662,r.width,25),"+",new GUIStyle(center){fontSize=18,normal={textColor=new Color(.18f,.29f,.32f)}});continue;}
                float burst=ShopBenchPromotionRemaining(piece.Instance);
                if(burst>0)DrawShopPromotionLight(ShopModelFoot(i),piece.Star,burst);
            }
            foreach(var arrival in shopArrivals)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==arrival.Instance);if(slot<0)continue;
                float t=Mathf.Clamp01((Time.unscaledTime-arrival.At)/ShopArrivalDuration);
                Vector2 head=ShopArrivalPoint(arrival,ShopModelFoot(slot),t);
                for(int trail=1;trail<8;trail++)
                {
                    Vector2 tail=ShopArrivalPoint(arrival,ShopModelFoot(slot),Mathf.Max(0,t-trail*.022f));
                    ShopLine(tail,head,new Color(.45f,.9f,1,(1-trail/8f)*.2f),Mathf.Max(1,5-trail*.5f));head=tail;
                }
            }
            if(Event.current.type==EventType.Repaint&&shopModelCamera!=null)shopModelCamera.Render();
            if(shopModelTarget!=null)GUI.DrawTexture(new Rect(0,0,1280,720),shopModelTarget,ScaleMode.StretchToFill,true);
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var p=shop.Bench[i];var r=ShopBenchRect(i);
                if(p.Star>1)DrawShopRankBadge(new Rect(r.xMax-36,650,34,13),p.Star,false);
                else DrawShopStars(new Rect(r.xMax-20,650,13,12),1,new Color(.75f,.89f,1));
                GUI.Label(new Rect(r.x,697,r.width,17),shop.Definition(p.Id).Name,new GUIStyle(center){fontSize=10,normal={textColor=new Color(.69f,.8f,.84f)}});
            }
        }
        void DrawShopPromotionLight(Vector2 foot,int star,float remaining)
        {
            float age=1-remaining,strength=Mathf.Sin(Mathf.Clamp01(age/.12f)*Mathf.PI*.5f)*remaining;
            Color old=GUI.color;GUI.color=ShopRankColor(star,strength*.9f);
            GUI.DrawTexture(new Rect(foot.x-55,foot.y-100,110,122),shopAura,ScaleMode.StretchToFill,true);
            if(star==3)
            {
                GUI.color=new Color(1,.82f,.3f,strength*.75f);GUI.DrawTexture(new Rect(foot.x-22,foot.y-152,44,172),shopAura,ScaleMode.StretchToFill,true);
                GUI.color=new Color(1,.94f,.6f,strength*.85f);GUI.DrawTexture(new Rect(foot.x-8,foot.y-130,16,140),shopAura,ScaleMode.StretchToFill,true);
                GUI.DrawTexture(new Rect(foot.x-55,foot.y-36,110,18),shopAura,ScaleMode.StretchToFill,true);
                for(int ray=0;ray<9;ray++)
                {
                    float direction=(ray-4)*.22f;Vector2 start=foot+new Vector2(direction*9,-15),end=foot+new Vector2(direction*(25+age*45),-55-age*60-Mathf.Abs(ray-4)*4);
                    ShopLine(start,end,new Color(1,.84f,.36f,strength*.32f),ray==4?4:2);
                    Vector2 dust=Vector2.Lerp(start,end,Mathf.Repeat(age*1.4f+ray*.117f,1));
                    Fill(new Rect(dust.x,dust.y,2,4),new Color(1,.92f,.63f,strength*.85f));
                }
            }
            GUI.color=old;
            float width=30+age*90;ShopBorder(new Rect(foot.x-width/2,foot.y-6-age*5,width,8+age*10),ShopRankColor(star,strength*.65f),1);
        }
    }
}
