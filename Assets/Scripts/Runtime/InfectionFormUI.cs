using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static readonly Color InfectionOrange=new Color(.68f,.33f,.15f);
        static readonly Color InfectionBlue=new Color(.36f,.53f,.62f);
        static readonly Color InfectionRule=new Color(.73f,.80f,.83f);
        static readonly Color InfectionCardPaper=new Color(.96f,.97f,.965f);
        public static Rect InfectionFormPanelRect=>new Rect(230,105,1026,480);
        public static Rect InfectionCloseRect=>new Rect(InfectionFormPanelRect.xMax-47,InfectionFormPanelRect.y+12,31,28);
        static Rect InfectionFormCardRect(InfectionForm form)
        {
            int column=(int)form;if(column<0||column>2)return Rect.zero;
            var panel=InfectionFormPanelRect;float width=(panel.width-60)/3;
            return new Rect(panel.x+18+column*(width+12),panel.y+112,width,235);
        }
        public static Rect InfectionPrimaryRect(InfectionForm form)
        {
            var card=InfectionFormCardRect(form);return card.width<=0?Rect.zero:new Rect(card.x+12,card.y+200,(card.width-30)/2,27);
        }
        public static Rect InfectionSecondaryRect(InfectionForm form)
        {
            var primary=InfectionPrimaryRect(form);return primary.width<=0?Rect.zero:new Rect(primary.xMax+6,primary.y,primary.width,primary.height);
        }
        bool InfectionChoiceEditable=>CampaignActive&&Loop.CanShop&&Battle!=null&&Battle.Phase==BattlePhase.Ready;
        int lastInfectionChoiceTier;
        void RefreshInfectionChoicePrompt()
        {
            if(!InfectionChoiceEditable)return;
            int tier=Battle.InfectionTier,previous=lastInfectionChoiceTier;
            // Remember the current tier, not the highest tier ever reached. Losing members
            // rearms the next upward crossing, while buying duplicates or changing forms does not.
            lastInfectionChoiceTier=tier;
            if(tier<2||tier<=previous)return;
            inspectedSynergy="感染生物";campaignConstructOpen=false;CancelShopDrag();
        }
        InfectionForm VisibleInfectionMain=>InfectionChoiceEditable?infectionForm:Battle.InfectionShape;
        InfectionForm VisibleInfectionSecond=>InfectionChoiceEditable?infectionSecond:Battle.InfectionSecondShape;

        void DrawInfectionFormRow(Rect row,int tier)
        {
            string main=FormName(VisibleInfectionMain);
            string secondary=tier>=6?"副 "+FormName(VisibleInfectionSecond):"副6档解锁";
            GUI.Label(SynergySupplementRect(row),"主 "+main+" · "+secondary,
                CampaignText(10,color:tier>0?new Color(.68f,.75f,.83f):new Color(.55f,.62f,.71f)));
        }
        static string InfectionEffect(InfectionForm form,int tier)
        {
            if(tier<2)return "2种感染生物激活后生效";
            if(form==InfectionForm.Swarm)return tier>=6?"3只，各55%生命 / 攻击":tier>=4?"2只，各70%生命 / 攻击":"2只，各55%生命 / 攻击";
            if(form==InfectionForm.Shell)return tier>=6?"35%生命护盾，护甲＋3":tier>=4?"30%生命护盾，护甲＋2":"18%最大生命护盾";
            return tier>=6?"85法伤 · 半径1.3 · 最多3目标":tier>=4?"65法伤 · 半径1.2 · 最多3目标":"35法伤 · 半径1 · 最多2目标";
        }
        bool InfectionChoiceButton(Rect rect,string caption,bool selected,bool secondary,bool available)
        {
            bool enabled=GUI.enabled;GUI.enabled=enabled&&available;
            var color=selected?(secondary?InfectionBlue:InfectionOrange):new Color(.88f,.92f,.94f);
            var style=CampaignText(12,TextAnchor.MiddleCenter,selected?Color.white:ApprovedUi.Ink,600);
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused})
            {state.background=Texture2D.whiteTexture;state.textColor=selected?Color.white:ApprovedUi.Ink;}
            Color previous=GUI.backgroundColor;GUI.backgroundColor=color;
            bool clicked=GUI.Button(rect,caption,style);GUI.backgroundColor=previous;
            ApprovedUi.Border(rect,available?(selected?color:InfectionRule):new Color(.78f,.82f,.84f));GUI.enabled=enabled;
            return clicked;
        }
        void DrawInfectionFormPanel(SynergySummary summary)
        {
            var r=InfectionFormPanelRect;int tier=summary.Tier;bool editable=InfectionChoiceEditable;
            var primary=VisibleInfectionMain;var secondary=VisibleInfectionSecond;
            Fill(r,new Color(.925f,.944f,.95f));ApprovedUi.Border(r,InfectionRule);
            GUI.Label(new Rect(r.x+18,r.y+10,260,17),"构筑选择 / INFECTION",CampaignText(10,color:ApprovedUi.Muted));
            GUI.Label(new Rect(r.x+18,r.y+28,150,30),"感染生物",CampaignText(22,weight:600));
            GUI.Label(new Rect(r.x+137,r.y+33,350,24),summary.Count+"种 · "+(tier>=6?"6档 / 主形态＋二档副形态":tier>0?tier+"档 / 仅主形态":"未达2种激活门槛"),CampaignText(12,color:ApprovedUi.Muted));
            GUI.Label(new Rect(r.xMax-275,r.y+25,214,26),editable?"开战时锁定本场选择":"本场选择已锁定",CampaignText(12,TextAnchor.MiddleRight,editable?ApprovedUi.Muted:InfectionOrange));
            if(LoopButton(InfectionCloseRect,"×")){inspectedSynergy=null;return;}
            Fill(new Rect(r.x,r.y+65,r.width,1),InfectionRule);
            GUI.Label(new Rect(r.x+18,r.y+76,r.width-36,25),!editable?"当前为本场已锁定形态，战斗中不可更改":tier>=6?
                "选择一种主形态；再选一种不同的副形态（取2档效果）":"选择一种主形态；副形态在6档解锁",CampaignText(13,weight:600));
            for(int i=0;i<3;i++)
            {
                var form=(InfectionForm)i;var card=InfectionFormCardRect(form);bool main=primary==form,second=tier>=6&&secondary==form;
                Color accent=main?InfectionOrange:second?InfectionBlue:InfectionRule;
                Fill(card,main?new Color(.98f,.955f,.918f):InfectionCardPaper);ApprovedUi.Border(card,accent,main||second?1.5f:1);
                DrawInfectionFormIcon(form,new Vector2(card.x+25,card.y+29),12,main?InfectionOrange:InfectionBlue);
                GUI.Label(new Rect(card.x+48,card.y+10,115,30),FormName(form),CampaignText(21,weight:600));
                GUI.Label(new Rect(card.xMax-125,card.y+13,113,23),main?"主形态 · "+(tier>0?tier+"档":"待激活"):second?"副形态 · 2档":"未选择",
                    CampaignText(11,TextAnchor.MiddleRight,main?InfectionOrange:second?InfectionBlue:ApprovedUi.Muted));
                string description=form==InfectionForm.Swarm?"以数量形成推进压力":form==InfectionForm.Shell?"用短时护盾支撑第一轮接敌":"用最终死亡交换近身范围伤害";
                GUI.Label(new Rect(card.x+48,card.y+44,card.width-60,21),description,CampaignText(11,color:ApprovedUi.Muted));
                int effective=main?tier:second?2:0;
                for(int level=0;level<3;level++)
                {
                    int n=2+level*2;float y=card.y+82+level*27;
                    Fill(new Rect(card.x+13,y-3,card.width-26,1),InfectionRule);
                    Color ink=effective==n?(main?InfectionOrange:InfectionBlue):ApprovedUi.Muted;
                    GUI.Label(new Rect(card.x+13,y,31,21),n+"档",CampaignText(11,color:ink,weight:effective==n?600:400));
                    GUI.Label(new Rect(card.x+49,y,card.width-61,21),InfectionEffect(form,n),CampaignText(11,color:ink));
                }
                string note=form==InfectionForm.Swarm?"一次部署仍只扣一次DP / 次数。":form==InfectionForm.Shell?"护盾持续6秒，仅作用感染实体。":"死亡0.2秒后引爆；伤害每升1星＋35%。";
                GUI.Label(new Rect(card.x+13,card.y+169,card.width-26,25),note,CampaignText(10,color:ApprovedUi.Muted));
                if(InfectionChoiceButton(InfectionPrimaryRect(form),main?"✓ 已选主形态":"选为主形态",main,false,editable))SetCampaignForms(form,infectionSecond);
                string secondCaption=second?"✓ 已选副形态":tier<6?"6档解锁副形态":main?"不能与主相同":"选为副形态";
                if(InfectionChoiceButton(InfectionSecondaryRect(form),secondCaption,second,true,editable&&tier>=6&&!main))SetCampaignForms(infectionForm,form);
            }
            // A click can replace the preparation snapshot, so the summary reads the current selection.
            tier=Battle.InfectionTier;primary=VisibleInfectionMain;secondary=VisibleInfectionSecond;
            var result=new Rect(r.x+18,r.y+360,r.width-36,65);Fill(result,new Color(.865f,.905f,.927f));Fill(new Rect(result.x,result.y,3,result.height),InfectionOrange);
            GUI.Label(new Rect(result.x+13,result.y+6,270,16),"本场主形态",CampaignText(10,color:ApprovedUi.Muted));
            GUI.Label(new Rect(result.x+13,result.y+23,270,20),FormName(primary)+" · "+(tier>0?tier+"档":"未激活"),CampaignText(14,weight:600));
            GUI.Label(new Rect(result.x+13,result.y+44,290,16),InfectionEffect(primary,tier),CampaignText(10,color:ApprovedUi.Muted));
            Fill(new Rect(result.x+314,result.y+10,1,45),InfectionRule);
            GUI.Label(new Rect(result.x+331,result.y+6,285,16),"本场副形态",CampaignText(10,color:ApprovedUi.Muted));
            GUI.Label(new Rect(result.x+331,result.y+23,285,20),tier>=6?FormName(secondary)+" · 2档效果":"未解锁",CampaignText(14,weight:600));
            GUI.Label(new Rect(result.x+331,result.y+44,285,16),tier>=6?InfectionEffect(secondary,2):"达到6个不同感染成员后可选",CampaignText(10,color:ApprovedUi.Muted));
            var noteStyle=CampaignText(10,TextAnchor.MiddleRight,ApprovedUi.Muted);noteStyle.wordWrap=true;
            GUI.Label(new Rect(result.xMax-325,result.y+12,308,43),"爆裂与角色固有自爆独立结算。\n形态不改变原卡的部署DP或次数。",noteStyle);
            GUI.Label(new Rect(r.x+18,r.y+439,r.width-161,27),editable?"选择即时生效；完成后返回整备，开战时锁定。":"本场已锁定；结束验证并返回整备后再修改。",CampaignText(11,color:ApprovedUi.Muted));
            if(LoopButton(new Rect(r.xMax-122,r.yMax-40,104,28),editable?"完成选择":"返回战斗",true))inspectedSynergy=null;
        }
        void DrawInfectionFormIcon(InfectionForm form,Vector2 center,float radius,Color color)
        {
            if(form==InfectionForm.Shell)
            {
                var p=new[]{new Vector2(0,-radius),new Vector2(radius,-radius*.5f),new Vector2(radius,radius*.35f),new Vector2(0,radius),new Vector2(-radius,radius*.35f),new Vector2(-radius,-radius*.5f),new Vector2(0,-radius)};
                for(int i=1;i<p.Length;i++)ShopLine(center+p[i-1],center+p[i],color,1.2f);
                ShopLine(center+Vector2.up*(-radius*.5f),center+Vector2.up*(radius*.55f),color,1.1f);return;
            }
            if(form==InfectionForm.Swarm)
            {
                for(int n=-1;n<=1;n++)
                {
                    var o=center+new Vector2(n*radius*.72f,n==0?-radius*.15f:radius*.22f);
                    for(int i=1;i<=9;i++)
                    {float a=(i-1)*Mathf.PI/9,b=i*Mathf.PI/9;ShopLine(o+new Vector2(Mathf.Cos(a),-Mathf.Sin(a))*radius*.63f,o+new Vector2(Mathf.Cos(b),-Mathf.Sin(b))*radius*.63f,color,1.1f);}
                    ShopLine(o+Vector2.left*radius*.63f,o+Vector2.right*radius*.63f,color,1.1f);
                }
                return;
            }
            Vector2 previous=Vector2.zero;
            for(int i=0;i<=16;i++)
            {
                float angle=(i*360/16-90)*Mathf.Deg2Rad;var point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*(i%2==0?1:.48f);
                if(i>0)ShopLine(previous,point,color,1.1f);previous=point;
            }
        }
    }
}
