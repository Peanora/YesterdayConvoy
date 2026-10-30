using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        ShopTrialModel shop;
        readonly Dictionary<string,ManualBattleDefinition> shopDefinitions=new Dictionary<string,ManualBattleDefinition>();
        readonly List<string> shopLineup=new List<string>();
        readonly List<ManualBattleDefinition> shopDeck=new List<ManualBattleDefinition>();
        DefenderTrialDefinition shopMapCopy;
        Rect shopPreviousViewport;
        float shopPreviousFieldOfView;
        Vector3 shopMapCameraPosition;
        int shopDrag=-1,shopPending=-1,shopPromotionStar;
        string shopPromotionId,shopMessage;
        Vector2 shopDragStart,shopPointer;
        float shopPromotionAt=-100,shopMessageUntil,shopRefreshAt=-100,shopUpgradeAt=-100;
        bool shopMoving,shopVisible;
        Texture2D shopCoin,shopStar,shopFade;
        public bool ShopTrialActive=>shop!=null;
        public ShopTrialModel Shop=>shop;
        public string ShopFeedback=>shopMessage;
        public bool ShopVisible=>shopVisible;
        public static Rect ShopBattlefieldRect=>new Rect(0,64,1280,538);
        public static Rect ShopPanelRect=>new Rect(0,64,1280,199);
        public static Rect ShopSellRect=>new Rect(16,688,1248,30);
        public static Rect ShopOfferRect(int i)=>new Rect(18+i*219,78,207,165);
        public static Rect ShopBenchRect(int i)=>new Rect(16+i*125,656,116,46);
        public bool ShopMapPointAvailable(Vector2 point)=>ShopBattlefieldRect.Contains(point)&&(!shopVisible||!ShopPanelRect.Contains(point));
        public void SetShopVisible(bool visible)
        {
            if(shop==null)return;
            CancelShopDrag();shopVisible=visible;
        }

        public void EnterShopTrial(int preset=0)
        {
            if(shop==null){shopPreviousViewport=Camera.main.rect;shopPreviousFieldOfView=Camera.main.fieldOfView;}
            shopDefinitions.Clear();
            string[] supported={"CARD_M02","CARD_M03","CARD_M05","CARD_M08",OriginalSlugArt.CardId,"CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19"};
            foreach(var option in monsterTests)
                if(option.Playable&&supported.Contains(option.Id))shopDefinitions[option.Id]=option.Definition;
            if(!shopDefinitions.ContainsKey("CARD_M08"))throw new InvalidOperationException("M08 required for shop map trial");
            shop=new ShopTrialModel(shopDefinitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}));
            var pieces=new List<ShopTrialModel.Piece>();
            if(preset==2){pieces.Add(new ShopTrialModel.Piece{Id="CARD_M08",Star=2});pieces.Add(new ShopTrialModel.Piece{Id="CARD_M08",Star=2});}
            pieces.Add(new ShopTrialModel.Piece{Id="CARD_M08",Star=1});pieces.Add(new ShopTrialModel.Piece{Id="CARD_M08",Star=1});
            if(preset>0)
                foreach(string id in shopDefinitions.Keys.Where(id=>id!="CARD_M08").OrderBy(id=>id).Take(10-pieces.Count))pieces.Add(new ShopTrialModel.Piece{Id=id,Star=1});
            string[] offerIds={"CARD_M08","CARD_M02",OriginalSlugArt.CardId,"CARD_M18","CARD_M05"};
            shop.Preset(pieces,offerIds.Where(shopDefinitions.ContainsKey),preset==2?7:5,preset==2?46:18);
            shopLineup.Clear();shopDeck.Clear();shopLineup.Add("CARD_M08");
            shopDrag=-1;shopMoving=false;shopPending=-1;shopPromotionAt=-100;shopVisible=preset>0;
            if(shopMapCopy!=null)Destroy(shopMapCopy);
            shopMapCopy=Instantiate(Resources.Load<DefenderTrialDefinition>("MapTrials/city"));
            shopMapCopy.mapTitle="泵站回廊 · 商店试玩";
            if(shopCoin==null)
            {
                shopCoin=new Texture2D(2,2,TextureFormat.RGBA32,false);
                shopCoin.LoadImage(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath,"ShopTrial/originium.png")));
            }
            if(shopStar==null)shopStar=CreateShopStar();
            if(shopFade==null)
            {
                shopFade=new Texture2D(64,1,TextureFormat.RGBA32,false){name="Shop rarity fade",wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[64];for(int i=0;i<64;i++)pixels[i]=new Color(1,1,1,.72f*Mathf.Clamp01(1-i/50f));shopFade.SetPixels(pixels);shopFade.Apply();
            }
            EnterDefense(shopMapCopy);
            shopMapCameraPosition=Camera.main.transform.position;
            SyncShopDeck();
            ShopNotice(preset==0?"Tab 打开商店；备战棋子拖到红门部署，向下拖出售":"满席测试：购买第一张弩手，确认后合成并腾出空位");
        }
        void SyncShopDeck(bool autoFill=true)
        {
            if(shop==null||Battle.Phase!=BattlePhase.Ready)return;
            shopLineup.RemoveAll(id=>!shop.Bench.Any(p=>p.Id==id));
            if(autoFill)foreach(var piece in shop.Bench)
                if(shopLineup.Count<shop.Level&&!shopLineup.Contains(piece.Id))shopLineup.Add(piece.Id);
            shopDeck.Clear();foreach(string id in shopLineup)shopDeck.Add(shopDefinitions[id]);
            var effective=shopDeck.Count>0?shopDeck:new List<ManualBattleDefinition>{shopDefinitions["CARD_M08"]};
            defenderTrial.battle=effective[0];defenderTrial.rosterCards=effective.Skip(1).ToArray();definition=effective[0];
            ClearViews();Battle=CreateBattle();selectedRosterIndex=0;Selected=false;CancelInteraction();EnableCombatView();
            Camera.main.rect=new Rect(0,118/720f,1,538/720f);
            FrameShopMap();
        }
        IEnumerable<Vector3> ShopMapPlayablePoints()
        {
            foreach(var lane in shopMapCopy.lanes)foreach(var point in lane.points){yield return point;yield return point+Vector3.up*.65f;}
            foreach(var defender in shopMapCopy.defenders){yield return defender.position;yield return defender.position+Camera.main.transform.up*1.65f;}
        }
        void FrameShopMap()
        {
            var camera=Camera.main;camera.transform.position=shopMapCameraPosition;camera.fieldOfView=shopPreviousFieldOfView;
            var points=ShopMapPlayablePoints().ToArray();
            // Recenter the playable board, leaving decorative canals outside the framing budget.
            for(int pass=0;pass<2;pass++)
            {
                var projected=points.Select(camera.WorldToViewportPoint).ToArray();
                float centerX=(projected.Min(p=>p.x)+projected.Max(p=>p.x))*.5f,centerY=(projected.Min(p=>p.y)+projected.Max(p=>p.y))*.5f;
                float depth=projected.Average(p=>p.z);
                camera.transform.position+=camera.ViewportToWorldPoint(new Vector3(centerX,centerY,depth))-camera.ViewportToWorldPoint(new Vector3(.5f,.5f,depth));
            }
            float extent=points.Select(camera.WorldToViewportPoint).Max(p=>Mathf.Max(Mathf.Abs(p.x-.5f)/.47f,Mathf.Abs(p.y-.5f)/.45f));
            float zoom=Mathf.Min(1.4f,1/extent);
            camera.fieldOfView=2*Mathf.Atan(Mathf.Tan(shopPreviousFieldOfView*.5f*Mathf.Deg2Rad)/zoom)*Mathf.Rad2Deg;
        }
        public ShopTrialModel.Result PurchaseShopCard(int slot,bool confirm=false)
        {
            if(shop==null||Battle.Phase!=BattlePhase.Ready)return new ShopTrialModel.Result{Message="战斗开始后不能招募或升星"};
            var result=shop.Purchase(slot,confirm);
            if(result.NeedsConfirmation){shopPending=slot;return result;}
            if(result.Success)
            {
                shopPending=-1;
                if(shopLineup.Count<shop.Level&&!shopLineup.Contains(result.Id))shopLineup.Add(result.Id);
                SyncShopDeck(false);
                if(result.FinalStar>1){shopPromotionAt=Time.unscaledTime;shopPromotionId=result.Id;shopPromotionStar=result.FinalStar;}
            }
            ShopNotice(result.Message);return result;
        }
        public ShopTrialModel.Result SellShopCard(int instance)
        {
            if(shop==null||Battle.Phase!=BattlePhase.Ready)return new ShopTrialModel.Result{Message="战斗中不能出售"};
            var r=shop.Sell(instance);if(r.Success)SyncShopDeck(false);ShopNotice(r.Success?$"出售 {r.Message} · +{r.Value} 源石":r.Message);return r;
        }
        void ShopNotice(string message){shopMessage=message;shopMessageUntil=Time.unscaledTime+3;}
        public void CancelShopDrag(){shopDrag=-1;shopMoving=false;shopPending=-1;CancelInteraction();}
        public void LeaveShopTrial()
        {
            CancelShopDrag();shop=null;shopLineup.Clear();shopDeck.Clear();Camera.main.rect=shopPreviousViewport;Camera.main.fieldOfView=shopPreviousFieldOfView;
            ReturnToMonsterTests();if(shopMapCopy!=null){Destroy(shopMapCopy);shopMapCopy=null;}
        }
        bool ShopPointerEvent(EventType type,Vector2 point,int button=0)
        {
            shopPointer=point;
            if(shopPending>=0)return false;
            if(type==EventType.MouseDown&&button==0)
            {
                for(int i=0;i<shop.Bench.Count;i++)if(ShopBenchRect(i).Contains(point)){shopDrag=shop.Bench[i].Instance;shopDragStart=point;shopMoving=false;return true;}
                if(ShopMapPointAvailable(point))ProcessPointerDown(ScreenPosition(point));
            }
            if(type==EventType.MouseDrag&&shopDrag>=0){shopMoving|=(point-shopDragStart).magnitude>8;if(shopMoving)shopVisible=false;return true;}
            if(type!=EventType.MouseUp||button!=0||shopDrag<0)return false;
            int instance=shopDrag;bool moved=shopMoving;shopDrag=-1;shopMoving=false;
            var piece=shop.Bench.FirstOrDefault(p=>p.Instance==instance);if(piece==null)return true;
            if(moved&&ShopSellRect.Contains(point)&&point.y>shopDragStart.y+12){SellShopCard(instance);return true;}
            if(moved&&ShopMapPointAvailable(point))
            {
                int index=shopLineup.IndexOf(piece.Id);
                if(index<0){ShopNotice("编队位已满：点击备战棋子调整编队，或先升级");return true;}
                selectedRosterIndex=index;Selected=true;
                if(PointerGround(ScreenPosition(point),out var world)){var result=TryDeployAt(world,true);ShopNotice(result.Success?"部署成功 · 战斗开始后商店锁定":Feedback);}
                Selected=false;return true;
            }
            if(!moved&&Battle.Phase==BattlePhase.Ready)
            {
                if(shopLineup.Contains(piece.Id))shopLineup.Remove(piece.Id);
                else if(shopLineup.Count<shop.Level)shopLineup.Add(piece.Id);
                else {ShopNotice("编队位已满，先取消其他棋子或升级");return true;}
                SyncShopDeck(false);
            }
            return true;
        }
        void DrawShopTrial()
        {
            EnsureStyles();var e=Event.current;Vector2 point=new Vector2(e.mousePosition.x*1280/Screen.width,e.mousePosition.y*720/Screen.height);
            if(ShopPointerEvent(e.type,point,e.button))e.Use();
            Matrix4x4 previous=GUI.matrix;Color old=GUI.color,background=GUI.backgroundColor;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));GUI.backgroundColor=new Color(.16f,.27f,.32f);
            var label=new GUIStyle(textStyle){fontSize=17,alignment=TextAnchor.MiddleLeft};
            var title=new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold};
            var center=new GUIStyle(label){alignment=TextAnchor.MiddleCenter};
            var shopButton=new GUIStyle(buttonStyle);
            foreach(var state in new[]{shopButton.normal,shopButton.hover,shopButton.active,shopButton.focused})state.textColor=new Color(.88f,.95f,.98f);
            bool prep=Battle.Phase==BattlePhase.Ready&&shopPending<0;
            DrawShopDefenderBars();
            foreach(var unit in Battle.Units)
            {
                if(!unit.Active)continue;Vector2 health=GUIPosition(Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*UnitVisualHeight(unit)));
                Fill(new Rect(health.x-26,health.y,52,5),new Color(.03f,.05f,.06f));Fill(new Rect(health.x-25,health.y+1,50*unit.Health/unit.MaxHealth,3),new Color(.4f,.9f,.55f));
            }
            Fill(new Rect(0,0,1280,64),new Color(.055f,.09f,.11f,.97f));
            Fill(new Rect(22,65,1236,1),new Color(.22f,.31f,.36f));
            GUI.DrawTexture(new Rect(25,25,22,22),shopCoin,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(54,17,58,38),"源石",label);
            GUI.Label(new Rect(105,12,170,44),shop.Gold.ToString(),new GUIStyle(title){fontSize=29,normal={textColor=new Color(1,.86f,.57f)}});
            GUI.Label(new Rect(650,17,55,38),"等级",label);GUI.Label(new Rect(702,12,50,44),shop.Level.ToString(),new GUIStyle(title){fontSize=29});
            Fill(new Rect(751,33,118,5),new Color(.20f,.27f,.30f));Fill(new Rect(751,33,118*(shop.Level==10?1:(float)shop.XP/ShopTrialModel.Experience[shop.Level]),5),new Color(.27f,.78f,.84f));
            GUI.Label(new Rect(878,18,65,34),shop.Level==10?"MAX":$"{shop.XP}/{ShopTrialModel.Experience[shop.Level]}",new GUIStyle(center){fontSize=12,normal={textColor=new Color(.72f,.85f,.91f)}});
            GUI.enabled=prep;
            GUI.backgroundColor=new Color(.07f,.18f,.22f);
            if(GUI.Button(new Rect(958,17,132,37),"升级     4",shopButton)){var r=shop.Upgrade();if(r.Success){shopUpgradeAt=Time.unscaledTime;SyncShopDeck();}ShopNotice(r.Message);}
            ShopBorder(new Rect(958,17,132,37),new Color(.33f,.66f,.73f),1);GUI.DrawTexture(new Rect(1037,26,18,18),shopCoin,ScaleMode.ScaleToFit,true);
            GUI.enabled=shopPending<0;
            if(GUI.Button(new Rect(1125,17,132,37),new GUIContent(shopVisible?"收起商店":"打开商店","Tab 切换商店"),shopButton))SetShopVisible(!shopVisible);
            if(shopVisible)
            {
            Fill(ShopPanelRect,new Color(.055f,.09f,.11f,.96f));ShopBorder(new Rect(8,65,1264,191),new Color(.32f,.41f,.45f),1);
            GUI.enabled=prep;
            var refreshRect=new Rect(1125,78,132,101);GUI.backgroundColor=new Color(.72f,.36f,.07f);
            if(GUI.Button(refreshRect,GUIContent.none,shopButton)){var r=shop.Refresh();if(r.Success)shopRefreshAt=Time.unscaledTime;ShopNotice(r.Message);}
            ShopBorder(refreshRect,new Color(.91f,.65f,.32f),1);DrawShopRefreshIcon(new Vector2(1157,124),new Color(1,.90f,.75f));
            GUI.Label(new Rect(1183,96,73,43),"刷新",new GUIStyle(title){fontSize=24,normal={textColor=new Color(1,.91f,.77f)}});GUI.DrawTexture(new Rect(1194,141,15,15),shopCoin,ScaleMode.ScaleToFit,true);GUI.Label(new Rect(1214,133,25,29),"2",new GUIStyle(label){fontSize=13});
            GUI.backgroundColor=shop.Locked?new Color(.23f,.19f,.12f):new Color(.08f,.14f,.18f);
            if(GUI.Button(new Rect(1125,194,132,49),new GUIContent("",shop.Locked?"已锁定 · 点击解锁":"锁定商店"),shopButton))shop.Locked=!shop.Locked;
            Color lockColor=shop.Locked?new Color(1,.83f,.42f):new Color(.68f,.79f,.84f);ShopBorder(new Rect(1125,194,132,49),shop.Locked?new Color(.60f,.48f,.27f):new Color(.27f,.38f,.43f),1);
            ShopBorder(new Rect(1181,216,21,15),lockColor,2);ShopBorder(new Rect(shop.Locked?1185:1190,206,13,12),lockColor,2);Fill(new Rect(1190,221,3,6),lockColor);
            GUI.backgroundColor=new Color(.16f,.27f,.32f);GUI.enabled=shopPending<0;
            for(int i=0;i<5;i++)
            {
                Rect r=ShopOfferRect(i);Fill(r,new Color(.047f,.09f,.11f));ShopBorder(r,new Color(.28f,.38f,.42f),1);string id=shop.Offers[i];if(id==null)continue;
                var role=shop.Definition(id);float enter=Mathf.Clamp01((Time.unscaledTime-shopRefreshAt-i*.045f)/.25f);r.y+=(1-enter)*14;
                Color tint=GUI.color;GUI.color=ShopTier(role.Cost);GUI.DrawTexture(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),shopFade);GUI.color=tint;
                DrawTestPortrait(new Rect(r.x+5,r.y+11,118,127),OriginalTalulahArt.CardPortrait(shopDefinitions[id].Card));
                GUI.Label(new Rect(r.x+112,r.y+11,94,27),role.Name,new GUIStyle(label){fontSize=role.Name.Length>6?11:14,fontStyle=FontStyle.Bold,clipping=TextClipping.Clip});
                string[] tags=ShopTags(id);for(int t=0;t<tags.Length;t++){Fill(new Rect(r.x+113,r.y+46+t*17,1,12),new Color(.46f,.60f,.65f));GUI.Label(new Rect(r.x+118,r.y+40+t*17,88,23),tags[t],new GUIStyle(label){fontSize=11,normal={textColor=new Color(.76f,.88f,.92f)}});}
                GUI.DrawTexture(new Rect(r.x+13,r.yMax-31,18,18),shopCoin,ScaleMode.ScaleToFit,true);GUI.Label(new Rect(r.x+38,r.yMax-36,45,28),role.Cost.ToString(),label);
                bool merge=shop.Bench.Count(p=>p.Id==id&&p.Star==1)>=2;
                if(merge){ShopBorder(r,new Color(.86f,.78f,.51f),1);DrawShopStars(new Rect(r.xMax-27,r.yMax-32,18,18),1,new Color(1,.87f,.57f));}
                GUI.enabled=prep&&shop.Gold>=role.Cost&&shopPending<0;
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))PurchaseShopCard(i);GUI.enabled=shopPending<0;
            }
            }
            GUI.enabled=shopPending<0;
            GUI.backgroundColor=new Color(.16f,.27f,.32f);
            Fill(new Rect(0,602,1280,28),new Color(.03f,.06f,.08f,.94f));
            GUI.Label(new Rect(18,602,550,27),$"泵站回廊 / {(Battle.Phase==BattlePhase.Ready?"准备":"战斗中 · 商店锁定")}   DP {Battle.DP}   守方 {Battle.Defense}",new GUIStyle(label){fontSize=13});
            if(GUI.Button(new Rect(619,603,115,25),Battle.IsPaused?"继续战斗":"暂停战斗",shopButton)&&Battle.Phase==BattlePhase.Running)Battle.SetPaused(!Battle.IsPaused);
            if(GUI.Button(new Rect(742,603,115,25),"重新测试",shopButton))EnterShopTrial();
            GUI.enabled=prep;
            if(GUI.Button(new Rect(865,603,115,25),"满席两星",shopButton))EnterShopTrial(1);
            if(GUI.Button(new Rect(988,603,115,25),"满席三星",shopButton))EnterShopTrial(2);GUI.enabled=shopPending<0;
            if(GUI.Button(new Rect(1111,603,146,25),"退出测试",shopButton)){LeaveShopTrial();GUI.matrix=previous;GUI.color=old;GUI.backgroundColor=background;return;}GUI.enabled=true;
            Fill(new Rect(0,632,1280,88),new Color(.04f,.07f,.09f,.98f));
            ShopBorder(new Rect(8,632,1264,80),new Color(.25f,.35f,.39f),1);
            GUI.Label(new Rect(18,633,1240,21),$"备战区   {shop.Bench.Count} / 10      编队 {shopLineup.Count}/{shop.Level}       点击切换编队 · 拖到红门部署 · 向下出售",new GUIStyle(smallStyle){fontSize=12,normal={textColor=new Color(.65f,.77f,.82f)}});
            for(int i=0;i<10;i++)
            {
                var r=ShopBenchRect(i);Fill(r,new Color(.07f,.12f,.15f));ShopBorder(r,new Color(.23f,.34f,.40f),1);
                if(i>=shop.Bench.Count){GUI.Label(r,"+",new GUIStyle(center){fontSize=20,normal={textColor=new Color(.19f,.27f,.31f)}});continue;}
                var p=shop.Bench[i];if(p.Star>1)Fill(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),p.Star==3?new Color(.28f,.24f,.15f):new Color(.17f,.23f,.28f));
                bool active=shopLineup.Contains(p.Id)&&shop.Bench.Where(b=>b.Id==p.Id).OrderByDescending(b=>b.Star).ThenBy(b=>b.Instance).First().Instance==p.Instance;
                if(active||p.Star>1)ShopBorder(r,active?new Color(.52f,.90f,.93f):p.Star==3?new Color(.89f,.74f,.38f):new Color(.66f,.72f,.81f),1);
                DrawTestPortrait(new Rect(r.x+2,r.y+1,46,44),OriginalTalulahArt.CardPortrait(shopDefinitions[p.Id].Card));
                DrawShopStars(new Rect(r.x+53,r.y+4,36,11),p.Star,p.Star==3?new Color(1,.89f,.61f):new Color(.75f,.89f,1));
                GUI.Label(new Rect(r.x+49,r.y+19,65,24),shop.Definition(p.Id).Name,new GUIStyle(center){fontSize=10,normal={textColor=new Color(.67f,.77f,.82f)}});
            }
            if(shopDrag>=0&&shopMoving)
            {
                var q=shop.Quote(shopDrag);bool hit=ShopSellRect.Contains(point)&&point.y>shopDragStart.y+12;
                Fill(ShopSellRect,hit?new Color(.42f,.29f,.12f,.98f):new Color(.12f,.17f,.20f,.98f));ShopBorder(ShopSellRect,new Color(.85f,.69f,.36f),2);
                GUI.Label(ShopSellRect,$"{(hit?"松开出售":"向下拖动出售")}   {q.Message} · {q.FinalStar}星   +{q.Value} 源石",center);
                if(q.Success)DrawTestPortrait(new Rect(point.x-40,point.y-55,80,80),OriginalTalulahArt.CardPortrait(shopDefinitions[q.Id].Card));
            }
            if(Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost)GUI.Label(new Rect(450,450,380,45),Battle.Phase==BattlePhase.Won?"突破成功 · 可重新测试":"作战结束 · 可重新测试",title);
            DrawShopPromotion();
            if(Time.unscaledTime<shopMessageUntil)
            {
                Fill(new Rect(280,510,720,36),new Color(.04f,.12f,.15f,.94f));GUI.Label(new Rect(280,510,720,36),shopMessage,new GUIStyle(center){fontSize=16});
            }
            if(shopPending>=0)
            {
                GUI.enabled=true;
                Fill(new Rect(0,0,1280,720),new Color(0,0,0,.6f));Fill(new Rect(340,260,600,190),new Color(.08f,.15f,.19f));
                var quote=shop.Purchase(shopPending);
                GUI.Label(new Rect(365,280,550,70),quote.Message,new GUIStyle(center){wordWrap=true});
                if(GUI.Button(new Rect(385,374,200,48),"取消",shopButton))shopPending=-1;
                if(GUI.Button(new Rect(695,374,200,48),"购买并合成",shopButton)){int index=shopPending;shopPending=-1;PurchaseShopCard(index,true);}
            }
            GUI.matrix=previous;GUI.color=old;GUI.backgroundColor=background;
        }
        static Color ShopTier(int cost)=>cost==5?new Color(.58f,.43f,.17f):cost==4?new Color(.53f,.28f,.64f):cost==3?new Color(.12f,.34f,.61f):cost==2?new Color(.16f,.44f,.26f):new Color(.48f,.52f,.54f);
        static string[] ShopTags(string id)=>id==OriginalSlugArt.CardId||id=="CARD_M05"?new[]{"感染生物"}:id=="CARD_M02"?new[]{"感染生物","整合运动"}:id=="CARD_M03"?new[]{"感染生物","游击队","整合运动"}:id=="CARD_M08"?new[]{"整合运动","战地拾荒"}:id=="CARD_M17"||id=="CARD_M18"?new[]{"游击队","整合运动","战地拾荒"}:id=="CARD_M19"?new[]{"游击队","整合运动","萨卡兹"}:new[]{"游击队","整合运动"};
        static void ShopLine(Vector2 a,Vector2 b,Color color,float width=2)
        {
            Matrix4x4 previous=GUI.matrix;GUI.matrix=previous*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width/2,Vector2.Distance(a,b),width),color);GUI.matrix=previous;
        }
        static void DrawShopRefreshIcon(Vector2 center,Color color)
        {
            for(int arc=0;arc<2;arc++)for(int i=0;i<12;i++){float a=(arc*180+25+i*10)*Mathf.Deg2Rad,b=a+10*Mathf.Deg2Rad;ShopLine(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*17,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*17,color);}
            ShopLine(center+new Vector2(-19,-9),center+new Vector2(-19,0),color);ShopLine(center+new Vector2(-19,0),center+new Vector2(-10,0),color);ShopLine(center+new Vector2(19,9),center+new Vector2(19,0),color);ShopLine(center+new Vector2(19,0),center+new Vector2(10,0),color);
        }
        static void ShopBorder(Rect r,Color c,float width){Fill(new Rect(r.x,r.y,r.width,width),c);Fill(new Rect(r.x,r.yMax-width,r.width,width),c);Fill(new Rect(r.x,r.y,width,r.height),c);Fill(new Rect(r.xMax-width,r.y,width,r.height),c);}
        void DrawShopDefenderBars()
        {
            foreach(var defender in Battle.Defenders)
            {
                if(!defender.Alive)continue;
                Vector2 p=GUIPosition(Camera.main.WorldToScreenPoint(defender.Position+Camera.main.transform.up*1.6f));
                if(!ShopBattlefieldRect.Contains(p))continue;
                Fill(new Rect(p.x-24,p.y,48,6),new Color(.03f,.05f,.06f));Fill(new Rect(p.x-23,p.y+1,46*defender.Health/defender.MaxHealth,4),new Color(.96f,.66f,.3f));
                if(!defender.Settings.Skill.Enabled)continue;
                bool active=Battle.DefenderSkillActive(defender);
                float progress=active?(float)((defender.SkillActiveUntil-Battle.Clock)/defender.Settings.Skill.Duration):defender.SkillSP/defender.Settings.Skill.RequiredSP;
                Fill(new Rect(p.x-24,p.y+8,48,4),new Color(.03f,.05f,.06f));Fill(new Rect(p.x-23,p.y+9,46*Mathf.Clamp01(progress),2),active?new Color(1,.8f,.2f):new Color(.25f,.65f,1));
            }
        }
        static Texture2D CreateShopStar()
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
        void DrawShopStars(Rect bounds,int count,Color color)
        {
            Color previous=GUI.color;GUI.color=color;float size=Mathf.Min(bounds.height,bounds.width/count),start=bounds.center.x-count*size/2;
            for(int i=0;i<count;i++)GUI.DrawTexture(new Rect(start+i*size,bounds.y,size,size),shopStar,ScaleMode.ScaleToFit,true);GUI.color=previous;
        }
        void DrawShopPromotion()
        {
            float elapsed=Time.unscaledTime-shopPromotionAt;if(elapsed<0||elapsed>2.6f)return;
            float alpha=Mathf.Clamp01(Mathf.Min(elapsed/.18f,(2.6f-elapsed)/.5f));Color c=shopPromotionStar==3?new Color(1,.83f,.42f,alpha):new Color(.62f,.88f,1,alpha);
            Rect panel=new Rect(410,300,460,145);Fill(panel,new Color(.025f,.06f,.08f,.96f*alpha));ShopBorder(new Rect(403,293,474,159),new Color(c.r,c.g,c.b,.22f*alpha),3);ShopBorder(panel,c,2);
            Color previous=GUI.color;GUI.color=new Color(1,1,1,alpha);DrawTestPortrait(new Rect(427,309,111,123),OriginalTalulahArt.CardPortrait(shopDefinitions[shopPromotionId].Card));GUI.color=previous;
            DrawShopStars(new Rect(555,311,300,54),shopPromotionStar,c);
            GUI.Label(new Rect(555,366,300,55),shop.Definition(shopPromotionId).Name+" · 晋升成功",new GUIStyle(textStyle){fontSize=24,fontStyle=FontStyle.Bold,normal={textColor=new Color(1,1,1,alpha)},alignment=TextAnchor.MiddleCenter});
        }
    }
}
