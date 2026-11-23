using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float OpeningFirstDispatch = .64f;
        const float OpeningDispatchGap = .15f;
        const float OpeningFlight = .57f;
        const float OpeningSettle = .29f;
        FormationCampaignRun openingDeliveryRun;
        float openingDeliveryAge;

        void TickFormationOpeningDelivery(float deltaTime)
        {
            if (Campaign?.OpeningRewardsPending != true)
            {
                openingDeliveryRun = null;
                openingDeliveryAge = 0;
                return;
            }
            if (openingDeliveryRun != Campaign)
            {
                openingDeliveryRun = Campaign;
                openingDeliveryAge = 0;
                RevealEquipmentSlot(0);
                return;
            }
            if (formationMenuOpen) return;

            openingDeliveryAge += deltaTime;
            float duration = OpeningFirstDispatch + Campaign.OpeningRecruits.Count * OpeningDispatchGap + OpeningFlight + OpeningSettle;
            if (openingDeliveryAge < duration || !Session.CompleteOpeningDelivery()) return;
            notice = "开局补给已发放";
            RefreshCampaignPreparation();
        }

        void DrawFormationOpeningDelivery()
        {
            if (Campaign?.OpeningRewardsPending != true || openingDeliveryRun != Campaign ||
                formationMenuOpen || Event.current.type != EventType.Repaint) return;

            float age = openingDeliveryAge;
            var recruits = Campaign.OpeningRecruits;
            float lastDispatch = OpeningFirstDispatch + recruits.Count * OpeningDispatchGap;
            float panelAlpha = StrategyEaseOut(StrategyProgress(age, .19f)) * (1 - StrategyProgress(age - lastDispatch, .24f));
            var panel = new Rect(414, 238, 452, 167);
            if (panelAlpha > .005f)
            {
                Fill(BattleRect, new Color(.045f, .065f, .07f, .24f * panelAlpha));
                Fill(panel, OpeningAlpha(FormationPreparationPalette.Surface, .96f * panelAlpha));
                ApprovedUi.Border(panel, OpeningAlpha(FormationPreparationPalette.Edge, panelAlpha));
                Fill(new Rect(panel.x, panel.y, panel.width, 2), OpeningAlpha(FormationPreparationPalette.Amber, panelAlpha));
                OperationsLabel(new Rect(panel.x, panel.y + 6, panel.width, 30), "开局补给", 19,
                    OpeningAlpha(FormationPreparationPalette.Text, panelAlpha), true, TextAnchor.MiddleCenter);
            }

            for (int index = 0; index <= recruits.Count; index++)
            {
                bool equipment = index == recruits.Count;
                var recruit = equipment ? null : recruits[index];
                int cost = equipment ? 0 : Shop.Definition(recruit.CardId).Cost;
                var accent = equipment ? FormationPreparationPalette.Amber : FormationFeeColor(cost);
                var origin = new Rect(panel.center.x - recruits.Count * 53 - 46 + index * 106, 277, 92, 112);
                var target = equipment ? EquipmentTileOnScreen(0) : FormationReserveRect(index);
                float appearance = StrategyEaseOut(StrategyProgress(age - .10f - index * .06f, .22f));
                if (appearance <= .005f) continue;

                float flightAge = age - OpeningFirstDispatch - index * OpeningDispatchGap;
                float travel = StrategyProgress(flightAge, OpeningFlight);
                if (travel >= 1)
                {
                    float pulse = 1 - StrategyProgress(flightAge - OpeningFlight, OpeningSettle);
                    Fill(target, OpeningAlpha(accent, .22f * pulse));
                    if (pulse > 0) ApprovedUi.Border(target, OpeningAlpha(Color.Lerp(accent, Color.white, .55f), pulse), 2);
                }

                var point = OpeningFlightPoint(origin.center, target.center, travel, equipment);
                if (travel > 0 && travel < 1)
                {
                    var previous = OpeningFlightPoint(origin.center, target.center, Mathf.Max(0, travel - .13f), equipment);
                    DrawLootStreak(previous, point, OpeningAlpha(accent, Mathf.Sin(travel * Mathf.PI) * .75f), 2);
                }
                var size = Vector2.Lerp(new Vector2(origin.width, origin.height) * Mathf.Lerp(.87f, 1, appearance),
                    new Vector2(equipment ? 42 : 68, equipment ? 40 : 58), travel);
                var token = new Rect(point - size / 2, size);
                var component = equipment ? Campaign.OpeningComponent : null;
                DrawOpeningToken(token, recruit, component, cost, accent, appearance, travel);
            }
        }

        void DrawOpeningToken(Rect rect, FormationCampaignRun.RewardRecruit recruit, string component, int cost,
            Color accent, float alpha, float travel)
        {
            Fill(rect, OpeningAlpha(FormationPreparationPalette.Recess, alpha));
            ApprovedUi.Border(rect, OpeningAlpha(accent, alpha));
            Fill(new Rect(rect.x, rect.y, rect.width, 2), OpeningAlpha(accent, alpha));
            bool showLabel = travel < .45f;
            float size = Mathf.Min(rect.width - 10, rect.height - (showLabel ? 36 : 10));
            var icon = new Rect(rect.center.x - size / 2, rect.y + (showLabel ? 11 : 5), size, size);
            var previous = GUI.color;
            GUI.color = previous * new Color(1, 1, 1, alpha);
            if (recruit != null) ApprovedShopPresentation.DrawTestPortrait(icon, FormationUnitArt.For(recruit.CardId)?.Portrait);
            else DrawSupplyItem(icon, component);
            GUI.color = previous;

            if (!showLabel) return;
            string name = recruit?.Name ?? FormationCampaignRun.ItemName(component);
            var ink = OpeningAlpha(FormationPreparationPalette.Text, alpha * (1 - travel / .45f));
            OperationsLabel(new Rect(rect.x + 3, rect.yMax - 22, rect.width - 6, 21), name,
                name.Length >= 8 ? 9 : 11, ink, true, TextAnchor.MiddleCenter);
            if (recruit != null)
            {
                var tag = new Rect(rect.x + 4, rect.y + 4, 39, 16);
                Fill(tag, OpeningAlpha(FormationPreparationPalette.Recess, .88f * alpha));
                OperationsLabel(tag, $"{cost}费 {recruit.Star}★", 10, ink, true, TextAnchor.MiddleCenter);
            }
        }

        static Vector2 OpeningFlightPoint(Vector2 origin, Vector2 target, float progress, bool equipment)
        {
            var point = Vector2.Lerp(origin, target, Mathf.SmoothStep(0, 1, progress));
            point.y -= Mathf.Sin(progress * Mathf.PI) * (equipment ? 64 : 50);
            return point;
        }

        static Color OpeningAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
    }
}
