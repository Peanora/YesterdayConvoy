using System.Collections;
using UnityEngine;
namespace YesterdayConvoy
{
    [DefaultExecutionOrder(-200)]
    public sealed class BossArenaBootstrap:MonoBehaviour
    {
        public BossArenaDefinition definition;
        void Awake(){FindAnyObjectByType<ManualBattleController>().BindBossArena(definition);}
        IEnumerator Start()
        {
            yield return null;definition.Validate();
            yield return FindAnyObjectByType<ManualBattleController>().PrepareCampaign(true,741,definition.preparation);
        }
        void Update()
        {
            var c=FindAnyObjectByType<ManualBattleController>();
            if(c!=null&&c.BossArenaActive&&c.ShopTrialActive&&Input.GetKeyDown(KeyCode.Tab))c.SetShopVisible(!c.ShopVisible);
        }
    }
}
