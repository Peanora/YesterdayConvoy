using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class FormationCanyonWater : MonoBehaviour
    {
        static readonly int FlowTime = Shader.PropertyToID("_FlowTime");
        [SerializeField] FormationBattleController controller;
        [SerializeField] Renderer[] water;
        MaterialPropertyBlock properties;
        float phase;

        public void Bind(FormationBattleController owner, Renderer[] surfaces)
        {
            controller = owner;
            water = surfaces;
        }

        void LateUpdate()
        {
            if (controller?.Model == null || controller.Model.Paused) return;
            phase += Time.deltaTime;
            properties ??= new MaterialPropertyBlock();
            properties.SetFloat(FlowTime, phase);
            foreach (var surface in water) surface.SetPropertyBlock(properties);
        }
    }
}
