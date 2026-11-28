using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
namespace YesterdayConvoy
{
    // Both controllers use these original approved A render functions and the same animated model/flow stage.
    // This presentation never creates a battle or owns transactions.
    public sealed class ApprovedShopPresentation : IDisposable
    {
        public sealed class Host
        {
            public Transform Parent;
            public Func<ShopTrialModel> Shop;
            public Func<Font> Font;
            public Func<bool> Preparing,Loading,Visible,Dragging,OddsVisible,FlowActive,CanCollapse,ReserveOnly,ReserveOverflow;
            public Func<int> DragInstance;
            public Func<int> LineupSeatCount;
            public Func<Vector2> Pointer;
            public Func<int,Rect> PieceRect,FlowPieceRect,OfferRect;
            public Func<Rect> MarketRect;
            public Func<Rect> ReserveDeck;
            public Func<int,Rect> ReserveSlot;
            public Func<int,bool> ReserveSelection;
            public Func<int,Color> ReserveFee;
            public Func<int,bool> InLineup;
            public Func<string,Sprite> Portrait;
            public Func<string,double,Sprite> IdleFrame;
            public Func<string,float> ModelHeight;
            public Func<string,string[]> Tags;
            public Func<Texture2D> Coin,Star;
            public Action<bool> StoreVisible,SetVisible,StoreOdds;
            public Action<int,Vector2> Purchase;
            public Action Refresh,BuyXP,ToggleLock,OpenOdds,CloseOverlays,DrawDropTarget;
            public string LineupHint;
        }
        readonly Host host;
        public ApprovedShopPresentation(Host source){host=source??throw new ArgumentNullException(nameof(source));}
        ShopTrialModel shop=>host.Shop();
        bool ShopTrialActive=>shop!=null;
        bool ShopTradingAvailable=>host.Preparing()&&shopVisible&&!ShopPanelTransitioning;
        Transform transform=>host.Parent;
        bool shopVisible {get=>host.Visible();set=>host.StoreVisible(value);}
        bool shopOddsOpen {get=>host.OddsVisible();set=>host.StoreOdds(value);}
        bool shopMoving=>host.Dragging();
        int shopDrag=>host.DragInstance();
        Vector2 shopPointer=>host.Pointer();
        Texture2D shopCoin=>host.Coin();
        Texture2D shopStar=>host.Star();
        int shopPromotedInstance=-1;
        float shopPromotionAt=-100;
        public int PromotedInstance=>shopPromotedInstance;
        public float PromotionAt=>shopPromotionAt;
        public Camera ModelCamera=>shopModelCamera;
        public RenderTexture ModelTarget=>shopModelTarget;
        public GameObject ModelStage=>shopModelStage;
        Rect ShopPanelRect=>host.MarketRect?.Invoke()??ApprovedUi.ShopPanel;
        public static Rect OddsPanelRect=>new Rect(306,105,670,498);
        static Rect ShopOddsPanelRect=>OddsPanelRect;
        Rect ShopOfferRect(int slot)=>host.OfferRect?.Invoke(slot)??ApprovedUi.Offer(slot);
        static Rect ShopResourceHudRect=>new Rect(12,581,164,124);
        Rect ShopPieceRect(int slot)=>host.PieceRect(slot);
        Rect ShopFlowPieceRect(int slot)=>host.FlowPieceRect?.Invoke(slot)??ShopPieceRect(slot);
        string[] ShopTags(string id)=>host.Tags(id);
        int ShopHeldCopies(string id)=>shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star));
        float ShopRecruitPulse=>.15f+.85f*(.5f-.5f*Mathf.Cos(Time.unscaledTime*Mathf.PI*2/1.5f));
        static void Fill(Rect rect,Color color)=>ApprovedUi.Fill(rect,color);
        static void ShopBorder(Rect rect,Color color,float width=1)=>ApprovedUi.Border(rect,color,width);
        static void Destroy(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}

        const float ShopArrivalDuration=.63f;
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
            internal Vector2 Origin;
            internal bool BoundToFlow,Merge;
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
        public void AnimateShopVisibility(bool visible)
        {
            if(shopVisible==visible)return;
            shopVisibilityFrom=ShopPanelVisibility;shopVisible=visible;shopVisibilityAt=Time.unscaledTime;
            shopVisibilityDuration=(visible?.24f:.18f)*Mathf.Abs((visible?1:0)-shopVisibilityFrom);
        }
        public void ResetShopPresentation()
        {
            ClearShopBenchModels();shopVisibilityFrom=shopVisible?1:0;shopVisibilityAt=-100;shopVisibilityDuration=0;
        }
        public void EnsureShopModelStage()
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
        Sprite ShopIdleFrame(string id,double time)=>host.IdleFrame(id,time);
        float ShopModelScale(string id)
        {
            float height=host.ModelHeight?.Invoke(id)??LocalMonsterArt.For(id)?.Height??1;
            return .57f/1.855f*Mathf.Min(1,2.05f/height);
        }
        Vector2 ShopModelFoot(int slot){var r=ShopFlowPieceRect(slot);return new Vector2(r.center.x,r.y+55);}
        static Vector3 ShopModelPosition(Vector2 gui)=>new Vector3(gui.x*.01f,(720-gui.y)*.01f,0);
        public void SyncShopBenchModels()
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
        public void BeginShopArrival(ShopTrialModel.Result result)
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
        public void Tick(){if(!host.Loading()&&ShopTrialActive){PruneShopFlows();if(shopModelStage==null)SyncShopBenchModels();else UpdateShopBenchModels();}}
        void UpdateShopBenchModels()
        {
            for(int i=shopArrivals.Count-1;i>=0;i--)
                if(Time.unscaledTime-shopArrivals[i].At>=ShopArrivalDuration||!shop.Bench.Any(p=>p.Instance==shopArrivals[i].Instance)||shopMoving&&shopDrag==shopArrivals[i].Instance)
                {shopArrivals[i].Model.gameObject.SetActive(false);Destroy(shopArrivals[i].Model.gameObject);shopArrivals.RemoveAt(i);}
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];if(!shopBenchModels.TryGetValue(piece.Instance,out var model))continue;
                model.sprite=ShopIdleFrame(piece.Id,Time.unscaledTime-shopBenchIdleOrigins[piece.Instance]);
                var bounds=ShopPieceRect(i);model.enabled=bounds.width>0&&bounds.height>0;
                float burst=ShopFlowPromotionRemaining(piece.Instance),scale=ShopModelScale(piece.Id)*(1+.08f*Mathf.Sin((1-burst)*Mathf.PI)*burst);
                model.transform.localScale=Vector3.one*scale;model.transform.localPosition=ShopModelPosition(ShopModelFoot(i));model.sortingOrder=10;
                if(shopMoving&&shopDrag==piece.Instance){model.transform.localPosition=ShopModelPosition(shopPointer+new Vector2(0,-8));model.transform.localScale*=1.15f;model.sortingOrder=110;}
                float alpha=shopArrivals.Any(a=>a.Instance==piece.Instance&&!a.Merge)?0:1;
                model.color=piece.Star>=3&&burst>0?new Color(1,1,.65f+.35f*(1-burst),alpha):new Color(1,1,1,alpha);
            }
            foreach(var arrival in shopArrivals)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==arrival.Instance);float t=Mathf.Clamp01((Time.unscaledTime-arrival.At)/ShopArrivalDuration);
                var target=slot<0?Rect.zero:ShopFlowPieceRect(slot);
                arrival.Model.enabled=target.width>0&&target.height>0;
                if(!arrival.Model.enabled)continue;
                arrival.Model.sprite=ShopIdleFrame(arrival.Id,Time.unscaledTime-shopBenchIdleOrigins[arrival.Instance]);arrival.Model.sortingOrder=100;
                var destination=ShopModelFoot(slot);
                arrival.Model.transform.localPosition=ShopModelPosition(arrival.BoundToFlow?ShopFlowPoint(arrival.Origin,destination,ShopFlowEase(Mathf.Clamp01((Time.unscaledTime-arrival.At-.055f)/.56f))):ShopEntranceFoot(destination,t));
                arrival.Model.transform.localScale=Vector3.one*ShopModelScale(arrival.Id)*(arrival.BoundToFlow?.54f:1);
                float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.14f));
                arrival.Model.color=new Color(1,1,1,arrival.Merge?0:reveal*.65f);
            }
        }
        public void ClearShopBenchModels()
        {
            host.CloseOverlays?.Invoke();
            if(shopModelStage!=null){shopModelStage.SetActive(false);Destroy(shopModelStage);shopModelStage=null;}
            if(shopModelTarget!=null){shopModelTarget.Release();Destroy(shopModelTarget);shopModelTarget=null;}
            shopModelCamera=null;shopBenchModels.Clear();shopBenchIdleOrigins.Clear();shopArrivals.Clear();ClearShopFlows();
        }
        public void DrawShopBench(GUIStyle center)
        {
            int reserve=shop.Bench.Count(p=>!host.InLineup(p.Instance));
            var title=ApprovedUi.Text(host.Font(),13,TextAnchor.MiddleLeft,Color.white,600);
            var hint=ApprovedUi.Text(host.Font(),10,TextAnchor.MiddleRight,new Color(.87f,.92f,.95f));
            bool singleReserve=host.ReserveOnly?.Invoke()??false;
            if(singleReserve)
            {
                bool overflow=host.ReserveOverflow?.Invoke()??false;
                var deck=host.ReserveDeck?.Invoke()??new Rect(184,627,950,73);
                Fill(deck,overflow?FormationPreparationPalette.Rust:FormationPreparationPalette.Recess);
                ShopBorder(deck,overflow?FormationPreparationPalette.RustEdge:FormationPreparationPalette.Edge);
                for(int slot=0;slot<10;slot++)
                {
                    var pad=host.ReserveSlot?.Invoke(slot)??ApprovedUi.ReserveSlot(slot);
                    Fill(pad,overflow?Color.Lerp(FormationPreparationPalette.Surface,FormationPreparationPalette.Rust,.45f):FormationPreparationPalette.Surface);
                    ShopBorder(pad,overflow?FormationPreparationPalette.RustEdge:FormationPreparationPalette.Line);
                }
                string reserveText=$"备战席 {Mathf.Min(reserve, ShopSeatLayout.ReserveCapacity)}/{ShopSeatLayout.ReserveCapacity}";
                GUI.Label(new Rect(deck.x+7,deck.y+1,135,15),reserveText,LoopText(10,TextAnchor.MiddleLeft,FormationPreparationPalette.Text,600));
                GUI.Label(new Rect(deck.xMax-139,deck.y+1,132,15),$"已上阵 {shop.Bench.Count(p=>host.InLineup(p.Instance))}/{shop.PopulationCap}",
                    LoopText(10,TextAnchor.MiddleRight,FormationPreparationPalette.Muted,600));
            }
            else
            {
                // Preserve the original two-row presentation for legacy callers.
                DrawShopWalkway(ApprovedUi.LineupSlot(0,host.LineupSeatCount?.Invoke()??10),shop.PopulationCap,true);
                DrawShopWalkway(ApprovedUi.ReserveSlot(0),10,false);
                Fill(new Rect(188,483,152,21),new Color(.22f,.32f,.40f,.85f));GUI.Label(new Rect(193,483,145,21),$"出战编队  {shop.Bench.Count(p=>host.InLineup(p.Instance))}/{shop.PopulationCap}",title);
                Fill(new Rect(188,594,152,21),new Color(.22f,.32f,.40f,.85f));GUI.Label(new Rect(193,594,145,21),$"备战席  {reserve}/10",title);
                GUI.Label(new Rect(650,493,478,21),host.LineupHint??"自由拖放位置 · 重叠交换 · 开战自动向左收拢空位",hint);
                GUI.Label(new Rect(650,604,478,21),"可跨排拖放交换 · 同名限编队一张 · 两侧出售",hint);
            }
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var p=shop.Bench[i];var r=ShopPieceRect(i);if(r.width<=0||r.height<=0)continue;
                int cost=shop.Definition(p.Id).Cost;
                if(singleReserve)
                {
                    Color tier=host.ReserveFee?.Invoke(cost)??ApprovedUi.Tier(cost);
                    Fill(r,Color.Lerp(FormationPreparationPalette.Surface,tier,.57f));
                    ShopBorder(r,Color.Lerp(FormationPreparationPalette.Line,tier,.70f));
                }
                else DrawLoopCostBackground(r,cost);
                Fill(new Rect(r.x+15,r.y+51,r.width-30,5),new Color(.04f,.09f,.13f,.31f));
            }
            if(Event.current.type==EventType.Repaint&&shopModelCamera!=null)shopModelCamera.Render();
            if(shopModelTarget!=null)GUI.DrawTexture(new Rect(0,0,1280,720),shopModelTarget,ScaleMode.StretchToFill,true);
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var p=shop.Bench[i];var r=ShopPieceRect(i);if(r.width<=0||r.height<=0)continue;
                int cost=shop.Definition(p.Id).Cost;Fill(new Rect(r.x+3,r.y+2,15,15),new Color(.12f,.23f,.32f,.9f));
                GUI.Label(new Rect(r.x+3,r.y+1,15,16),cost.ToString(),ApprovedUi.Text(host.Font(),12,TextAnchor.MiddleCenter,Color.white,700));
                int star=ShopDisplayedStar(p);DrawShopStars(new Rect(r.xMax-8-star*12,r.y+2,star*12,12),star,new Color(1,.94f,.72f));
                var footer=new Rect(r.x,r.y+55,r.width,singleReserve?14:16);
                Color fee=singleReserve?(host.ReserveFee?.Invoke(cost)??ApprovedUi.Tier(cost)):ApprovedUi.Tier(cost);
                Fill(footer,Color.Lerp(fee,Color.black,singleReserve?.31f:.30f));
                footer.x+=1;footer.width-=2;
                string name=shop.Definition(p.Id).Name;
                GUI.Label(footer,name,ApprovedUi.Text(host.Font(),name.Length>8&&singleReserve?10:11,TextAnchor.MiddleCenter,Color.white,600));
                if(singleReserve&&host.ReserveSelection?.Invoke(p.Instance)==true)ShopBorder(r,new Color32(167,217,255,255),1.5f);
            }
            host.DrawDropTarget?.Invoke();
        }
        public void DrawShopWalkway(Rect first,int unlocked,bool lineup)
        {
            var deck=new Rect(first.x-4,first.y,950,73);Fill(deck,new Color(.51f,.59f,.64f));
            Fill(new Rect(deck.x,deck.y,deck.width,3),new Color(.69f,.77f,.81f));
            Fill(new Rect(deck.x,deck.yMax,deck.width,5),new Color(.25f,.35f,.43f));
            Fill(new Rect(deck.x,deck.yMax+5,deck.width,2),new Color(.13f,.23f,.30f));
            int count=lineup?host.LineupSeatCount?.Invoke()??10:10;
            for(int i=0;i<count;i++)
            {
                var r=lineup?ApprovedUi.LineupSlot(i,count):ApprovedUi.ReserveSlot(i);
                Fill(r,new Color(.29f,.39f,.46f,lineup&&i>=unlocked?.55f:.2f));
                Fill(new Rect(r.xMax-1,r.y,1,r.height),new Color(.75f,.83f,.87f,.25f));
                if(lineup&&i>=unlocked)GUI.Label(r,"·",ApprovedUi.Text(host.Font(),22,TextAnchor.MiddleCenter,new Color(.77f,.84f,.88f,.5f)));
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
        const float ShopFlowLaunch=.055f,ShopFlowStagger=.042f,ShopFlowFlight=.470f;
        const float ShopFlowGather=.610f,ShopFlowReveal=.650f,ShopFlowFinish=1.350f,ShopFlowChain=.690f;
        public sealed class ShopFlowSnapshot
        {
            internal readonly List<ShopTrialModel.Piece> Pieces=new List<ShopTrialModel.Piece>();
            internal readonly Dictionary<int,Rect> Bounds=new Dictionary<int,Rect>();
        }
        sealed class ShopFlowMaterial { internal int Instance; internal Rect Bounds; internal bool Existing; }
        sealed class ShopFlowStep
        {
            internal int Star;
            internal Rect Anchor;
            internal readonly List<ShopFlowMaterial> Materials=new List<ShopFlowMaterial>();
        }
        sealed class ShopFlow
        {
            internal string Id;
            internal int Instance,OldStar,Star;
            internal float At,Reveal,Duration;
            internal Vector2 Origin;
            internal readonly List<ShopFlowStep> Steps=new List<ShopFlowStep>();
            internal bool Merge=>Steps.Count>0;
        }
        readonly List<ShopFlow> shopFlows=new List<ShopFlow>();
        public bool HasUnrevealedFlow(Func<string,bool> matchesCard)
            =>shopFlows.Any(flow=>matchesCard(flow.Id)&&Time.unscaledTime-flow.At<flow.Reveal);

        public ShopFlowSnapshot CaptureShopFlowSnapshot()
        {
            var snapshot=new ShopFlowSnapshot();if(shop==null)return snapshot;
            for(int i=0;i<shop.Bench.Count;i++)
            {
                var piece=shop.Bench[i];snapshot.Pieces.Add(piece.Copy());
                snapshot.Bounds[piece.Instance]=ShopFlowPieceRect(i);
            }
            return snapshot;
        }
        public void BeginShopFlow(ShopFlowSnapshot snapshot,ShopTrialModel.Result result,Vector2 origin)
        {
            if(snapshot==null||result==null||!result.Success||shop==null)return;
            PruneShopFlows();EnsureShopAura();
            var flow=new ShopFlow{Id=result.Id,Instance=result.Instance,Star=result.FinalStar,At=Time.unscaledTime,
                Origin=origin,OldStar=snapshot.Pieces.FirstOrDefault(p=>p.Instance==result.Instance)?.Star??1};
            // This repeats the model's read-only grouping order only to locate consumed visual sources.
            // The incoming placeholder is never a real card ID and never reaches game state.
            const int incoming=int.MaxValue;
            var planned=snapshot.Pieces.Select(p=>p.Copy()).ToList();
            planned.Add(new ShopTrialModel.Piece{Id=result.Id,Instance=incoming,Star=1});
            for(int tier=1;tier<shop.MaxStar(result.Id);tier++)
            {
                var group=planned.Where(p=>p.Id==result.Id&&p.Star==tier).OrderByDescending(p=>shop.IsLineupInstance(p.Instance)).ThenBy(p=>p.Instance).Take(3).ToArray();
                if(group.Length!=3)continue;
                var step=new ShopFlowStep{Star=tier+1,Anchor=snapshot.Bounds.TryGetValue(group[0].Instance,out var anchor)?anchor:new Rect(origin,Vector2.one)};
                foreach(var material in group)
                {
                    bool existing=snapshot.Bounds.TryGetValue(material.Instance,out var bounds);
                    step.Materials.Add(new ShopFlowMaterial{Instance=material.Instance,Bounds=existing?bounds:new Rect(origin,Vector2.one),Existing=existing});
                }
                flow.Steps.Add(step);group[0].Star++;planned.Remove(group[1]);planned.Remove(group[2]);
            }
            if(flow.Merge)
            {
                var consumed=new HashSet<int>(flow.Steps.SelectMany(s=>s.Materials).Select(m=>m.Instance));
                shopFlows.RemoveAll(old=>consumed.Contains(old.Instance));
                flow.Reveal=ShopFlowReveal+(flow.Steps.Count-1)*ShopFlowChain;
                flow.Duration=ShopFlowFinish+(flow.Steps.Count-1)*ShopFlowChain;
                shopPromotedInstance=result.Instance;shopPromotionAt=flow.At+flow.Reveal;
            }
            else {flow.Reveal=.630f;flow.Duration=.950f;}
            shopFlows.Add(flow);
            var arrival=shopArrivals.LastOrDefault(a=>a.Instance==result.Instance);
            if(arrival!=null){arrival.At=flow.At;arrival.Origin=origin;arrival.BoundToFlow=true;arrival.Merge=flow.Merge;}
            UpdateShopBenchModels();
        }
        public void ClearShopFlows(){shopFlows.Clear();shopPromotedInstance=-1;shopPromotionAt=-100;}
        public void PruneShopFlows()
        {
            if(!ShopTrialActive||!(host.FlowActive?.Invoke()??host.Preparing()))
            {ClearShopFlows();return;}
            float now=Time.unscaledTime;
            shopFlows.RemoveAll(f=>now-f.At>f.Duration||!shop.Bench.Any(p=>p.Instance==f.Instance)||shopMoving&&shopDrag==f.Instance);
        }
        public float ShopFlowPromotionRemaining(int instance)
        {
            if(shop==null)return 0;
            float now=Time.unscaledTime;
            for(int i=shopFlows.Count-1;i>=0;i--)
            {
                var flow=shopFlows[i];float age=now-flow.At;
                if(flow.Instance==instance&&flow.Merge&&age>=flow.Reveal&&age<flow.Duration)
                    return 1-Mathf.Clamp01((age-flow.Reveal)/(flow.Duration-flow.Reveal));
            }
            return 0;
        }
        public int ShopDisplayedStar(ShopTrialModel.Piece piece)
        {
            for(int i=shopFlows.Count-1;i>=0;i--)
                if(shopFlows[i].Instance==piece.Instance&&shopFlows[i].Merge&&Time.unscaledTime-shopFlows[i].At<shopFlows[i].Reveal)
                    return shopFlows[i].OldStar;
            return piece.Star;
        }
        static Vector2 ShopFlowFoot(Rect rect)=>new Vector2(rect.center.x,rect.y+55);
        static float ShopFlowEase(float t)=>t<.5f?2*t*t:1-Mathf.Pow(-2*t+2,2)/2;
        static Vector2 ShopFlowPoint(Vector2 a,Vector2 b,float t)
        {
            Vector2 delta=b-a,first=a+new Vector2(delta.x*.14f,delta.y*.5f),second=b-new Vector2(delta.x*.2f,delta.y*.2f);
            float u=1-t;return u*u*u*a+3*u*u*t*first+3*u*t*t*second+t*t*t*b;
        }
        public void DrawShopFlows()
        {
            PruneShopFlows();if(Event.current.type!=EventType.Repaint||shopFlows.Count==0)return;
            float now=Time.unscaledTime;
            foreach(var flow in shopFlows)
            {
                int slot=shop.Bench.ToList().FindIndex(p=>p.Instance==flow.Instance);if(slot<0)continue;
                var bounds=ShopFlowPieceRect(slot);if(bounds.width<=0||bounds.height<=0)continue;
                Vector2 end=ShopFlowFoot(bounds);float elapsed=now-flow.At;
                if(!flow.Merge)
                {
                    float progress=(elapsed-.055f)/.560f;
                    if(progress>=0&&progress<=1)DrawShopFlowTrace(flow.Origin,end,progress,new Color(.90f,.80f,.57f));
                    DrawShopFlowBurst(end,(elapsed-.615f)/.300f,new Color(.91f,.81f,.59f),1);
                    continue;
                }
                for(int index=0;index<flow.Steps.Count;index++)
                {
                    var step=flow.Steps[index];float local=elapsed-index*ShopFlowChain;
                    bool final=index==flow.Steps.Count-1;
                    Vector2 anchor=final?end:ShopFlowFoot(step.Anchor);Color color=ShopRankColor(step.Star);
                    for(int n=0;n<step.Materials.Count;n++)
                    {
                        var material=step.Materials[n];Vector2 start=material.Existing?ShopFlowFoot(material.Bounds):flow.Origin;
                        float progress=(local-ShopFlowLaunch-n*ShopFlowStagger)/ShopFlowFlight;
                        if(progress>=0&&progress<=1&&Vector2.Distance(start,anchor)>8)
                        {
                            Vector2 head=DrawShopFlowTrace(start,anchor,progress,color);
                            DrawShopFlowPortrait(flow.Id,head+new Vector2(0,-6),24,.48f);
                        }
                        if(local>0&&local<.190f&&material.Existing)DrawShopFlowPortrait(flow.Id,start+new Vector2(0,-16),55,(1-local/.190f)*.7f);
                    }
                    DrawShopFlowBurst(anchor,(local-ShopFlowGather)/.300f,color,step.Star);
                    if(!final&&local>.620f&&local<.890f)
                    {
                        DrawShopFlowPortrait(flow.Id,anchor+new Vector2(0,-22),53,Mathf.Min(1,(.890f-local)/.150f));
                        GUI.Label(new Rect(anchor.x-32,anchor.y-59,64,19),new string('★',step.Star),ApprovedUi.Text(host.Font(),13,TextAnchor.MiddleCenter,color,600));
                    }
                }
            }
        }
        void DrawShopFlowPortrait(string id,Vector2 center,float size,float alpha)
        {
            var portrait=host.Portrait(id);if(portrait==null)return;
            Color old=GUI.color;GUI.color=old*new Color(1,1,1,Mathf.Clamp01(alpha));
            DrawTestPortrait(new Rect(center.x-size/2,center.y-size/2,size,size),portrait);GUI.color=old;
        }
        Vector2 DrawShopFlowTrace(Vector2 start,Vector2 end,float progress,Color color)
        {
            float p=Mathf.Clamp01(progress);
            for(int strand=0;strand<3;strand++)
            {
                float offset=(strand-1)*2.67f,lag=strand*.012f;Vector2 previous=Vector2.zero;
                for(int segment=0;segment<=24;segment++)
                {
                    float q=Mathf.Clamp01(p-lag-.17f+segment*.17f/24);
                    Vector2 point=ShopFlowPoint(start,end,ShopFlowEase(q))+new Vector2(1,.5f)*Mathf.Sin(q*Mathf.PI)*offset;
                    if(segment>0)ShopLine(previous,point,new Color(color.r,color.g,color.b,strand==1?.72f:.35f),strand==1?2.7f:.9f);
                    previous=point;
                }
            }
            Vector2 head=ShopFlowPoint(start,end,ShopFlowEase(p));Color old=GUI.color;
            GUI.color=new Color(color.r,color.g,color.b,.55f);GUI.DrawTexture(new Rect(head.x-9,head.y-9,18,18),shopAura);
            GUI.color=new Color(1,.97f,.85f,.95f);GUI.DrawTexture(new Rect(head.x-3,head.y-3,6,6),shopAura);GUI.color=old;
            return head;
        }
        void DrawShopFlowBurst(Vector2 point,float progress,Color color,int star)
        {
            if(progress<0||progress>1)return;
            float fade=Mathf.Sin(progress*Mathf.PI),radius=20+31*progress;Color old=GUI.color;
            GUI.color=new Color(color.r,color.g,color.b,fade*.6f);GUI.DrawTexture(new Rect(point.x-radius,point.y-radius,radius*2,radius*2),shopAura);
            if(star>1){GUI.color=new Color(color.r,color.g,color.b,fade*.5f);GUI.DrawTexture(new Rect(point.x-16,point.y-113,32,120),shopAura);}
            GUI.color=old;
            float width=17+16*progress,height=3+3*progress;
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;
                ShopLine(point+new Vector2(Mathf.Cos(a)*width,Mathf.Sin(a)*height+5),point+new Vector2(Mathf.Cos(b)*width,Mathf.Sin(b)*height+5),new Color(color.r,color.g,color.b,fade*.65f),1);
            }
            for(int i=0;i<(star>1?12:6);i++)
            {
                Vector2 spark=point+new Vector2(Mathf.Sin(i*2.4f)*37*progress,-12-(10+i*5.3f)*progress);
                ShopLine(spark,spark+new Vector2(Mathf.Cos(i)*2,-4),new Color(color.r,color.g,color.b,fade*.8f),.8f);
            }
        }
        Texture2D shopAura;
        static Color ShopRankColor(int star,float alpha=1)=>star>=3?new Color(1,.78f,.28f,alpha):new Color(.81f,.87f,.96f,alpha);
        public void EnsureShopAura()
        {
            if(shopAura!=null)return;
            shopAura=new Texture2D(96,96,TextureFormat.RGBA32,false){name="Shop rank aura",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color[96*96];
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            {
                float radius=new Vector2((x-47.5f)/47.5f,(y-47.5f)/47.5f).magnitude;
                float alpha=Mathf.Pow(Mathf.Clamp01(1-radius),1.8f);
                pixels[y*96+x]=new Color(1,1,1,alpha);
            }
            shopAura.SetPixels(pixels);shopAura.Apply();
        }
        public void DrawShopRankPortrait(Rect rect,Sprite portrait,int star,float burst=0)
        {
            if(star>1)
            {
                float time=Time.unscaledTime,pulse=.5f+.5f*Mathf.Sin(time*3.4f);
                Color previous=GUI.color;GUI.color=ShopRankColor(star,.32f+.12f*pulse+.32f*burst);
                GUI.DrawTexture(new Rect(rect.x-8,rect.y-7,rect.width+16,rect.height+14),shopAura,ScaleMode.StretchToFill,true);
                GUI.color=ShopRankColor(star,.18f+.1f*pulse+.12f*burst);
                float spread=rect.width>70?2:1.2f;
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;Rect outline=rect;outline.position+=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*spread;DrawTestPortrait(outline,portrait);
                }
                GUI.color=previous;
                if(burst>0)
                {
                    Vector2 center=rect.center;
                    float radius=1+(1-burst)*.45f;
                    Rect ring=new Rect(center.x-rect.width*.52f*radius,center.y-rect.height*.48f*radius,rect.width*1.04f*radius,rect.height*.96f*radius);
                    ShopBorder(ring,ShopRankColor(star,burst*.7f),1);
                }
            }
            DrawTestPortrait(rect,portrait);
        }
        public void DrawShopRankBadge(Rect rect,int star,bool animate=true)
        {
            float wave=animate?.5f+.5f*Mathf.Sin(Time.unscaledTime*(star==3?4.6f:4f)):1;
            Fill(rect,new Color(.025f,.045f,.055f,.82f));
            ShopBorder(rect,ShopRankColor(star,.25f+.3f*wave),1);
            if(animate)
            {
                Color previous=GUI.color;GUI.color=ShopRankColor(star,.2f+.25f*wave);
                GUI.DrawTexture(new Rect(rect.x-4,rect.y-8,rect.width+8,rect.height+16),shopAura,ScaleMode.StretchToFill,true);GUI.color=previous;
            }
            DrawShopStars(new Rect(rect.x+3,rect.y+2,rect.width-6,rect.height-4),star,ShopRankColor(star,.5f+.5f*wave));
        }
        public void DrawApprovedShopMarket(bool prep)
        {
            if(shop==null)return;
            if(!shopVisible&&!ShopPanelTransitioning)
            {
                if(host.ReserveOnly?.Invoke()??false)return; // Formation uses the persistent coin entry only.
                bool enabled=GUI.enabled;GUI.enabled=enabled&&host.Preparing();
                if(LoopButton(new Rect(1144,69,118,30),"打开商店 Tab"))host.SetVisible(true);
                GUI.enabled=enabled;return;
            }
            bool originalEnabled=GUI.enabled;
            float reveal=ShopPanelVisibility,offset=-(1-reveal)*ShopPanelRect.height;
            Rect panel=ShopPanelRect;
            GUI.BeginGroup(panel);
            try
            {
                Fill(new Rect(0,offset,panel.width,30),ApprovedUi.Paper);
                GUI.Label(new Rect(9,offset+3,102,24),"招募商店",LoopText(15,TextAnchor.MiddleLeft,null,700));
                GUI.Label(new Rect(105,offset+4,220,22),"五格商品 / 点击购买",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                if(LoopButton(new Rect(680,offset+4,271,22),"查看等级1–10招募概率"))host.OpenOdds();
                if(host.CanCollapse?.Invoke()??true)
                {
                    if(LoopButton(new Rect(980,offset+4,86,22),"收起商店"))host.SetVisible(false);
                }
                else GUI.Label(new Rect(968,offset+4,98,22),"整备期间常驻",LoopText(10,TextAnchor.MiddleCenter,ApprovedUi.Muted));

                Rect refresh=new Rect(956,offset+36,118,32),lockRect=new Rect(956,offset+74,118,32);
                GUI.enabled=originalEnabled&&prep&&ShopTradingAvailable;
                if(LoopButton(refresh,shop.UnlimitedGold?"免费刷新":shop.FreeRefreshes>0?"免费刷新 "+shop.FreeRefreshes:"刷新 "+shop.RefreshPrice+"金币",true))
                {host.Refresh();}
                if(LoopButton(lockRect,shop.Locked?"已锁定 · 解锁":"锁定商店")){host.ToggleLock();}
                GUI.enabled=originalEnabled;
                for(int i=0;i<5;i++)
                {
                    Rect global=ShopOfferRect(i);global.y+=offset;
                    var local=global;local.position-=panel.position;
                    DrawApprovedShopOffer(i,local,global,originalEnabled&&prep&&ShopTradingAvailable);
                }
            }
            finally{GUI.enabled=originalEnabled;GUI.EndGroup();}
        }
        public void DrawApprovedShopOffer(int slot,Rect rect,Rect global,bool trading)
        {
            string id=shop.Offers[slot];
            if(id==null)
            {
                Fill(rect,new Color(.52f,.60f,.65f,.50f));ShopBorder(rect,new Color(.78f,.84f,.88f,.55f),1);
                GUI.Label(rect,"暂无商品",LoopText(12,TextAnchor.MiddleCenter,new Color(.83f,.89f,.93f)));return;
            }
            var role=shop.Definition(id);int copies=ShopHeldCopies(id);bool affordable=shop.CanAfford(shop.Price(id));
            var preview=shop.PreviewPurchase(slot);int upgrade=preview.Success&&preview.FinalStar>1?preview.FinalStar:0;
            DrawLoopCostBackground(rect,role.Cost);
            DrawTestPortrait(new Rect(rect.x+rect.width*.34f,rect.y+5,rect.width*.64f,108),host.Portrait(id));
            Fill(new Rect(rect.x,rect.y+73,rect.width,47),new Color(.09f,.15f,.20f,.91f));
            if(copies>0)
            {
                Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),new Color(.88f,.95f,1,.055f));
                ShopBorder(rect,new Color(.87f,.94f,1,.55f),1.5f);
            }
            if(upgrade>0&&affordable)
            {
                float pulse=ShopRecruitPulse;
                Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),new Color(1,.93f,.64f,.08f*pulse));
                ShopBorder(rect,new Color(1,.91f,.59f,.94f*pulse),2);
            }
            if(shopCoin!=null)GUI.DrawTexture(new Rect(rect.x+11,rect.y+12,13,13),shopCoin,ScaleMode.ScaleToFit,true);
            if(!affordable)
            {
                // The transparent purchase button cannot dim artwork already drawn behind it.
                Fill(rect,new Color(.16f,.16f,.16f,.76f));ShopBorder(rect,new Color(.40f,.40f,.40f),1);
            }
            Color ink=affordable?Color.white:new Color(.76f,.76f,.76f);
            GUI.Label(new Rect(rect.x+29,rect.y+5,36,26),shop.Price(id).ToString(),LoopText(23,TextAnchor.MiddleLeft,ink,700));
            GUI.Label(new Rect(rect.x+10,rect.y+75,rect.width-20,22),role.Name,LoopText(role.Name.Length>10?12:14,TextAnchor.MiddleLeft,ink,600));
            var tags=LoopText(9,TextAnchor.UpperLeft,affordable?new Color(.89f,.93f,.96f):new Color(.62f,.62f,.62f));tags.wordWrap=true;
            GUI.Label(new Rect(rect.x+10,rect.y+98,rect.width-20,22),string.Join(" / ",ShopTags(id)),tags);
            if(!affordable)
            {
                var reason=new Rect(rect.xMax-65,rect.y+6,59,17);Fill(reason,new Color(.29f,.29f,.29f,.97f));
                GUI.Label(reason,"金币不足",LoopText(10,TextAnchor.MiddleCenter,new Color(.85f,.85f,.85f),500));
            }
            else if(upgrade>0)DrawApprovedUpgradeCue(new Rect(rect.xMax-65,rect.y+6,59,17),upgrade);
            else if(copies>0)
            {
                var owned=new Rect(rect.xMax-45,rect.y+6,39,17);Fill(owned,new Color(.90f,.95f,.98f,.93f));
                GUI.Label(owned,"持有 "+copies,LoopText(9,TextAnchor.MiddleCenter,ApprovedUi.Ink,500));
            }
            bool enabled=GUI.enabled;GUI.enabled=enabled&&trading&&affordable;
            if(GUI.Button(rect,new GUIContent(string.Empty,role.Name+" · "+shop.Price(id)+"金币 · 持有"+copies+"/"+shop.Capacity(id)),GUIStyle.none))
            {host.Purchase(slot,global.center);}
            GUI.enabled=enabled;
        }
        public void DrawApprovedUpgradeCue(Rect rect,int star)
        {
            Fill(rect,new Color(.94f,.83f,.50f,.97f));
            GUI.Label(new Rect(rect.x+4,rect.y,29,rect.height),"可合成",LoopText(9,TextAnchor.MiddleCenter,new Color(.36f,.28f,.14f),600));
            DrawShopStars(new Rect(rect.x+33,rect.y+2,rect.width-36,rect.height-4),star,new Color(.45f,.32f,.11f));
        }
        public void DrawShopResourceHud(bool preparing)
        {
            var r=ShopResourceHudRect;Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            Fill(new Rect(r.x,r.y,3,r.height),ApprovedUi.Gold);
            if(shopCoin!=null)GUI.DrawTexture(new Rect(r.x+12,r.y+13,18,18),shopCoin,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(r.x+37,r.y+6,43,29),"金币",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            string gold=shop.UnlimitedGold?"∞":shop.Gold.ToString();
            GUI.Label(new Rect(r.x+76,r.y+4,77,32),gold,LoopText(gold.Length>4?19:27,TextAnchor.MiddleRight,new Color(.45f,.34f,.16f),700));
            Fill(new Rect(r.x+12,r.y+42,r.width-24,1),ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+12,r.y+48,64,22),"等级 "+shop.Level,LoopText(15,TextAnchor.MiddleLeft,null,600));
            GUI.Label(new Rect(r.x+72,r.y+50,80,18),shop.Level==10?"已达上限":$"{shop.XP}/{shop.NextLevelXP} 经验",LoopText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
            Fill(new Rect(r.x+12,r.y+76,r.width-24,4),new Color(.65f,.72f,.77f));
            Fill(new Rect(r.x+12,r.y+76,(r.width-24)*(shop.Level==10?1:Mathf.Clamp01((float)shop.XP/shop.NextLevelXP)),4),new Color(.29f,.48f,.63f));
            bool enabled=GUI.enabled;GUI.enabled=enabled&&preparing&&!ShopPanelTransitioning;
            if(LoopButton(new Rect(r.x+12,r.y+89,r.width-24,25),"买经验 +"+shop.PurchasedXP+" · 4金币"))
            {host.BuyXP();}
            GUI.enabled=enabled;
        }
        public void DrawShopOdds(Vector2 point)
        {
            if(!shopOddsOpen||shop==null)return;
            var e=Event.current;var r=ShopOddsPanelRect;
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){shopOddsOpen=false;e.Use();return;}
            if(e.type==EventType.MouseDown&&e.button==0&&!r.Contains(point)){shopOddsOpen=false;e.Use();return;}
            var paper=ApprovedUi.Paper;paper.a=1;Fill(r,paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+22,r.y+15,r.width-85,29),"招募概率",LoopText(23,TextAnchor.MiddleLeft,null,700));
            if(LoopButton(new Rect(r.xMax-54,r.y+15,32,28),"×")){shopOddsOpen=false;return;}
            GUI.Label(new Rect(r.x+22,r.y+49,r.width-44,23),"等级决定费用档；同费用内按剩余牌数加权抽取",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            const float rowHeight=28;float top=r.y+84;float col=(r.width-143)/5;
            GUI.Label(new Rect(r.x+24,top,85,24),"队伍等级",LoopText(11,TextAnchor.MiddleCenter,ApprovedUi.Muted));
            for(int cost=1;cost<=5;cost++)GUI.Label(new Rect(r.x+119+(cost-1)*col,top,col,24),cost+"费",LoopText(12,TextAnchor.MiddleCenter,ApprovedUi.Tier(cost),600));
            for(int level=1;level<=10;level++)
            {
                float y=top+26+(level-1)*rowHeight;bool current=level==shop.Level;
                if(current){Fill(new Rect(r.x+20,y,r.width-40,rowHeight),new Color(.75f,.82f,.85f));Fill(new Rect(r.x+20,y,3,rowHeight),ApprovedUi.Gold);}
                var ink=current?ApprovedUi.Ink:ApprovedUi.Muted;var weights=ShopProbabilityUi.Baseline(level);
                GUI.Label(new Rect(r.x+24,y,85,rowHeight),level+(current?"  当前":""),LoopText(12,TextAnchor.MiddleCenter,ink,current?700:400));
                for(int cost=0;cost<5;cost++)GUI.Label(new Rect(r.x+119+cost*col,y,col,rowHeight),weights[cost]+"%",LoopText(13,TextAnchor.MiddleCenter,ink,current?600:400));
            }
            float footer=top+26+10*rowHeight+13;var actual=ShopProbabilityUi.FirstDraw(shop,shop.Level);
            GUI.Label(new Rect(r.x+24,footer,r.width-48,21),"按当前库存返还商品后，首抽费用档概率： "+string.Join("  /  ",actual.Select(v=>v.ToString("0.#")+"%")),LoopText(11,TextAnchor.MiddleLeft,null,600));
            var text=LoopText(11,TextAnchor.UpperLeft,ApprovedUi.Muted);text.wordWrap=true;
            GUI.Label(new Rect(r.x+24,footer+27,r.width-48,51),"上表是基础权重。已解锁费用档完全缺货时剔除该档，其余有库存档按原权重归一；每抽一张库存都会变化。五格商品占库，刷新先返库；库存不足五张时按实际数量展示。",text);
        }
        public GUIStyle LoopText(int size=15,TextAnchor alignment=TextAnchor.MiddleLeft,Color? color=null,int weight=400)
            =>ApprovedUi.Text(host.Font(),size,alignment,color??ApprovedUi.Ink,weight);
        public bool LoopButton(Rect rect,string text,bool primary=false)
        {
            var style=LoopText(14,TextAnchor.MiddleCenter,primary?new Color(.26f,.23f,.16f):ApprovedUi.Ink,600);
            style.padding=new RectOffset(5,5,0,0);style.border=new RectOffset();
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused})
            {state.textColor=primary?new Color(.26f,.23f,.16f):ApprovedUi.Ink;state.background=Texture2D.whiteTexture;}
            Color old=GUI.backgroundColor;GUI.backgroundColor=primary?ApprovedUi.Gold:new Color(.82f,.87f,.90f);
            bool clicked=GUI.Button(rect,text,style);GUI.backgroundColor=old;
            ApprovedUi.Border(rect,new Color(.51f,.61f,.69f,GUI.enabled?.55f:.2f));
            return clicked;
        }
        public void DrawLoopCostBackground(Rect rect,int recruitCost,float opacity=1f)
        {
            Color tier=ApprovedUi.Tier(recruitCost),top=Color.Lerp(tier,Color.white,.30f);
            tier.a=top.a=opacity;
            for(int row=0;row<16;row++)Fill(new Rect(rect.x,rect.y+row*rect.height/16,rect.width,rect.height/16+1),Color.Lerp(top,tier,row/15f));
            var shadow=Color.Lerp(tier,Color.black,.28f);shadow.a=opacity;
            var edge=Color.Lerp(tier,Color.white,.45f);edge.a=opacity;
            Fill(new Rect(rect.x,rect.yMax-5,rect.width,5),shadow);
            ShopBorder(rect,edge,1);
        }
        static void ShopLine(Vector2 a,Vector2 b,Color color,float width=2)
        {
            Matrix4x4 previous=GUI.matrix;GUI.matrix=previous*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width/2,Vector2.Distance(a,b),width),color);GUI.matrix=previous;
        }
        public static Texture2D CreateShopStar()
        {
            var points=new Vector2[10];for(int i=0;i<10;i++){float angle=(90+i*36)*Mathf.Deg2Rad,r=i%2==0?29:13;points[i]=new Vector2(32+Mathf.Cos(angle)*r,32+Mathf.Sin(angle)*r);}
            var pixels=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                int coverage=0;for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++)
                {
                    float px=x+(sx+.5f)/2,py=y+(sy+.5f)/2;bool inside=false;
                    for(int i=0,j=9;i<10;j=i++)if((points[i].y>py)!=(points[j].y>py)&&px<(points[j].x-points[i].x)*(py-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
                    if(inside)coverage++;
                }
                pixels[y*64+x]=new Color(1,1,1,coverage/4f);
            }
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Shop promotion star",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};texture.SetPixels(pixels);texture.Apply();return texture;
        }
        public void DrawShopStars(Rect bounds,int count,Color color)
        {
            Color previous=GUI.color;GUI.color=color;float size=Mathf.Min(bounds.height,bounds.width/count),start=bounds.center.x-count*size/2;
            for(int i=0;i<count;i++)GUI.DrawTexture(new Rect(start+i*size,bounds.y,size,size),shopStar,ScaleMode.ScaleToFit,true);GUI.color=previous;
        }
        public static void DrawTestPortrait(Rect bounds,Sprite sprite)
        {
            if(sprite==null) return;
            Rect source=sprite.textureRect; float ratio=Mathf.Min(bounds.width/source.width,bounds.height/source.height);
            Rect target=new Rect(bounds.center.x-source.width*ratio/2,bounds.center.y-source.height*ratio/2,source.width*ratio,source.height*ratio);
            GUI.DrawTextureWithTexCoords(target,sprite.texture,new Rect(source.x/sprite.texture.width,source.y/sprite.texture.height,source.width/sprite.texture.width,source.height/sprite.texture.height));
        }
        public static void DrawLoopShield(Vector2 origin,Color color)
        {
            Vector2[] vertices={new Vector2(0,0),new Vector2(15,0),new Vector2(15,12),new Vector2(7.5f,19),new Vector2(0,12),new Vector2(0,0)};
            for(int i=0;i<vertices.Length-1;i++)ShopLine(origin+vertices[i],origin+vertices[i+1],color,1.5f);
            ShopLine(origin+new Vector2(5,4),origin+new Vector2(10,4),color,1.5f);
            ShopLine(origin+new Vector2(7.5f,4),origin+new Vector2(7.5f,12),color,1.5f);
        }
        public void Dispose(){ClearShopBenchModels();if(shopAura!=null){Destroy(shopAura);shopAura=null;}}
    }
}
