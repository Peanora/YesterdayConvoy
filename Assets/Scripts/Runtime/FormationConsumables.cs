using System;
using System.Linq;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignRun
    {
        public const string CopierKey="复制器", RemoverKey="拆卸器";
        public static bool IsConsumable(string key)=>key==CopierKey||key==RemoverKey;
        public ShopTrialModel.Result UseItem(int itemInstance,int pieceInstance)
        {
            var item=items.FirstOrDefault(i=>i.Instance==itemInstance);
            return item?.Key==CopierKey?UseCopier(itemInstance,pieceInstance):item?.Key==RemoverKey?
                UseRemover(itemInstance,pieceInstance):Equip(itemInstance,pieceInstance);
        }
        public ShopTrialModel.Result UseRemover(int itemInstance,int pieceInstance)
        {
            if(HasBlockingChoice||OpeningRewardsPending)return Fail("请先完成待选事项");
            var item=items.FirstOrDefault(i=>i.Instance==itemInstance&&i.Key==RemoverKey&&i.Owner==0);
            var piece=shop.Bench.FirstOrDefault(p=>p.Instance==pieceInstance);
            if(item==null||piece==null)return Fail("拆卸器或目标不存在");
            var worn=Equipment(pieceInstance).ToArray();
            if(worn.Length==0)return Fail("目标没有装备，拆卸器已保留");
            foreach(var gear in worn)gear.Owner=0;
            items.Remove(item);OnShopChanged();
            return new ShopTrialModel.Result{Success=true,Instance=pieceInstance,Id=piece.Id,Value=worn.Length,
                Message=$"已卸下{shop.Definition(piece.Id).Name}的{worn.Length}件装备，消耗1个拆卸器"};
        }
        void PreserveCopiedMergeEquipment(int[] oldOwners,int survivor)
        {
            var consumed=oldOwners.Where(owner=>!shop.Bench.Any(p=>p.Instance==owner)).ToArray();
            foreach(var gear in items.Where(i=>consumed.Contains(i.Owner)).ToArray())
            {
                gear.Owner=0;
                // Keep legal equipment on the promoted body; excess/duplicate gear returns to inventory.
                Equip(gear.Instance,survivor);
            }
        }
        void DropRewardRemover(string node)
        {
            var payout=new BallPayout();payout.items.Add(RemoverKey);
            pendingBalls.Add(new RewardDrop(++ballSequence,node,"",payout,RemoverKey));
        }
    }
    public sealed partial class FormationShopSession
    {
        public ShopTrialModel.Result UseItem(int itemInstance,int pieceInstance)
        {
            if(!CanShop||Run==null)return Unavailable();
            SavePositions(model);
            var result=Run.UseItem(itemInstance,pieceInstance);
            if(result.Success)Synchronize();
            return result;
        }
    }
}
