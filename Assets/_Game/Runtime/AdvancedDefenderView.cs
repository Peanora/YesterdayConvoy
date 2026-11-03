using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Existing passive overlays and native chess actors. Six selected effects have a dedicated visual director.
    public sealed class AdvancedDefenderView:MonoBehaviour
    {
        Func<ManualBattle> source;
        Material material;
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        sealed class ChessVisual { public SpriteRenderer Renderer; public double Created; }
        readonly Dictionary<SixthCell,ChessVisual> chess=new Dictionary<SixthCell,ChessVisual>();
        readonly HashSet<SixthCell> seenChess=new HashSet<SixthCell>();
        readonly List<SixthCell> staleChess=new List<SixthCell>();
        DefenderArt chessArt;
        int used;
        public void Bind(Func<ManualBattle> battle)
        {
            source=battle;
            var effects=GetComponent<DefenderSkillVfxView>()??gameObject.AddComponent<DefenderSkillVfxView>();
            effects.Bind(battle);
        }
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
            used=0;seenChess.Clear();var b=source?.Invoke();var cam=Camera.main;
            if(b!=null&&cam!=null)
            {
                // Snow accumulation is a passive gameplay marker, separate from the short attack burst.
                foreach(var cell in b.SnowCells)if(cell.Layers>0&&b.FindDefender(cell.SourceId).Alive)
                    Ring(cell.Position,.35f,new Color(.6f,.9f,1,.12f+cell.Layers*.035f),.018f);
                foreach(var cell in b.ChessPieces)PresentChess(cell,b.Clock,cam);
                foreach(var d in b.Defenders)
                {
                    if(d.Settings.Skill.Kind==DefenderSkillKind.SilverAshReign&&b.DefenderSkillActive(d)&&b.FindDefender(d.PartnerId) is DefenderState partner&&partner.Alive)
                        Ring(partner.Position,.55f,new Color(.4f,.8f,1,.8f),.07f);
                    if(b.IsFifth(d)&&d.Alive)
                    {Ring(d.Settings.RelayPosition,.42f,new Color(.3f,1,.75f,.7f),.04f);if(b.IsFifthActive(d))Ring(d.Position,.65f,new Color(1,.45f,.2f,.7f),.045f);}
                }
                foreach(var u in b.Units)if(u.Active&&u.ScorchUntil>b.Clock)Ring(u.Motion.Position,.45f,new Color(1,.34f,.08f,.65f),.05f);
            }
            for(int i=used;i<lines.Count;i++)lines[i].enabled=false;
            staleChess.Clear();foreach(var pair in chess)if(!seenChess.Contains(pair.Key))staleChess.Add(pair.Key);
            foreach(var cell in staleChess){Destroy(chess[cell].Renderer.gameObject);chess.Remove(cell);}
        }
        void PresentChess(SixthCell cell,double clock,Camera cam)
        {
            seenChess.Add(cell);
            if(chessArt==null)chessArt=DefenderArt.Load("D28Chess");
            if(!chess.TryGetValue(cell,out var visual))
            {
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));
                var r=new GameObject("Wang original chess piece",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(transform,false);r.sharedMaterial=material;
                visual=new ChessVisual { Renderer=r,Created=clock };chess.Add(cell,visual);
            }
            var renderer=visual.Renderer;double age=clock-visual.Created;
            renderer.enabled=cell.Time==0||clock-cell.Time<chessArt["attack"].Duration;
            renderer.sprite=cell.Time>0?chessArt["attack"].At(clock-cell.Time,false):chessArt.StartAt(age)??chessArt.IdleAt(age-chessArt.StartDuration);
            renderer.transform.SetPositionAndRotation(cell.Position,cam.transform.rotation);
            renderer.transform.localScale=Vector3.one;
            renderer.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,cell.Position)+28;
            renderer.color=cell.Active?Color.white:new Color(.65f,.7f,.78f,.8f);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
