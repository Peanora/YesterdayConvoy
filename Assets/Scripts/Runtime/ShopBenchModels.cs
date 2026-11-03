using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        const float ShopArrivalDuration=.72f;
        readonly Dictionary<int,SpriteRenderer> shopBenchModels=new Dictionary<int,SpriteRenderer>();
        readonly Dictionary<int,float> shopBenchIdleOrigins=new Dictionary<int,float>();
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
        public SpriteRenderer ShopEntranceModel(int instance)=>shopArrivals.LastOrDefault(a=>a.Instance==instance)?.Model;
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
        Sprite ShopIdleFrame(string id,double time)
        {
            if(id==talulahDefinition.Card.CardId)return OriginalTalulahArt.For(talulahDefinition.Card)?["idle"].At(time,true,out _)??talulahDefinition.Card.BattleSprite;
            var local=LocalMonsterArt.For(id);if(local!=null)return local["idle"].At(time,true,out _);
            var slug=OriginalSlugArt.For(id);return slug==null?null:slug["idle"].At(time,true,out _);
        }
        static float ShopModelScale(string id)
        {
            float height=LocalMonsterArt.For(id)?.Height??1;
            return .50f/1.855f*Mathf.Min(1,2.05f/height);
        }
        static Vector2 ShopModelFoot(int slot)=>new Vector2(ShopBenchRect(slot).center.x,693);
        static Vector3 ShopModelPosition(Vector2 gui)=>new Vector3(gui.x*.01f,(720-gui.y)*.01f,0);
        void SyncShopBenchModels()
        {
            if(!ShopTrialActive)return;
            EnsureShopModelStage();
            foreach(int instance in shopBenchModels.Keys.Where(id=>!shop.Bench.Any(p=>p.Instance==id)).ToArray())
            {
                shopBenchModels[instance].gameObject.SetActive(false);Destroy(shopBenchModels[instance].gameObject);shopBenchModels.Remove(instance);shopBenchIdleOrigins.Remove(instance);
            }
            foreach(var piece in shop.Bench)if(!shopBenchModels.ContainsKey(piece.Instance))
            {shopBenchModels.Add(piece.Instance,NewShopModel("Bench "+piece.Id+" #"+piece.Instance));shopBenchIdleOrigins[piece.Instance]=Time.unscaledTime-piece.Instance*.13f;}
            UpdateShopBenchModels();
        }
        void BeginShopArrival(ShopTrialModel.Result result)
        {
            for(int i=shopArrivals.Count-1;i>=0;i--)if(shopArrivals[i].Instance==result.Instance)
            {shopArrivals[i].Model.gameObject.SetActive(false);Destroy(shopArrivals[i].Model.gameObject);shopArrivals.RemoveAt(i);}
            shopBenchIdleOrigins[result.Instance]=Time.unscaledTime+ShopArrivalDuration*.6f;
            shopArrivals.Add(new ShopArrival{Instance=result.Instance,Id=result.Id,At=Time.unscaledTime,Model=NewShopModel("Entering "+result.Id)});
            UpdateShopBenchModels();
        }
        static Vector2 ShopEntranceFoot(Vector2 destination,float progress)
        {
            float drop=Mathf.Clamp01((progress-.12f)/.48f);
            return destination+new Vector2(0,-78*(1-drop*drop));
        }
        void LateUpdate(){if(ShopTrialActive){if(shopModelStage==null)SyncShopBenchModels();else UpdateShopBenchModels();}}
        void UpdateShopBenchModels()
        {
            for(int i=shopArrivals.Count-1;i>=0;i--)
                if(Time.unscaledTime-shopArrivals[i].At>=ShopArrivalDuration||!shop.Bench.Any(p=>p.Instance==shopArrivals[i].Instance)||shopMoving&&shopDrag==shopArrivals[i].Instance)
                {shopArrivals[i].Model.gameObject.SetActive(false);Destroy(shopArrivals[i].Model.gameObject);shopArrivals.RemoveAt(i);}
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];if(!shopBenchModels.TryGetValue(piece.Instance,out var model))continue;
                model.sprite=ShopIdleFrame(piece.Id,Time.unscaledTime-shopBenchIdleOrigins[piece.Instance]);
                float burst=ShopBenchPromotionRemaining(piece.Instance),scale=ShopModelScale(piece.Id)*(1+.12f*Mathf.Sin((1-burst)*Mathf.PI)*burst);
                model.transform.localScale=Vector3.one*scale;model.transform.localPosition=ShopModelPosition(ShopModelFoot(i));model.sortingOrder=10;
                if(shopMoving&&shopDrag==piece.Instance){model.transform.localPosition=ShopModelPosition(shopPointer+new Vector2(0,-8));model.transform.localScale*=1.15f;model.sortingOrder=110;}
                float alpha=shopArrivals.Any(a=>a.Instance==piece.Instance)?0:1;
                model.color=piece.Star==3&&burst>0?new Color(1,1,.65f+.35f*(1-burst),alpha):new Color(1,1,1,alpha);
            }
            foreach(var arrival in shopArrivals)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==arrival.Instance);float t=Mathf.Clamp01((Time.unscaledTime-arrival.At)/ShopArrivalDuration);
                arrival.Model.sprite=ShopIdleFrame(arrival.Id,Time.unscaledTime-shopBenchIdleOrigins[arrival.Instance]);arrival.Model.sortingOrder=100;
                arrival.Model.transform.localPosition=ShopModelPosition(ShopEntranceFoot(ShopModelFoot(slot),t));
                float settle=Mathf.Sin(Mathf.Clamp01((t-.6f)/.4f)*Mathf.PI);
                arrival.Model.transform.localScale=new Vector3(1+.12f*settle,1-.18f*settle,1)*ShopModelScale(arrival.Id);
                float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.14f));
                arrival.Model.color=new Color(1,1,1,reveal);
            }
        }
        void ClearShopBenchModels()
        {
            if(shopModelStage!=null){shopModelStage.SetActive(false);Destroy(shopModelStage);shopModelStage=null;}
            if(shopModelTarget!=null){shopModelTarget.Release();Destroy(shopModelTarget);shopModelTarget=null;}
            shopModelCamera=null;shopBenchModels.Clear();shopBenchIdleOrigins.Clear();shopArrivals.Clear();
        }
        void DrawShopBench(GUIStyle center)
        {
            Fill(new Rect(0,632,1280,88),new Color(.035f,.06f,.075f,.98f));
            Fill(new Rect(8,632,1264,1),new Color(.3f,.44f,.48f));
            // A single raised walkway, as seen in the user's recording; seat hit areas stay invisible.
            Fill(new Rect(14,656,1252,48),new Color(.018f,.03f,.035f));
            for(int row=0;row<37;row++)
            {
                float inset=12-row*.21f;
                Fill(new Rect(inset,652+row,1280-2*inset,1),Color.Lerp(new Color(.27f,.34f,.36f),new Color(.36f,.42f,.43f),row/36f));
            }
            Fill(new Rect(12,651,1256,2),new Color(.51f,.58f,.59f));
            foreach(int y in new[]{660,673,686}){Fill(new Rect(13,y,1254,1),new Color(.2f,.27f,.29f));Fill(new Rect(13,y+1,1254,1),new Color(.4f,.47f,.48f));}
            for(int row=0;row<10;row++)Fill(new Rect(5+row*.6f,689+row,1270-row*1.2f,1),Color.Lerp(new Color(.27f,.33f,.35f),new Color(.1f,.15f,.17f),row/9f));
            Fill(new Rect(10,699,1260,2),new Color(.035f,.06f,.075f));
            foreach(int x in new[]{26,410,826,1252}){Fill(new Rect(x,656,3,2),new Color(.17f,.23f,.25f));Fill(new Rect(x,682,3,2),new Color(.17f,.23f,.25f));}
            for(int i=0;i<10;i++)
            {
                var r=ShopBenchRect(i);var piece=i<shop.Bench.Count?shop.Bench[i]:null;
                bool active=piece!=null&&shopLineup.Contains(piece.Id)&&shop.Bench.Where(p=>p.Id==piece.Id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First().Instance==piece.Instance;
                Color color=piece!=null&&piece.Star>1?ShopRankColor(piece.Star,.65f):new Color(.24f,.39f,.44f);
                if(piece==null)continue;
                Fill(new Rect(r.center.x-30,694,60,2),active?new Color(.45f,.88f,.92f):color);
                float burst=ShopBenchPromotionRemaining(piece.Instance);
                if(burst>0)DrawShopPromotionLight(ShopModelFoot(i),piece.Star,burst);
            }
            foreach(var arrival in shopArrivals)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==arrival.Instance);if(slot<0)continue;
                float t=Mathf.Clamp01((Time.unscaledTime-arrival.At)/ShopArrivalDuration);
                DrawShopEntranceGate(ShopModelFoot(slot),t);
            }
            if(Event.current.type==EventType.Repaint&&shopModelCamera!=null)shopModelCamera.Render();
            if(shopModelTarget!=null)GUI.DrawTexture(new Rect(0,0,1280,720),shopModelTarget,ScaleMode.StretchToFill,true);
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var p=shop.Bench[i];var r=ShopBenchRect(i);
                if(p.Star>1)DrawShopRankBadge(new Rect(r.xMax-36,650,34,13),p.Star,false);
                else DrawShopStars(new Rect(r.xMax-20,650,13,12),1,new Color(.75f,.89f,1));
                GUI.Label(new Rect(r.x,697,r.width,17),shop.Definition(p.Id).Name,new GUIStyle(center){fontSize=10,normal={textColor=new Color(.69f,.8f,.84f)}});
                if(Battle.StrongholdChallenge)
                {
                    var card=Battle.FindCard(p.Id);string state=card!=null?$"{card.Settings.DeploymentCost} DP · {card.RemainingUses}次":shopLineup.Contains(p.Id)&&p.Id==Battle.Settings.CardId?$"{Battle.Settings.DeploymentCost} DP · {Battle.RemainingUses}次":"未编队";
                    if(card!=null&&Battle.CardCooldown(p.Id)>0)state=$"冷却 {Battle.CardCooldown(p.Id):0.0}秒";
                    GUI.Label(new Rect(r.x,633,r.width,16),state,new GUIStyle(center){fontSize=10,normal={textColor=new Color(.75f,.88f,.94f)}});
                }
            }
        }
        void DrawShopEntranceGate(Vector2 foot,float progress)
        {
            float fall=Mathf.Sin(Mathf.Clamp01(progress/.6f)*Mathf.PI),age=Mathf.Clamp01((progress-.56f)/.44f),impact=Mathf.Sin(age*Mathf.PI);
            Color previous=GUI.color;GUI.color=new Color(.85f,.96f,1,fall*.3f);
            GUI.DrawTexture(new Rect(foot.x-18,foot.y-142,36,145),shopAura,ScaleMode.StretchToFill,true);
            float height=foot.y-ShopEntranceFoot(foot,progress).y;
            for(int i=0;i<3;i++)Fill(new Rect(foot.x-20+i*19,foot.y-height-72+i*8,1,36-i*4),new Color(.9f,.98f,1,fall*.55f));
            GUI.color=new Color(.35f,1,.87f,impact*.9f);GUI.DrawTexture(new Rect(foot.x-50,foot.y-16,100,30),shopAura,ScaleMode.StretchToFill,true);GUI.color=previous;
            float radius=20+age*34;
            for(int segment=0;segment<24;segment++)
            {
                float a=segment*Mathf.PI*2/24,b=(segment+1)*Mathf.PI*2/24;
                ShopLine(foot+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.19f),foot+new Vector2(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius*.19f),new Color(.7f,1,.94f,impact*.8f),1);
            }
            for(int i=0;i<5;i++)
            {
                Vector2 spark=foot+new Vector2((i-2)*19*age,-Mathf.Sin(age*Mathf.PI)*(17+Mathf.Abs(i-2)*10));
                ShopLine(spark,spark+new Vector2((i-2)*2,-7),new Color(.8f,1,.96f,impact),2);
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
