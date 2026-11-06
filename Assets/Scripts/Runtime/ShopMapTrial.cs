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
        int shopDrag=-1,shopPromotedInstance=-1;
        string shopMessage;
        Vector2 shopDragStart,shopPointer;
        Vector2? shopPurchaseOrigin;
        float shopPromotionAt=-100,shopMessageUntil,shopRefreshAt=-100,shopUpgradeAt=-100;
        bool shopMoving,shopVisible;
        Texture2D shopCoin,shopStar,shopFade,shopRefreshSymbol;
        public bool ShopTrialActive=>shop!=null;
        public ShopTrialModel Shop=>shop;
        public string ShopFeedback=>shopMessage;
        public bool ShopVisible=>shopVisible;
        public float ShopBenchPromotionRemaining(int instance)=>ShopTrialActive?ShopFlowPromotionRemaining(instance):0;
        public static Rect ShopBattlefieldRect=>new Rect(0,0,1280,720);
        public static Rect ShopPanelRect=>ApprovedUi.ShopPanel;
        // ShopSellRect is now the left edge. The former bottom sale target is retired.
        public static Rect ShopSellRect=>ApprovedUi.SellLeft;
        public static Rect ShopSellRightRect=>ApprovedUi.SellRight;
        public static Rect ShopRecruitListRect=>ApprovedUi.RecruitList;
        public static Rect ShopRecruitRowRect(int i)=>new Rect(ShopRecruitListRect.x+6,ShopRecruitListRect.y+35+i*37,ShopRecruitListRect.width-12,34);
        public static Rect ShopOfferRect(int i)=>ApprovedUi.Offer(i);
        public static Rect ShopBenchRect(int i)=>ApprovedUi.ReserveSlot(i);
        Rect ShopPreparationPieceRect(int index)
        {
            if(shop==null||index<0||index>=shop.Bench.Count)return Rect.zero;
            var piece=shop.Bench[index];
            if(IsLineupPiece(piece))return ApprovedUi.LineupSlot(shopLineup.IndexOf(piece.Id));
            int slot=shop.Bench.Where(p=>!IsLineupPiece(p)).ToList().FindIndex(p=>p.Instance==piece.Instance);
            return slot<0?Rect.zero:ApprovedUi.ReserveSlot(slot);
        }
        bool ShopSalePoint(Vector2 point)=>ShopSellRect.Contains(point)||ShopSellRightRect.Contains(point);
        bool ShopUiBlocksPoint(Vector2 point)
        {
            if(new Rect(0,0,1280,60).Contains(point)||CampaignOverlayBlocksPoint(point)||SynergyBlocksMap(point))return true;
            if(loopMenuOpen&&LoopMenuRect.Contains(point)||loopTestSettlementOpen&&LoopTestPanelRect.Contains(point))return true;
            if((shopVisible||ShopPanelTransitioning)&&(ShopPanelRect.Contains(point)||
                Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop)&&ShopRecruitListRect.Contains(point)))return true;
            bool preparing=Loop==null?Battle.Phase==BattlePhase.Ready:Loop.CanShop;
            if(preparing)
                return new Rect(188,480,942,25).Contains(point)||new Rect(188,516,942,74).Contains(point)||
                    new Rect(188,595,942,25).Contains(point)||new Rect(188,627,942,74).Contains(point)||new Rect(1142,624,120,69).Contains(point);
            return new Rect(0,552,1280,168).Contains(point);
        }
        public bool ShopMapPointAvailable(Vector2 point)=>ShopTrialActive&&!CampaignUiModalOpen&&!loopMenuOpen&&!loopTestSettlementOpen&&
            (Loop==null||Loop.CanDeploy)&&(Battle.Phase==BattlePhase.Ready||Battle.Phase==BattlePhase.Running)&&CurrentShopBattlefieldRect.Contains(point)&&!ShopUiBlocksPoint(point)&&
            (!DefenderInspectorVisible||!DefenderInspectorRect.Contains(point));
        public void SetShopVisible(bool visible)
        {
            if(shop==null||visible&&Loop!=null&&!Loop.CanShop)return;
            CancelShopDrag();if(visible)CloseDefenderInspector();AnimateShopVisibility(visible);
        }

        public void EnterShopTrial(int preset=0,string mapId=StrongholdChallengeLayout.Id,DefenderTrialDefinition encounter=null)
        {
            activeSynergyTrial=null;Loop=null;
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
            if(mapId==StrongholdChallengeLayout.Id)offerIds[2]="CARD_M17";
            shop.Preset(pieces,offerIds.Where(shopDefinitions.ContainsKey),preset==2?7:5,preset==2?46:18);
            if(mapId==StrongholdChallengeLayout.Id&&preset==0)shop.Preset(pieces,offerIds.Where(shopDefinitions.ContainsKey),5,18,24);
            shopLineup.Clear();shopDeck.Clear();shopLineup.Add("CARD_M08");
            CloseDefenderInspector();shopDrag=-1;shopMoving=false;shopPromotionAt=-100;shopPromotedInstance=-1;shopVisible=preset>0;
            ResetShopPresentation();
            if(shopMapCopy!=null)Destroy(shopMapCopy);
            shopMapCopy=Instantiate(encounter!=null?encounter:Resources.Load<DefenderTrialDefinition>("Maps/BattleShop"));
            if(shopCoin==null)
            {
                shopCoin=new Texture2D(2,2,TextureFormat.RGBA32,false);
                shopCoin.LoadImage(File.ReadAllBytes(ArtSourcePaths.Resolve("ShopTrial/originium.png")));
            }
            if(shopStar==null)shopStar=CreateShopStar();EnsureShopAura();
            if(shopFade==null)
            {
                shopFade=new Texture2D(64,1,TextureFormat.RGBA32,false){name="Shop rarity fade",wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[64];for(int i=0;i<64;i++)pixels[i]=new Color(1,1,1,.72f*Mathf.Clamp01(1-i/50f));shopFade.SetPixels(pixels);shopFade.Apply();
            }
            EnterDefense(shopMapCopy);
            shopMapCameraPosition=Camera.main.transform.position;
            SyncShopDeck();
            ShopNotice(preset==0?"Tab 打开商店；拖到红门部署，拖到左右两侧出售":"满席测试：点击第一张弩手，直接合成并腾出空位");
        }
        void SyncShopDeck(bool autoFill=true,bool newStage=false)
        {
            if(shop==null||!newStage&&(Battle.Phase!=BattlePhase.Ready||Loop!=null&&!Loop.CanShop))return;
            PruneGuerrillaTransfers();
            shopLineup.RemoveAll(id=>!shop.Bench.Any(p=>p.Id==id));
            if(autoFill)foreach(var piece in shop.Bench)
                if(shopLineup.Count<shop.Level&&!shopLineup.Contains(piece.Id))shopLineup.Add(piece.Id);
            if(CampaignActive&&!shop.SetLineup(shopLineup)){ShopNotice("独立备战已满，请先出售或合成");return;}
            shopDeck.Clear();foreach(string id in shopLineup)shopDeck.Add(shopDefinitions[id]);
            var effective=shopDeck.Count>0?shopDeck:new List<ManualBattleDefinition>{shopDefinitions["CARD_M08"]};
            defenderTrial.battle=effective[0];defenderTrial.rosterCards=effective.Skip(1).ToArray();definition=effective[0];
            ClearViews();Battle=CreateBattle();selectedRosterIndex=0;Selected=false;CancelInteraction();EnableCombatView();
            Camera.main.rect=TacticalViewport.Current.CameraRect(CurrentShopBattlefieldRect);
            FrameShopMap();
            SyncShopBenchModels();
            RefreshInfectionChoicePrompt();
        }
        IEnumerable<Vector3> ShopMapPlayablePoints()
        {
            foreach(var lane in shopMapCopy.lanes)foreach(var point in lane.points){yield return point;yield return point+Camera.main.transform.up*1.65f;}
            foreach(var defender in shopMapCopy.defenders){yield return defender.position;yield return defender.position+Camera.main.transform.up*1.65f;}
        }
        int shopWindowWidth,shopWindowHeight;
        void RefreshShopWindowFrame()
        {
            if(Screen.width==shopWindowWidth&&Screen.height==shopWindowHeight)return;
            CancelShopDrag();FrameShopMap();
        }
        void FrameShopMap()
        {
            shopWindowWidth=Screen.width;shopWindowHeight=Screen.height;
            var camera=Camera.main;camera.rect=TacticalViewport.Current.CameraRect(CurrentShopBattlefieldRect);camera.transform.position=shopMapCameraPosition;camera.fieldOfView=shopPreviousFieldOfView;
            var points=ShopMapPlayablePoints().ToArray();
            // Keep interactive terrain between the trait rail, header and bottom card bar.
            // Decorative geometry still fills the full camera; only the fit budget is inset.
            var center=new Vector2(.56f,.61f);
            for(int pass=0;pass<5;pass++)
            {
                var projected=points.Select(camera.WorldToViewportPoint).ToArray();
                float centerX=(projected.Min(p=>p.x)+projected.Max(p=>p.x))*.5f,centerY=(projected.Min(p=>p.y)+projected.Max(p=>p.y))*.5f;
                float depth=projected.Average(p=>p.z);
                camera.transform.position+=camera.ViewportToWorldPoint(new Vector3(centerX,centerY,depth))-camera.ViewportToWorldPoint(new Vector3(center.x,center.y,depth));
                float extent=points.Select(camera.WorldToViewportPoint).Max(p=>Mathf.Max(Mathf.Abs(p.x-center.x)/.39f,Mathf.Abs(p.y-center.y)/.26f));
                float zoom=1/Mathf.Max(.05f,extent);
                camera.fieldOfView=2*Mathf.Atan(Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)/zoom)*Mathf.Rad2Deg;
            }
        }
        public ShopTrialModel.Result PurchaseShopCard(int slot)
        {
            Vector2 origin=shopPurchaseOrigin??ShopOfferRect(slot).center;shopPurchaseOrigin=null;
            if(shop==null||Battle.Phase!=BattlePhase.Ready||Loop!=null&&!Loop.CanShop)return new ShopTrialModel.Result{Message="战斗开始后不能招募或升星"};
            var snapshot=CaptureShopFlowSnapshot();
            var result=shop.Purchase(slot);
            if(result.Success)
            {
                shopMessage=null;shopMessageUntil=0;
                if(shopLineup.Count<shop.Level&&!shopLineup.Contains(result.Id))shopLineup.Add(result.Id);
                SyncShopDeck(false);
                BeginShopArrival(result);
                BeginShopFlow(snapshot,result,origin);
            }
            if(!result.Success)ShopNotice(result.Message);return result;
        }
        public ShopTrialModel.Result SellShopCard(int instance)
        {
            if(shop==null||Battle.Phase!=BattlePhase.Ready||Loop!=null&&!Loop.CanShop)return new ShopTrialModel.Result{Message="战斗中不能出售"};
            var r=shop.Sell(instance);if(r.Success)SyncShopDeck(false);ShopNotice(r.Success?$"出售 {r.Message} · +{r.Value} 金币":r.Message);return r;
        }
        void ShopNotice(string message){shopMessage=message;shopMessageUntil=Time.unscaledTime+3;}
        public void CancelShopDrag(){golemDragging=false;shopDrag=-1;shopMoving=false;CancelInteraction();}
        public void LeaveShopTrial()
        {
            activeSynergyTrial=null;Loop=null;
            CancelShopDrag();ClearShopBenchModels();shop=null;ResetGuerrillaTransfers();ResetMudrockTransfers();shopLineup.Clear();shopDeck.Clear();Camera.main.rect=shopPreviousViewport;Camera.main.fieldOfView=shopPreviousFieldOfView;
            ReturnToMonsterTests();if(shopMapCopy!=null){Destroy(shopMapCopy);shopMapCopy=null;}
        }
        bool ShopPointerEvent(EventType type,Vector2 point,int button=0)
        {
            if(shop==null)return false;
            if(CampaignUiModalOpen||loopMenuOpen||loopTestSettlementOpen)return false;
            if(Loop==null&&Battle.Phase!=BattlePhase.Ready&&Battle.Phase!=BattlePhase.Running)return false;
            if(CampaignActive&&Loop.RewardStage&&type==EventType.MouseDown&&button==0&&shop.Bench.Select((p,i)=>ShopPieceRect(i)).Any(r=>r.Contains(point)))
            {ShopNotice("本关是保送补给，不部署怪物；点击左侧领取，连续两次补给后进入战斗");return true;}
            if(HandleGolemPointer(type,point,button))return true;
            if(Loop!=null&&!Loop.CanDeploy&&!Loop.CanShop)return false;
            if(type==EventType.MouseDown||type==EventType.MouseDrag||type==EventType.MouseMove||type==EventType.MouseUp)shopPointer=point;
            if(type==EventType.MouseDown&&button==0)
            {
                if(Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop)&&shopVisible&&!ShopPanelTransitioning)
                    for(int i=0;i<shop.Offers.Count;i++)
                    {
                        if(shop.Offers[i]==null)continue;
                        Rect offer=ShopOfferRect(i),row=ShopRecruitRowRect(i);
                        if(!offer.Contains(point)&&!row.Contains(point))continue;
                        shopPurchaseOrigin=row.Contains(point)?row.center:offer.center;
                        PurchaseShopCard(i);return true;
                    }
                for(int i=0;i<shop.Bench.Count;i++)if(ShopPieceRect(i).Contains(point)){shopDrag=shop.Bench[i].Instance;shopDragStart=point;shopMoving=false;return true;}
                if(ShopMapPointAvailable(point)){if(HitUnit(ScreenPosition(point))==null&&InspectShopDefender(point))return true;CloseDefenderInspector();ProcessPointerDown(ScreenPosition(point));}
                else if(Loop!=null&&SkillMenuOpen&&SkillPopupRect.Contains(point)){ProcessPointerDown(ScreenPosition(point));return true;}
            }
            if(type==EventType.MouseDrag&&shopDrag>=0){shopMoving|=(point-shopDragStart).magnitude>8||ShopSalePoint(point);if(shopMoving){CloseDefenderInspector();if(Loop==null||Loop.CanDeploy)AnimateShopVisibility(false);}return true;}
            if(type!=EventType.MouseUp||button!=0||shopDrag<0)return false;
            int instance=shopDrag;bool moved=shopMoving;shopDrag=-1;shopMoving=false;
            var piece=shop.Bench.FirstOrDefault(p=>p.Instance==instance);if(piece==null)return true;
            if(moved&&(Loop==null||Loop.CanShop)&&ShopSalePoint(point)){SellShopCard(instance);return true;}
            if(moved&&Loop!=null&&Loop.CanShop)return true;
            if(moved&&ShopMapPointAvailable(point))
            {
                int index=shopLineup.IndexOf(piece.Id);
                if(index<0){ShopNotice("编队位已满：点击备战棋子调整编队，或先升级");return true;}
                selectedRosterIndex=index;Selected=true;
                if(PointerGround(ScreenPosition(point),out var world)){var result=TryDeployAt(world,true);if(!result.Success||Loop==null)ShopNotice(result.Success?"部署成功 · 战斗开始后商店锁定":Feedback);}
                Selected=false;return true;
            }
            if(!moved&&Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop))
            {
                if(shopLineup.Contains(piece.Id)){if(CampaignActive&&shop.Bench.Count-shopLineup.Count>=10){ShopNotice("备战已满，无法移出编队");return true;}shopLineup.Remove(piece.Id);}
                else if(shopLineup.Count<shop.Level)shopLineup.Add(piece.Id);
                else {ShopNotice("编队位已满，先取消其他棋子或升级");return true;}
                SyncShopDeck(false);
            }
            return true;
        }
        void DrawShopTrial()
        {
            if(Loop!=null){DrawLoopInterface();return;}
            DrawApprovedStandaloneShop();
        }
        void DrawShopMarket(bool prep)=>DrawApprovedShopMarket(prep);
        static Color ShopTier(int cost)=>cost==5?new Color(.58f,.43f,.17f):cost==4?new Color(.53f,.28f,.64f):cost==3?new Color(.12f,.34f,.61f):cost==2?new Color(.16f,.44f,.26f):new Color(.48f,.52f,.54f);
        static string[] ShopTags(string id)=>CampaignBalance.Tags(id);
        static void ShopLine(Vector2 a,Vector2 b,Color color,float width=2)
        {
            Matrix4x4 previous=GUI.matrix;GUI.matrix=previous*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width/2,Vector2.Distance(a,b),width),color);GUI.matrix=previous;
        }
        void DrawShopRefreshIcon(Vector2 center,Color color)
        {
            if(shopRefreshSymbol==null)
            {
                var segments=new List<Vector2>();
                for(int arc=0;arc<2;arc++)for(int i=0;i<13;i++)
                {
                    float angle=(arc*180+25+i*10)*Mathf.Deg2Rad;
                    segments.Add(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*23+Vector2.one*32);
                    float next=angle+10*Mathf.Deg2Rad;segments.Add(new Vector2(Mathf.Cos(next),Mathf.Sin(next))*23+Vector2.one*32);
                }
                segments.AddRange(new[]{new Vector2(7,19),new Vector2(7,32),new Vector2(7,32),new Vector2(20,32),new Vector2(57,45),new Vector2(57,32),new Vector2(57,32),new Vector2(44,32)});
                var pixels=new Color[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    var point=new Vector2(x+.5f,y+.5f);float distance=64;
                    for(int i=0;i<segments.Count;i+=2){Vector2 a=segments[i],v=segments[i+1]-a;distance=Mathf.Min(distance,Vector2.Distance(point,a+v*Mathf.Clamp01(Vector2.Dot(point-a,v)/v.sqrMagnitude)));}
                    pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(2-distance));
                }
                shopRefreshSymbol=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Shop refresh arrows",filterMode=FilterMode.Bilinear};shopRefreshSymbol.SetPixels(pixels);shopRefreshSymbol.Apply();
            }
            Color previous=GUI.color;GUI.color=previous*color;GUI.DrawTexture(new Rect(center.x-20,center.y-20,40,40),shopRefreshSymbol);GUI.color=previous;
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
                if(Battle.StrongholdChallenge)GUI.Label(new Rect(p.x-72,p.y-20,144,20),defender.Settings.Name,new GUIStyle(smallStyle){fontSize=11,alignment=TextAnchor.MiddleCenter,normal={textColor=defender.HighGround?new Color(.5f,.9f,1):new Color(1,.8f,.5f)}});
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
    }
}
