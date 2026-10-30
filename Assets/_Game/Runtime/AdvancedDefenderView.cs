using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Project-authored geometric VFX, driven only by the simulation clock. No damage here.
    public sealed class AdvancedDefenderView:MonoBehaviour
    {
        Func<ManualBattle> source;
        Material material;
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        readonly List<SpriteRenderer> flames=new List<SpriteRenderer>();
        readonly Sprite[] flameFrames=new Sprite[25];
        int used,flamesUsed;
        public void Bind(Func<ManualBattle> battle)=>source=battle;
        LineRenderer Line(string name,Color color,float width,int points)
        {
            if(used==lines.Count)
            {
                var l=new GameObject(name).AddComponent<LineRenderer>();l.transform.SetParent(transform,false);
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));
                l.sharedMaterial=material;l.useWorldSpace=true;l.numCapVertices=3;lines.Add(l);
            }
            var line=lines[used++];line.enabled=true;line.loop=false;line.startColor=line.endColor=color;
            line.widthMultiplier=width;line.positionCount=points;line.sortingOrder=60;return line;
        }
        void Ring(Vector3 p,float radius,Color color,float width)
        {
            var line=Line("Skill field",color,width,65);
            for(int i=0;i<=64;i++){float a=i*Mathf.PI/32;line.SetPosition(i,p+new Vector3(Mathf.Cos(a)*radius,.06f,Mathf.Sin(a)*radius));}
        }
        void LateUpdate()
        {
            used=flamesUsed=0;var b=source?.Invoke();var cam=Camera.main;
            if(b!=null&&cam!=null)
            {
                float t=(float)b.Clock;
                foreach(var cell in b.SnowCells)if(cell.Layers>0&&b.FindDefender(cell.SourceId).Alive)Ring(cell.Position,.35f,new Color(.6f,.9f,1,.3f+cell.Layers*.1f),.025f);
                foreach(var cell in b.ChessPieces)
                    if(cell.Time==0)Ring(cell.Position,.25f,cell.Active?new Color(.9f,.9f,.75f,.9f):new Color(.45f,.5f,.6f,.5f),.07f);
                    else if(b.Clock-cell.Time<.4)Ring(cell.Position,.25f+(float)(b.Clock-cell.Time)*2,new Color(1,.8f,.3f,.8f),.07f);
                foreach(var d in b.Defenders)if(d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign&&b.DefenderSkillActive(d)&&b.FindDefender(d.PartnerId) is DefenderState partner&&partner.Alive)Ring(partner.Position,.55f,new Color(.4f,.8f,1,.8f),.07f);
                foreach(var v in b.DefenderDrones)
                {
                    var p=v.Position+cam.transform.up*.7f;var right=cam.transform.right;var up=cam.transform.up;
                    var wolf=Line("Pursuit wolf silhouette",new Color(.75f,.48f,1,.9f),.065f,9);
                    Vector2[] shape={new Vector2(-.3f,0),new Vector2(-.22f,.22f),new Vector2(-.12f,.1f),new Vector2(0,.15f),new Vector2(.12f,.1f),new Vector2(.22f,.22f),new Vector2(.3f,0),new Vector2(0,-.13f),new Vector2(-.3f,0)};
                    for(int i=0;i<shape.Length;i++)wolf.SetPosition(i,p+right*shape[i].x+up*shape[i].y);
                    Ring(v.Position,.3f+Mathf.Sin(t*5+v.Index)*.04f,new Color(.6f,.25f,1,.4f),.025f);
                    var trail=Line("Pursuit wake",new Color(.55f,.25f,1,.35f),.09f,8);
                    for(int i=0;i<8;i++)trail.SetPosition(i,p-right*(i*.11f)+up*(Mathf.Sin(t*9-i*.6f)*.04f));
                }
                foreach(var d in b.Defenders)
                {
                    if(b.IsFifth(d)&&d.Alive){Ring(d.Settings.RelayPosition,.42f,new Color(.3f,1,.75f,.7f),.04f);if(b.IsFifthActive(d))Ring(d.Position,.65f,new Color(1,.45f,.2f,.7f),.045f);}
                    if(!b.DefenderSkillActive(d))continue;
                    switch(d.Settings.Skill.Kind)
                    {
                        case DefenderSkillKind.YuKitchen:
                            if(!b.DefenderWallActive(d))break;
                            Vector3 center=b.DefenderWallPoint(d),axis=Vector3.Cross(d.WallNormal,Vector3.up);
                            // Whole battle-line wall, broken into layered, flickering flame tongues.
                            for(int k=-12;k<=12;k++)
                            {
                                var p=center+axis*(k*.48f);float height=.45f+.5f*(.5f+.5f*Mathf.Sin(t*11+k*2.3f));
                                if(flameFrames[0]==null)
                                {
                                    var texture=Resources.Load<Texture2D>("FireRainEmbers");
                                    for(int f=0;f<25;f++)flameFrames[f]=Sprite.Create(texture,new Rect(f%5*128,(4-f/5)*128,128,128),new Vector2(.5f,0),128);
                                }
                                if(flamesUsed==flames.Count)
                                {
                                    var sprite=new GameObject("CC0 animated wall flame",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();sprite.transform.SetParent(transform,false);flames.Add(sprite);
                                }
                                var flameSprite=flames[flamesUsed++];flameSprite.enabled=true;flameSprite.sprite=flameFrames[((int)(t*20)+k*3+100000)%25];
                                flameSprite.transform.SetPositionAndRotation(p,cam.transform.rotation);flameSprite.transform.localScale=new Vector3(.8f,height*1.5f,1);
                                flameSprite.color=new Color(1,.75f,.4f,.8f);flameSprite.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,p)+35;
                                for(int layer=0;layer<2;layer++)
                                {
                                    var flame=Line("Yu fire wall",layer==0?new Color(1,.22f,.025f,.68f):new Color(1,.84f,.4f,.9f),layer==0?.16f:.055f,5);
                                    for(int j=0;j<5;j++){float f=j/4f;flame.SetPosition(j,p+Vector3.up*(height*f)+axis*(Mathf.Sin(t*14+k+j)*.1f*f));}
                                }
                            }
                            break;
                        case DefenderSkillKind.SariaCalcification:
                            Ring(d.Position,d.Settings.Skill.EffectRadius,new Color(.4f,.85f,1,.42f),.035f);
                            for(int k=0;k<8;k++)
                            {
                                float a=k*Mathf.PI/4+t*.25f;var p=d.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*d.Settings.Skill.EffectRadius;
                                var bar=Line("Calcification column",new Color(.65f,.94f,1,.45f),.035f,2);bar.SetPosition(0,p);bar.SetPosition(1,p+Vector3.up*(.2f+.1f*Mathf.Sin(t*2+k)));
                            }
                            break;
                        case DefenderSkillKind.MlynarGlory:
                            Ring(d.Position,.65f,new Color(1,.77f,.25f,.5f),.025f);
                            float age=(float)(b.Clock-d.LastActionTime);
                            if(age<0||age>.4f)break;
                            var slash=Line("Golden liberator arc",new Color(1,.86f,.48f,1-age/.4f),.09f,20);
                            for(int k=0;k<20;k++){float a=(-.8f+k/19f*2.2f)+age*3;slash.SetPosition(k,d.Position+cam.transform.up*.7f+(cam.transform.right*Mathf.Cos(a)+cam.transform.up*Mathf.Sin(a))*(.8f+age));}
                            break;
                    }
                }
                foreach(var u in b.Units)
                    if(u.Active&&u.ScorchUntil>b.Clock)Ring(u.Motion.Position,.45f,new Color(1,.34f,.08f,.65f),.05f);
            }
            for(int i=used;i<lines.Count;i++)lines[i].enabled=false;
            for(int i=flamesUsed;i<flames.Count;i++)flames[i].enabled=false;
        }
        void OnDestroy(){if(material!=null)Destroy(material);foreach(var frame in flameFrames)if(frame!=null)Destroy(frame);}
    }
}
