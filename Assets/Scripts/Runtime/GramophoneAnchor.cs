using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class GramophoneAnchor:MonoBehaviour
    {
        public string deviceId;
        [Range(0,100)]public float initialCharge=50;
        [Min(.1f)]public float range=3;
        public GramophoneSettings Snapshot()=>new GramophoneSettings(deviceId,transform.position,initialCharge,range);
    }
}
