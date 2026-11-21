using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float InspectionWidth = 300, InspectionHeight = 224;
        int formationInspectedInstance;
        string formationInspectionCard;
        Vector2 formationInspectionScroll;

        public string FormationInspectedCardId => FormationInspectionAvailable ? formationInspectionCard : null;

        bool FormationInspectionAvailable => Campaign != null && Model.Phase == FormationPhase.Preparation &&
            formationInspectionCard != null && FormationPieceRect(formationInspectedInstance).width > 0;

        Rect FormationInspectionTarget => FormationPieceRect(formationInspectedInstance);

        Rect FormationInspectionRect
        {
            get
            {
                var anchor = FormationInspectionTarget;
                float x = anchor.center.x > 640 ? anchor.xMin - InspectionWidth - 9 : anchor.xMax + 9;
                float y = anchor.yMin >= FormationExperienceRect.yMin - 2 ? anchor.yMin - InspectionHeight - 9 : anchor.yMin + 4;
                return new Rect(Mathf.Clamp(x, 170, 1261 - InspectionWidth),
                    Mathf.Clamp(y, MarketVisible ? FormationMarketRect.yMax + 7 : 87,
                        FormationExperienceRect.yMin - InspectionHeight - 5), InspectionWidth, InspectionHeight);
            }
        }

        bool FormationInspectionBlocksPoint(Vector2 point) =>
            FormationInspectionAvailable && FormationInspectionRect.Contains(point);

        bool CloseFormationInspection()
        {
            if (formationInspectionCard == null) return false;
            CloseFormationEquipmentDetails();
            formationInspectionCard = null;
            formationInspectedInstance = 0;
            formationInspectionScroll = Vector2.zero;
            return true;
        }

        void ToggleFormationPieceInspection(int instance)
        {
            if (Campaign == null) return;
            var piece = Shop.Bench.FirstOrDefault(entry => entry.Instance == instance);
            if (FormationCampaignRules.Find(piece?.Id) == null) return;
            bool wasOpen = formationInspectedInstance == instance;
            CloseFormationInspection();
            if (wasOpen) return;
            OpenFormationInspection(piece.Id);
            formationInspectedInstance = instance;
        }

        void OpenFormationInspection(string id)
        {
            CloseFormationRouteDetails();
            CloseFormationTraitDetails();
            CancelFormationEquipmentInteraction();
            CloseFormationEquipmentDetails();
            formationInspectionCard = id;
        }

        static Rect FormationInspectionEquipmentSlot(Rect panel, int index) =>
            new Rect(panel.x + 52 + index * 30, panel.y + 139, 26, 26);

        internal bool TryFormationInspectedEquipmentSlot(int itemInstance, out Rect slot)
        {
            slot = default;
            if (!FormationInspectionAvailable || formationInspectedInstance <= 0) return false;
            var equipment = Campaign.Equipment(formationInspectedInstance);
            for (int i = 0; i < equipment.Count; i++)
            {
                if (equipment[i].Instance != itemInstance) continue;
                slot = FormationInspectionEquipmentSlot(FormationInspectionRect, i);
                return true;
            }
            return false;
        }

        string FormationInspectionSkill(FormationCampaignRules.UnitDefinition spec) => spec.Id switch
        {
            "TAL" when Campaign.TalulahCost == 4 =>
                "焚城：半径2.5内至多6人，3段各280%攻击力法伤；6秒灼烧，每秒35%。强化后4段并获得20%最大生命护盾8秒。",
            "TAL" when Campaign.TalulahCost == 5 =>
                "火雨：全场至多8人，6段各260%攻击力法伤；8秒灼烧，每秒50%。火雨结束后恢复普攻。三星令本次灼烧的一半转为真伤，并获得整合三星终局奖励。",
            "B21" => SuiClawDescription,
            _ => spec.Effect
        };

        void DrawFormationInspection()
        {
            if (!FormationInspectionAvailable) return;
            string card = formationInspectionCard;
            var spec = FormationCampaignRules.Find(card);
            var piece = Shop.Bench.FirstOrDefault(entry => entry.Instance == formationInspectedInstance);
            var actor = Model.Units.FirstOrDefault(unit => unit.Instance == formationInspectedInstance);
            var anchor = FormationInspectionTarget;
            var r = FormationInspectionRect;
            int star = piece?.Star ?? 1;

            ApprovedUi.Border(anchor, OperationsFocus, 2);
            Fill(r, OperationsSurface);
            ApprovedUi.Border(r, OperationsEdge);
            Fill(new Rect(r.x, r.y, 2, r.height), OperationsFocus);
            string location = actor == null ? "备战席" : "已上阵";
            int cost = Shop.Definition(card).Cost;
            OperationsLabel(new Rect(r.x + 11, r.y + 6, 244, 17), $"{location} · {cost}费 · {spec.Role}", 11, OperationsMuted);
            string stars = piece == null ? "" : "  " + new string('★', piece.Star);
            OperationsLabel(new Rect(r.x + 11, r.y + 22, 245, 26), spec.Name + stars,
                spec.Name.Length <= 10 ? 18 : 15, OperationsInk, true);
            if (OperationsButton(new Rect(r.xMax - 31, r.y + 7, 23, 23), "×"))
            { CloseFormationInspection(); return; }
            OperationsLabel(new Rect(r.x + 11, r.y + 49, 275, 17),
                string.Join(" / ", spec.Factions.Concat(spec.Styles)), 11, OperationsFocus);
            Fill(new Rect(r.x + 11, r.y + 69, r.width - 22, 1), OperationsEdge);

            var portrait = new Rect(r.x + 11, r.y + 76, 62, 55);
            Fill(portrait, new Color32(205, 211, 224, 255));
            DrawFormationPortrait(portrait, card);
            string hp = (actor?.MaxHealth ?? FormationCampaignRules.BaseHealthAtStar(spec, star, Campaign.TalulahCost)).ToString("0");
            string attack = (actor?.Attack ?? spec.Attack * FormationCampaignRules.AttackStarMultiplier(star)).ToString("0");
            string armor = (actor?.Armor ?? spec.Armor).ToString("0");
            string resist = (actor?.MagicResistance ?? spec.MagicResistance).ToString("0");
            OperationsLabel(new Rect(r.x + 83, r.y + 75, 203, 17), $"生命 {hp}   攻击 {attack}", 12, OperationsInk);
            OperationsLabel(new Rect(r.x + 83, r.y + 94, 203, 17), $"护甲 {armor}   法抗 {resist}", 12, OperationsInk);
            OperationsLabel(new Rect(r.x + 83, r.y + 113, 203, 17), spec.RequiredSP <= 0 ? spec.Recovery :
                $"技力 {actor?.SkillCharge ?? spec.InitialSP:0.#}/{actor?.SkillRequired ?? spec.RequiredSP:0.#} · {spec.Recovery}回复",
                11, OperationsMuted);
            var equipped = piece == null ? null : Campaign.Equipment(piece.Instance);
            float offset = 139;
            if (piece != null)
            {
                OperationsLabel(new Rect(r.x + 11, r.y + offset, 40, 26), "装备", 11, OperationsMuted);
                for (int i = 0; i < 3; i++)
                {
                    var slot = FormationInspectionEquipmentSlot(r, i);
                    var item = i < equipped.Count ? equipped[i] : null;
                    DrawEquippedItemSlot(slot, item, item != null && item.Instance == FormationInspectedItemId);
                    if (item != null && TraitHit(slot, "查看 " + item.Name))
                        ToggleFormationEquipmentDetails(item.Instance, slot);
                }
                offset += 32;
            }
            var area = new Rect(r.x + 11, r.y + offset, r.width - 22, r.height - offset - 8);
            var style = new GUIStyle(approvedShop.LoopText(11, TextAnchor.UpperLeft, OperationsInk)){wordWrap=true};
            var description = new GUIContent(FormationInspectionSkill(spec));
            float bodyWidth = area.width - 18;
            float bodyHeight = Mathf.Max(area.height, style.CalcHeight(description, bodyWidth) + 2);
            formationInspectionScroll = GUI.BeginScrollView(area, formationInspectionScroll, new Rect(0, 0, bodyWidth, bodyHeight));
            GUI.Label(new Rect(0, 0, bodyWidth, bodyHeight), description, style);
            GUI.EndScrollView();
        }
    }
}
