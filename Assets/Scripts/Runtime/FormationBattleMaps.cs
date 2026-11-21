using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Selects saved scenery and the matching combat footprint as one board.</summary>
    [DisallowMultipleComponent]
    public sealed class FormationBattleMaps : MonoBehaviour
    {
        public enum Board { Canyon, Street, Hall, Ice }

        public GameObject[] canyonScenery;
        public FormationBattleTerrainAuthoring street, hall, ice;
        public Light sunlight;

        FormationBattleTerrainLayout[] layouts;
        Color canyonLight;
        Quaternion canyonDirection;
        float canyonIntensity;
        public Board ActiveBoard { get; private set; }

        public static Board ForNode(string nodeId, int plane)
        {
            return plane switch
            {
                1 when nodeId != "1-1" && nodeId != "1-2" && nodeId != "1-3" => Board.Street,
                2 => Board.Hall,
                3 => Board.Ice,
                _ => Board.Canyon
            };
        }

        public static string DisplayName(Board board) => board switch
        {
            Board.Street => "叙拉古街庭",
            Board.Hall => "金饰回廊",
            Board.Ice => "蓝冰雪谷",
            _ => "同源石谷"
        };

        public FormationBattleTerrainLayout Select(Board board, FormationBattleTerrainAuthoring canyon)
        {
            if (layouts == null)
            {
                layouts = new FormationBattleTerrainLayout[Enum.GetValues(typeof(Board)).Length];
                canyonLight = sunlight.color;
                canyonDirection = sunlight.transform.rotation;
                canyonIntensity = sunlight.intensity;
            }
            var authoring = board switch { Board.Street => street, Board.Hall => hall, Board.Ice => ice, _ => canyon };
            var layout = layouts[(int)board] ??= authoring.BuildLayout();
            bool showCanyon = board == Board.Canyon;
            foreach (var scenery in canyonScenery) scenery.SetActive(showCanyon);
            street.gameObject.SetActive(board == Board.Street);
            hall.gameObject.SetActive(board == Board.Hall);
            ice.gameObject.SetActive(board == Board.Ice);
            ActiveBoard = board;
            sunlight.color = board switch
            {
                Board.Street => new Color(1f, .95f, .86f),
                Board.Hall => new Color(1f, .91f, .77f),
                Board.Ice => new Color(.83f, .92f, 1f),
                _ => canyonLight
            };
            sunlight.intensity = board switch { Board.Street => .95f, Board.Hall => .88f, Board.Ice => 1.03f, _ => canyonIntensity };
            sunlight.transform.rotation = showCanyon ? canyonDirection :
                Quaternion.LookRotation(new Vector3(.35f, -1f, .40f));
            return layout;
        }
    }
}
