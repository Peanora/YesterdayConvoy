using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>A legless body uses the same motion frame, with independent hovering and lean.</summary>
    public sealed class FloatingVisualPresentation : UnitVisualPresentation
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer shadow;
        private float lastTime, movementWeight, lean;
        private bool initialized;
        public Transform Body => body.transform;
        public float MovementWeight => movementWeight;
        public override float MotionWeight => movementWeight;
        public override void ResetVisual() { initialized=false;lastTime=0;movementWeight=lean=0; }
        protected override void Apply(UnitVisualFrame frame)
        {
            if(frame.Time<lastTime) ResetVisual();
            float delta=initialized?Mathf.Max(0,frame.Time-lastTime):Mathf.Max(0,frame.Time);
            movementWeight=Mathf.MoveTowards(movementWeight,frame.Moving?1:0,delta/0.18f);
            lean=Mathf.Lerp(lean,-frame.Direction.x*4*movementWeight,1-Mathf.Exp(-delta/0.08f));
            float breath=Mathf.Sin(frame.Time*Mathf.PI*1.5f);
            body.transform.localPosition=new Vector3(0,0.28f+breath*0.035f,0);
            body.transform.localRotation=Quaternion.Euler(0,0,lean+breath*0.6f);
            body.transform.localScale=new Vector3(1+breath*0.008f,1-breath*0.006f,1);
            body.color=frame.Tint;body.sortingOrder=frame.SortingOrder+1;
            shadow.transform.position=transform.position+Vector3.up*0.012f;
            shadow.transform.rotation=Quaternion.Euler(90,0,0);
            shadow.color=new Color(0,0,0,0.25f-breath*0.025f);shadow.sortingOrder=frame.SortingOrder-1;
            lastTime=frame.Time;initialized=true;
        }
    }
}
