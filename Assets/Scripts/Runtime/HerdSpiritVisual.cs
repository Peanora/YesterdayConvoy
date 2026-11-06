using System;
using UnityEngine;

namespace YesterdayConvoy
{
    // A shared visual state over each unit's own frames, not a replacement character.
    public sealed class HerdSpiritVisual : MonoBehaviour
    {
        static Material bodyMaterial,mistMaterial;
        SpriteRenderer body;
        ManualBattle battle;
        DeployedUnitState unit;
        Func<double> clock;
        MeshRenderer mist;
        Mesh mesh;
        MaterialPropertyBlock properties;
        float height;
        bool grounded;
        readonly Vector3[] vertices=new Vector3[3*12*4+6*4];
        readonly Color[] colors=new Color[3*12*4+6*4];
        public MeshRenderer MistRenderer=>mist;
        public float Opacity {get;private set;}

        public void Bind(SpriteRenderer renderer,ManualBattle model,DeployedUnitState state,float bodyHeight,bool worldUpright,Func<double> presentationClock)
        {
            body=renderer;battle=model;unit=state;height=bodyHeight;grounded=worldUpright;clock=presentationClock;
            var shader=Resources.Load<Shader>("HerdSpirit");
            if(shader==null)throw new InvalidOperationException("Herd spirit shader is missing.");
            if(bodyMaterial==null)bodyMaterial=new Material(shader){name="Herd spirit body"};
            if(mistMaterial==null){mistMaterial=new Material(shader){name="Herd spirit wisps"};mistMaterial.SetFloat("_WispOnly",1);}
            body.sharedMaterial=bodyMaterial;properties=new MaterialPropertyBlock();
            mist=new GameObject("Herd spirit ankle wisps",typeof(MeshFilter),typeof(MeshRenderer)).GetComponent<MeshRenderer>();
            mist.transform.SetParent(transform,false);mist.sharedMaterial=mistMaterial;
            mesh=new Mesh{name="Clock driven spirit ribbons"};mesh.MarkDynamic();
            var indices=new int[vertices.Length/4*6];
            for(int q=0;q<vertices.Length/4;q++){int v=q*4,t=q*6;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;}
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=indices;mist.GetComponent<MeshFilter>().sharedMesh=mesh;
        }

        public void Present()
        {
            if(body==null||unit==null)return;
            double now=clock?.Invoke()??battle.Clock;
            var spirit=battle.HerdSpiritFor(unit.EntityId);if(spirit==null)return;
            bool rising=battle.Clock<spirit.ReadyAt;
            float emergence=rising?Mathf.Clamp01((float)((battle.Clock-spirit.StartedAt-HerdSpiritState.FallDuration)/HerdSpiritState.RiseDuration)):1;
            float terminal=unit.Resolution==UnitResolution.Dead?1-Mathf.Clamp01((float)((now-unit.DeathTime)/.45)):1;
            float lifetime=!rising?Mathf.Clamp01((float)battle.HerdRemaining(unit.EntityId)/.8f):1;
            Opacity=terminal*lifetime*(rising?Mathf.Lerp(.18f,1,emergence):1);
            var grounding=body.GetComponent<ActorGrounding>();
            if(grounding!=null&&grounding.Shadow!=null)grounding.Shadow.color=new Color(.1f,.5f,.65f,.065f*Opacity);
            float time=(float)(now-spirit.StartedAt);
            body.GetPropertyBlock(properties);
            properties.SetFloat("_SpiritTime",time);properties.SetFloat("_SpiritOpacity",Opacity);
            properties.SetFloat("_Grounded",grounded?1:0);properties.SetFloat("_BodyHeight",height);
            if(body.sprite!=null)
            {
                Rect rect=body.sprite.textureRect;var texture=body.sprite.texture;
                properties.SetVector("_SpriteUVRect",new Vector4(rect.xMin/texture.width,rect.yMin/texture.height,rect.xMax/texture.width,rect.yMax/texture.height));
            }
            body.SetPropertyBlock(properties);
            mist.enabled=body.enabled&&Opacity>.001f;
            mist.sortingLayerID=body.sortingLayerID;mist.sortingOrder=body.sortingOrder+1;
            mist.GetPropertyBlock(properties);properties.SetFloat("_Grounded",grounded?1:0);properties.SetFloat("_BodyHeight",height);mist.SetPropertyBlock(properties);
            BuildWisps(time,Opacity);
        }

        void BuildWisps(float time,float opacity)
        {
            int index=0;
            for(int ribbon=0;ribbon<3;ribbon++)for(int segment=0;segment<12;segment++)
            {
                float a=segment/12f,b=(segment+1)/12f;
                Vector3 p=Ribbon(ribbon,a,time),q=Ribbon(ribbon,b,time);
                float width=.014f*height*Mathf.Sin((a+.03f)*Mathf.PI);
                Quad(ref index,p-Vector3.right*width,p+Vector3.right*width,q+Vector3.right*width,q-Vector3.right*width,
                    new Color(.33f,.83f,1,.17f*opacity*Mathf.Sin((a+.03f)*Mathf.PI)));
            }
            for(int particle=0;particle<6;particle++)
            {
                float phase=Mathf.Repeat(time*.32f+particle*.173f,1),angle=particle*2.4f;
                var p=new Vector3((Mathf.Sin(angle)+Mathf.Sin(time+angle)*.18f)*height*.22f,height*(.025f+phase*.4f),-.001f);
                float size=height*.007f;Quad(ref index,p+Vector3.up*size,p+Vector3.right*size,p-Vector3.up*size,p-Vector3.right*size,
                    new Color(.55f,.92f,1,Mathf.Sin(phase*Mathf.PI)*opacity*.55f));
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
        }
        Vector3 Ribbon(int ribbon,float along,float time)
        {
            float side=ribbon==0?-1:ribbon==1?1:0;
            return new Vector3(height*(side*.23f+Mathf.Sin(along*7-time*1.8f+ribbon*2)*.045f),height*(.015f+along*(ribbon==2?.22f:.33f)),0);
        }
        void Quad(ref int i,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
        {vertices[i]=a;vertices[i+1]=b;vertices[i+2]=c;vertices[i+3]=d;for(int n=0;n<4;n++)colors[i+n]=color;i+=4;}
        void OnDisable(){if(mist!=null)mist.enabled=false;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
