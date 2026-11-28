using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original procedural icons; no external artwork or runtime file dependencies.</summary>
    static class FormationConsumableArt
    {
        static readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
        public static Texture2D Icon(string key)
        {
            if(icons.TryGetValue(key,out var cached)&&cached!=null)return cached;
            const int size=128;bool copy=key==FormationCampaignRun.CopierKey;
            var accent=copy?new Color(.36f,.85f,1):new Color(1,.74f,.30f);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Vector2 p=new Vector2(x,y);Color c=new Color(.075f,.11f,.14f);
                bool Box(float l,float b,float w,float h)=>x>=l&&x<l+w&&y>=b&&y<b+h;
                if(Box(3,3,122,122))c=Color.Lerp(c,new Color(.20f,.27f,.30f),y/190f);
                if((Box(4,4,120,2)||Box(4,122,120,2)||Box(4,4,2,120)||Box(122,4,2,120)))c=accent*.65f;
                if(copy)
                {
                    if(Box(27,40,55,66))c=accent*.70f;
                    if(Box(31,44,47,58))c=new Color(.10f,.22f,.28f);
                    if(Box(45,22,56,68))c=new Color(.82f,.92f,.94f);
                    if(Box(50,27,46,58))c=new Color(.10f,.23f,.30f);
                    if(Box(57,72,32,4)||Box(57,62,24,3))c=accent;
                    if(Box(70,35,5,20)||Box(62,43,21,5))c=accent;
                }
                else
                {
                    var a=new Vector2(30,29);var b=new Vector2(68,67);var d=b-a;
                    float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
                    if(Vector2.Distance(p,a+t*d)<10)c=new Color(.76f,.82f,.80f);
                    if(Vector2.Distance(p,a+t*d)<5)c=accent;
                    float r=Vector2.Distance(p,new Vector2(77,78));
                    if(r<27&&r>14&&x+y<177)c=accent;
                    if(r<23&&r>18&&x+y<171)c=new Color(.98f,.88f,.61f);
                    if(Vector2.Distance(p,a)<5)c=new Color(.09f,.16f,.19f);
                }
                if((Box(12,12,4,4)||Box(112,112,4,4)))c=accent;
                c.a=1;pixels[y*size+x]=c;
            }
            var icon=new Texture2D(size,size,TextureFormat.RGBA32,false){name=copy?"Convoy copier":"Convoy equipment remover",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            icon.SetPixels(pixels);icon.Apply(false,true);icons[key]=icon;return icon;
        }
    }
    public sealed partial class FormationBattleController
    {
        double consumableUsedAt=-100;
        Rect consumableTarget;
        string consumableUsedKey;
        readonly List<(string Key,int Slot)> consumableFlights=new List<(string,int)>();

        internal ShopTrialModel.Result UseFormationItem(int itemInstance,int pieceInstance,Vector2 origin)
        {
            var item=Campaign?.Inventory.FirstOrDefault(i=>i.Instance==itemInstance);
            string key=item?.Key;
            EnsureOriginalShop();
            var snapshot=approvedShop.CaptureShopFlowSnapshot();
            var worn=Campaign?.Equipment(pieceInstance).ToArray();
            var result=ShopAction(()=>Session.UseItem(itemInstance,pieceInstance),key==FormationCampaignRun.CopierKey,origin);
            if(result.Success&&FormationCampaignRun.IsConsumable(key))
            {
                consumableUsedAt=Time.unscaledTimeAsDouble;consumableUsedKey=key;
                consumableTarget=snapshot.Bounds.TryGetValue(pieceInstance,out var bounds)?bounds:new Rect(origin-Vector2.one*24,Vector2.one*48);
                consumableFlights.Clear();var inventory=Campaign.Inventory.ToArray();
                if(key==FormationCampaignRun.RemoverKey&&worn!=null)
                    foreach(var gear in worn)
                    {int slot=System.Array.FindIndex(inventory,i=>i.Instance==gear.Instance);if(slot>=0)consumableFlights.Add((gear.Key,slot));}
            }
            return result;
        }
        void DrawConsumableFeedback()
        {
            float age=(float)(Time.unscaledTimeAsDouble-consumableUsedAt);
            if(age<0||age>.8f)return;
            float t=age/.8f;Color before=GUI.color;
            GUI.color=new Color(1,1,1,1-t);
            Rect frame=consumableTarget;frame.xMin-=6+10*t;frame.xMax+=6+10*t;
            frame.yMin-=6+10*t;frame.yMax+=6+10*t;
            ApprovedUi.Border(frame,consumableUsedKey==FormationCampaignRun.CopierKey?new Color(.35f,.86f,1):OperationsGold,2);
            if(consumableUsedKey==FormationCampaignRun.RemoverKey)
                foreach(var flight in consumableFlights)
                {
                    Vector2 point=Vector2.Lerp(consumableTarget.center,EquipmentFlightTarget(flight.Slot),Mathf.SmoothStep(0,1,t));
                    point.y-=Mathf.Sin(t*Mathf.PI)*48;
                    DrawSupplyItem(new Rect(point-Vector2.one*19,Vector2.one*38),flight.Key);
                }
            GUI.color=before;
        }
    }
}
