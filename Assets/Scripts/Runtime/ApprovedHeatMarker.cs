using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class ApprovedHeatMarker : MonoBehaviour
    {
        public int index;
        Renderer target;
        MaterialPropertyBlock properties;
        public void Apply(ManualBattle battle)
        {
            if(index>=battle.ApprovedHeatPumpCount)return;
            if(target==null)target=GetComponent<Renderer>();if(target==null)return;
            if(properties==null)properties=new MaterialPropertyBlock();
            float state=battle.ApprovedHeatState(index);
            properties.SetColor("_Color",state==2?new Color(1,.26f,.04f):state==1?new Color(1,.65f,.12f):new Color(.2f,.17f,.13f));
            target.SetPropertyBlock(properties);
        }
    }
}
