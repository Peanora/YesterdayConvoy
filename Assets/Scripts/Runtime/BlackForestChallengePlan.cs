using System;
using UnityEngine;

namespace YesterdayConvoy
{
    // Independent encounter defaults; player profiles use the normal Campaign balance.
    public static class BlackForestChallengePlan
    {
        public const int InitialDP=16, TotalDP=50, Defense=6, InitialLevel=8;
        public const string Title="黑流树海 · 三波攻势";

        public static ShopTrialModel.Piece[] StartingPieces()=>new[]{
            new ShopTrialModel.Piece{Id="CARD_M07",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M08",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M15",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M16",Star=1},
            new ShopTrialModel.Piece{Id="CARD_M10",Star=1},
            new ShopTrialModel.Piece{Id="CARD_M05",Star=1},
            new ShopTrialModel.Piece{Id="CARD_M04",Star=1},
            new ShopTrialModel.Piece{Id="CARD_M38",Star=1}
        };

        public static string[] StartingIds=>Array.ConvertAll(StartingPieces(),piece=>piece.Id);

        public static DefenderPlacement[] CopyDefenders()
        {
            var source=Resources.Load<DefenderTrialDefinition>(BlackForestLayout.Resource);
            if(source==null||source.mapId!=BlackForestLayout.Id||source.terrainOnly||source.defenders==null||source.defenders.Length!=9)
                throw new InvalidOperationException("The authored Black Forest six-star encounter is incomplete.");
            _=source.Snapshot();
            // Skills are mutable serialized objects: clone each whole placement on every reset.
            return Array.ConvertAll(source.defenders,guard=>JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(guard)));
        }
    }
}
