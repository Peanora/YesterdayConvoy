using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Transforms the rendered battlefield only. The logical camera, input and IMGUI remain fixed.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class GuerrillaOrderScreenEffect:MonoBehaviour
    {
        Material material;
        RenderTexture heldFrame;
        bool capturedHold;
        GuerrillaOrderVolley observed;
        public Vector2 Offset{get;private set;}
        public float Zoom{get;private set;}=1;
        public float Roll{get;private set;}
        public bool Holding{get;private set;}
        public bool AssetsReady=>Resources.Load<Shader>("GuerrillaOrder/GuerrillaOrderScreen")?.isSupported??false;

        internal void Present(float age,GuerrillaOrderVolley order,float charge)
        {
            if(!ReferenceEquals(observed,order)){ReleaseHold();observed=order;}
            float small=0,finalAge=-1;
            if(order!=null)foreach(var strike in order.Strikes)
            {
                float since=age-strike.At;
                if(strike.Index<4&&since>=0&&since<.2f)small+=2.8f*Mathf.Exp(-since*22);
                if(strike.Index==4)finalAge=since;
            }
            float recoilAge=finalAge-.05f;
            float recoil=recoilAge>=0&&recoilAge<.55f?Mathf.Exp(-recoilAge*10.5f)*(1-GuerrillaOrderGeometry.Smooth(recoilAge/.55f)):0;
            // Use the same reference-pixel amplitude as the approved dynamic draft.
            Offset=new Vector2(Mathf.Sin(age*126)*small+21*recoil*(Mathf.Sin(recoilAge*48+1.5f)+.22f*Mathf.Sin(recoilAge*101)),
                Mathf.Cos(age*105)*small*.48f+10*recoil*Mathf.Sin(recoilAge*57+.4f));
            Roll=.0032f*recoil*Mathf.Sin(recoilAge*42);
            Zoom=1+.014f*charge+(finalAge>=0&&finalAge<.55f?.036f*(1-GuerrillaOrderGeometry.Smooth(finalAge/.55f)):0);
            if(Mathf.Abs(Offset.x)+Mathf.Abs(Offset.y)>1)Zoom=Mathf.Max(Zoom,1.035f);
            bool hold=finalAge>=0&&finalAge<.05f;
            if(!hold&&Holding)ReleaseHold();
            Holding=hold;
        }
        public void ClearPresentation()
        {
            Offset=Vector2.zero;Zoom=1;Roll=0;Holding=false;observed=null;ReleaseHold();
        }
        /// <summary>The same live postprocess as OnRenderImage, exposed for actual GPU verification.</summary>
        public void ApplyTo(RenderTexture source,RenderTexture destination)
        {
            // Unity passes a null destination for the camera's normal backbuffer.
            if(source==null)throw new System.ArgumentNullException(nameof(source));
            Texture input=source;
            if(Holding)
            {
                if(heldFrame==null||heldFrame.width!=source.width||heldFrame.height!=source.height)
                {
                    ReleaseHold();heldFrame=new RenderTexture(source.width,source.height,0,source.format)
                    {name="Guerrilla final strike / three-frame visual hold",hideFlags=HideFlags.DontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                    heldFrame.Create();
                }
                if(!capturedHold){Graphics.Blit(source,heldFrame);capturedHold=true;}
                input=heldFrame;
            }
            if(Offset==Vector2.zero&&Zoom==1&&Roll==0){Graphics.Blit(input,destination);return;}
            if(material==null)
            {
                var shader=Resources.Load<Shader>("GuerrillaOrder/GuerrillaOrderScreen");
                if(shader==null||!shader.isSupported)throw new System.InvalidOperationException("Missing supported guerrilla battlefield screen shader.");
                material=new Material(shader){name="Guerrilla strike / battlefield recoil",hideFlags=HideFlags.DontSave};
            }
            material.SetVector("_Transform",new Vector4(Offset.x/1920,-Offset.y/1080,Zoom,Roll));
            material.SetFloat("_Aspect",(float)source.width/source.height);
            Graphics.Blit(input,destination,material);
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)=>ApplyTo(source,destination);
        void ReleaseHold()
        {
            capturedHold=false;
            if(heldFrame==null)return;heldFrame.Release();Destroy(heldFrame);heldFrame=null;
        }
        void OnDisable()=>ClearPresentation();
        void OnDestroy(){ClearPresentation();if(material!=null)Destroy(material);}
    }
}
