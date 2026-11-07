using System;
using UnityEngine;

namespace YesterdayConvoy
{
    // Independent encounter defaults; player profiles use the normal Campaign balance.
    public static class BlackForestChallengePlan
    {
        public const int InitialDP=50, Defense=6, InitialLevel=8;
        public const string Title="树海 · 六星防线";

        public static ShopTrialModel.Piece[] StartingPieces()=>new[]{
            new ShopTrialModel.Piece{Id="CARD_M18",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M16",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M19",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M06",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M11",Star=3},
            new ShopTrialModel.Piece{Id="CARD_M12",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M13",Star=2},
            new ShopTrialModel.Piece{Id="CARD_M14",Star=1}
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
