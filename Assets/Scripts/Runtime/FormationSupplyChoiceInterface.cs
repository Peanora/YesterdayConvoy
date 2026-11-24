using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float SupplyCardWidth = 196.8f, SupplyCardHeight = 279, SupplyCardGap = 14;
        const float SupplyDealGap = .055f, SupplyConfirmDuration = .60f;
        static readonly Color SupplyPaper = new Color32(244, 236, 219, 255);
        static readonly Color SupplyMuted = new Color32(193, 204, 200, 255);
        static readonly Color SupplyGold = new Color32(245, 193, 121, 255);
        static readonly Color SupplyDark = new Color32(20, 29, 34, 255);
        static readonly Color SupplyLine = new Color32(156, 175, 170, 95);
        static readonly Rect SupplyScreen = new Rect(0, 0, 1280, 720);
        static readonly Rect SupplyConfirmRect = new Rect(547, 616, 186, 42);
        static Texture2D supplyDepot, supplyCase, supplyCaseBase, supplyCaseLid, supplyCardGrid;

        IReadOnlyList<FormationCampaignRun.ChoiceOption> shownSupplyOptions;
        FormationCampaignRun.ChoiceOption selectedSupplyOption;
        double supplyOpenedAt;
        double? supplyConfirmedAt;

        static void EnsureSupplyRevealArt()
        {
            if (supplyDepot != null) return;
            const string path = "UI/FormationSupplies/Reveal/";
            supplyDepot = Resources.Load<Texture2D>(path + "depot");
            supplyCase = Resources.Load<Texture2D>(path + "case");
            supplyCaseBase = Resources.Load<Texture2D>(path + "case-base");
            supplyCaseLid = Resources.Load<Texture2D>(path + "case-lid");
            supplyCardGrid = Resources.Load<Texture2D>(path + "card-grid");
        }

        bool DrawSupplyChoice()
        {
            var run = Campaign;
            if (run.SupplyPicksRemaining == 0)
            {
                shownSupplyOptions = null;
                selectedSupplyOption = null;
                supplyConfirmedAt = null;
                return false;
            }

            var options = run.Choices;
            double now = Time.unscaledTimeAsDouble;
            bool extraRecruit = options[0].SupplyComponent == null;
            if (!ReferenceEquals(shownSupplyOptions, options))
            {
                shownSupplyOptions = options;
                selectedSupplyOption = null;
                supplyConfirmedAt = null;
                supplyOpenedAt = extraRecruit ? now - .6 : now;
                EnsureSupplyRevealArt();
            }

            float age = (float)(now - supplyOpenedAt);
            bool confirming = supplyConfirmedAt.HasValue;
            float claim = confirming ? (float)(now - supplyConfirmedAt.Value) : 0;
            bool ready = age >= (extraRecruit ? .9f : 1.12f) + (options.Count - 1) * SupplyDealGap;
            bool enabled = ready && !confirming && GUI.enabled;
            bool repaint = Event.current.type == EventType.Repaint;
            bool confirm = HandleSupplyChoiceKey(options, enabled);
            int selectedIndex = -1;

            if (repaint)
                DrawSupplyBackdrop(run, options.Count, extraRecruit, age, claim);

            for (int i = 0; i < options.Count; i++)
            {
                var card = SupplyCardRect(i, options.Count);
                var option = options[i];
                var hit = new Rect(card.x, card.y, card.width, card.height + (extraRecruit ? 0 : 79));
                if (enabled && GUI.Button(hit, GUIContent.none, GUIStyle.none))
                    selectedSupplyOption = option;
                bool selected = ReferenceEquals(option, selectedSupplyOption);
                if (selected) selectedIndex = i;
                if (repaint && !(confirming && selected))
                    DrawSupplyOption(card, option, run.SupplyEquipmentWithheld, selected,
                        enabled && hit.Contains(Event.current.mousePosition), age - i * SupplyDealGap, confirming, claim);
            }

            if (repaint && confirming && selectedIndex >= 0)
                DrawSupplyOption(SupplyCardRect(selectedIndex, options.Count), options[selectedIndex], run.SupplyEquipmentWithheld,
                    true, false, age - selectedIndex * SupplyDealGap, true, claim);
            if (repaint && !extraRecruit && age < .86f) DrawSupplyUnseal(age);

            bool canConfirm = enabled && selectedIndex >= 0;
            if (canConfirm && GUI.Button(SupplyConfirmRect, GUIContent.none, GUIStyle.none)) confirm = true;
            if (repaint && ready) DrawSupplyActions(canConfirm, selectedSupplyOption, confirming, run.SupplyEquipmentWithheld);
            if (confirm && canConfirm) supplyConfirmedAt = now;
            if (repaint && claim >= SupplyConfirmDuration)
            {
                // The options list can lose one candidate in place after double supply.
                // Look up the selected option by identity before invoking the existing transaction.
                supplyConfirmedAt = null;
                selectedSupplyOption = null;
                if (selectedIndex >= 0) ChooseCampaignOption(selectedIndex);
            }
            return true;
        }

        bool HandleSupplyChoiceKey(IReadOnlyList<FormationCampaignRun.ChoiceOption> options, bool enabled)
        {
            var input = Event.current;
            if (!enabled || input.type != EventType.KeyDown || input.alt || input.control || input.command) return false;
            int index = (int)input.keyCode - (int)KeyCode.Alpha1;
            if (index >= 0 && index < options.Count)
            {
                selectedSupplyOption = options[index];
                input.Use();
            }
            else if (input.keyCode == KeyCode.Return || input.keyCode == KeyCode.KeypadEnter)
            {
                input.Use();
                return selectedSupplyOption != null;
            }
            return false;
        }

        static Rect SupplyCardRect(int index, int count)
        {
            float left = (1280 - count * SupplyCardWidth - (count - 1) * SupplyCardGap) * .5f;
            return new Rect(left + index * (SupplyCardWidth + SupplyCardGap), 212, SupplyCardWidth, SupplyCardHeight);
        }

        void DrawSupplyBackdrop(FormationCampaignRun run, int count, bool extraRecruit, float age, float claim)
        {
            float depot = extraRecruit ? 1 : StrategyEaseOut(StrategyProgress(age - .13f, .52f));
            Fill(SupplyScreen, new Color(.035f, .062f, .078f, Mathf.Lerp(.52f, 1, depot)));
            DrawSupplyTexture(SupplyScreen, supplyDepot, depot);
            Fill(SupplyScreen, new Color(.022f, .040f, .054f, .35f * depot));
            Fill(new Rect(0, 0, 1280, 71), new Color(.045f, .072f, .084f, .76f));
            Fill(new Rect(43, 70, 1194, 1), SupplyLine);
            SupplyLabel(new Rect(43, 23, 21, 27), "◆", 19, SupplyGold, true);
            SupplyLabel(new Rect(75, 23, 105, 27), "昨日圆车", 17, SupplyPaper, true);
            Fill(new Rect(174, 29, 1, 16), SupplyLine);
            SupplyLabel(new Rect(190, 24, 170, 25), "补给节点 " + run.CurrentNode.Id, 13, SupplyMuted);

            Fill(new Rect(551, 102, 74, 1), OpeningAlpha(SupplyGold, .55f));
            Fill(new Rect(655, 102, 74, 1), OpeningAlpha(SupplyGold, .55f));
            SupplyLabel(new Rect(624, 93, 32, 20), "◆", 12, SupplyGold, true, TextAnchor.MiddleCenter);
            string title = extraRecruit ? "入库抽检" : count == 5 && run.SupplyPicksRemaining == 1 ? "补给五选一" : "战地补给";
            SupplyLabel(new Rect(0, 113, 1280, 65), title, 52, SupplyPaper, true, TextAnchor.MiddleCenter);
            string hint = extraRecruit ? "额外领取一张角色卡" :
                run.SupplyPicksRemaining > 1 ? "依次领取两组补给" : "选择一组补给，一并领取角色与装备";
            if (run.SupplyEquipmentWithheld)
                hint = run.SupplyPicksRemaining > 1 ? "依次领取两组补给，首组装备由军械预付抵扣" : "选择一张角色卡，本次装备由军械预付抵扣";
            SupplyLabel(new Rect(0, 178, 1280, 28), hint, 16, SupplyMuted, false, TextAnchor.MiddleCenter);

            float rail = StrategyProgress(age - .42f, .42f) * (1 - StrategyProgress(claim - .27f, .32f));
            Fill(new Rect(96, 592, 1088, 1), OpeningAlpha(SupplyLine, rail));
            Fill(new Rect(96, 590, 18, 4), OpeningAlpha(SupplyGold, rail * .62f));
            Fill(new Rect(1166, 590, 18, 4), OpeningAlpha(SupplyGold, rail * .62f));

        }

        void DrawSupplyUnseal(float age)
        {
            var crate = new Rect(429, 309, 422, 212);
            float lift = StrategyEaseOut(StrategyProgress(age - .10f, .34f));
            float alpha = 1 - StrategyProgress(age - .31f, .24f);
            if (lift <= 0 || supplyCaseLid == null || supplyCaseBase == null)
                DrawSupplyTexture(crate, supplyCase, alpha);
            else
            {
                DrawSupplyTexture(crate, supplyCaseBase, alpha);
                var lid = new Rect(crate.x, crate.y - lift * 46, crate.width, crate.height * (1 - lift * .12f));
                DrawSupplyTexture(lid, supplyCaseLid, alpha);
            }

            float wave = StrategyProgress(age - .13f, .65f);
            float pulse = Mathf.Sin(wave * Mathf.PI) * (1 - wave);
            if (pulse <= .005f) return;
            float width = Mathf.Lerp(20, 1136, StrategyEaseOut(wave));
            for (int i = 0; i < 5; i++)
            {
                float height = 2 + i * 4;
                Fill(new Rect(640 - width / 2, 397 - height / 2, width, height), OpeningAlpha(SupplyGold, pulse * .44f / (i + 1)));
            }
            for (int i = 0; i < 14; i++)
            {
                float offset = (i - 6.5f) * 68;
                float rise = 28 + i % 4 * 25;
                Vector2 point = new Vector2(640 + offset * StrategyEaseOut(wave), 397 - (i % 3 - 1) * rise * wave);
                var color = OpeningAlpha(SupplyGold, pulse * 1.55f);
                DrawLootStreak(point, point + new Vector2((i % 2 == 0 ? -1 : 1) * 7, -4), color, 3 - wave);
            }
        }

        void DrawSupplyOption(Rect card, FormationCampaignRun.ChoiceOption option, bool withheld,
            bool selected, bool hovered, float age, bool confirming, float claim)
        {
            float deal = StrategyProgress(age - .24f, .63f);
            float fold = StrategyProgress(claim, .42f);
            if (deal <= 0 || (confirming && !selected && fold >= 1)) return;

            float spread = Mathf.SmoothStep(0, 1, deal);
            float x = Mathf.Lerp(640 + (card.center.x - 640) * .25f, card.center.x, spread);
            float y = card.y + (1 - StrategyEaseOut(deal)) * 63;
            float scaleX = Mathf.Lerp(.15f, 1, StrategyEaseOut(deal));
            float scaleY = Mathf.Lerp(.82f, 1, StrategyEaseOut(deal));

            if (confirming)
            {
                if (selected)
                {
                    x = Mathf.Lerp(card.center.x, 640, StrategyEaseOut(StrategyProgress(claim, .55f)));
                    scaleX = scaleY = 1 + .035f * fold;
                }
                else
                {
                    x += (card.center.x - 640) * .24f * fold;
                    y += 46 * fold;
                    scaleX *= 1 - fold;
                    scaleY *= 1 - .12f * fold;
                }
            }

            var original = GUI.matrix;
            GUI.matrix = original * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(scaleX, scaleY, 1));
            var face = new Rect(-card.width / 2, 0, card.width, card.height);
            DrawSupplyCardFace(face, option.SupplyUnit, selected, hovered, age);
            float tray = StrategyEaseOut(StrategyProgress(age - .72f, .38f));
            if (tray > .005f && option.SupplyComponent != null)
                DrawSupplyTray(face, option.SupplyComponent, selected, hovered, withheld, tray);
            GUI.matrix = original;
        }

        void DrawSupplyCardFace(Rect face, FormationCampaignRules.UnitDefinition unit, bool selected, bool hovered, float age)
        {
            if (selected)
                ApprovedUi.Border(new Rect(face.x - 4, face.y - 4, face.width + 8, face.height + 8),
                    OpeningAlpha(SupplyGold, .28f), 3);
            int cost = Shop.Definition(unit.CardId).Cost;
            approvedShop.DrawLoopCostBackground(face, cost);
            var art = new Rect(face.x + 3, face.y + 3, face.width - 6, face.height - 59);
            DrawSupplyTexture(art, supplyCardGrid, .85f);
            DrawFormationPortrait(new Rect(art.x + 4, art.y + 21, art.width - 8, art.height - 20), unit.CardId);

            var namePlate = new Rect(face.x + 1, face.yMax - 56, face.width - 2, 55);
            Fill(namePlate, selected ? new Color32(51, 45, 36, 255) : SupplyDark);
            Fill(new Rect(namePlate.x, namePlate.y, namePlate.width, 1), SupplyLine);
            SupplyLabel(new Rect(namePlate.x + 10, namePlate.y + 5, namePlate.width - 20, 27),
                unit.Name, unit.Name.Length > 8 ? 17 : 19, SupplyPaper, true);
            string traits = string.Join(" / ", unit.Factions.Concat(unit.Styles));
            SupplyLabel(new Rect(namePlate.x + 10, namePlate.y + 33, namePlate.width - 20, 17), traits, 11, SupplyMuted);

            var costTag = new Rect(face.x + 11, face.y + 10, 36, 24);
            Fill(costTag, SupplyDark);
            Fill(new Rect(costTag.x, costTag.y, 2, costTag.height), Color.Lerp(ApprovedUi.Tier(cost), Color.white, .55f));
            SupplyLabel(costTag, cost + "费", 14, SupplyPaper, true, TextAnchor.MiddleCenter);
            var star = new Rect(face.xMax - 33, face.y + 9, 23, 23);
            if (selected)
            {
                Fill(star, SupplyGold);
                SupplyLabel(star, "✓", 14, SupplyDark, true, TextAnchor.MiddleCenter);
            }
            else approvedShop.DrawShopStars(star, 1, SupplyGold);

            float scan = StrategyProgress(age - .48f, .47f);
            if (scan > 0 && scan < 1)
            {
                float y = Mathf.Lerp(face.y + 12, face.yMax - 15, scan);
                Fill(new Rect(face.x + 1, y, face.width - 2, 5), OpeningAlpha(SupplyGold, Mathf.Sin(scan * Mathf.PI) * .47f));
            }
            if (selected || hovered) ApprovedUi.Border(face, SupplyGold, selected ? 2 : 1);
        }

        void DrawSupplyTray(Rect face, string component, bool selected, bool hovered, bool withheld, float reveal)
        {
            var tray = new Rect(face.x, face.yMax + 18, face.width, 61);
            var original = GUI.matrix;
            GUI.matrix = original * Matrix4x4.TRS(new Vector3(0, tray.y * (1 - reveal), 0),
                Quaternion.identity, new Vector3(1, Mathf.Max(.02f, reveal), 1));
            Fill(tray, selected ? new Color32(49, 44, 34, 255) : new Color32(27, 37, 40, 255));
            ApprovedUi.Border(tray, selected || hovered ? SupplyGold : SupplyLine, selected ? 2 : 1);
            var equipmentIcon = new Rect(tray.x + 8, tray.y + 6, 48, 48);
            Fill(equipmentIcon, new Color32(16, 24, 29, 255));
            ApprovedUi.Border(equipmentIcon, SupplyLine);
            if (!withheld)
            {
                DrawSupplyItem(new Rect(equipmentIcon.x + 2, equipmentIcon.y + 2, 44, 44), component);
                SupplyLabel(new Rect(tray.x + 66, tray.y + 8, tray.width - 74, 23), FormationCampaignRun.ItemName(component), 15, SupplyPaper, true);
                SupplyLabel(new Rect(tray.x + 66, tray.y + 32, tray.width - 74, 18),
                    FormationCampaignRules.FindRecord(component)?.Effect, 10, SupplyMuted);
            }
            else
            {
                SupplyLabel(equipmentIcon, "—", 17, SupplyMuted, true, TextAnchor.MiddleCenter);
                SupplyLabel(new Rect(tray.x + 66, tray.y + 8, tray.width - 74, 23), "军械预付", 15, SupplyMuted, true);
                SupplyLabel(new Rect(tray.x + 66, tray.y + 32, tray.width - 74, 18), "首组装备已抵扣", 10, SupplyMuted);
            }
            GUI.matrix = original;
            float join = Mathf.Clamp01(reveal * 1.4f);
            Fill(new Rect(face.center.x - 12, face.yMax, 1, 18 * join), OpeningAlpha(SupplyLine, join));
            Fill(new Rect(face.center.x + 12, face.yMax, 1, 18 * join), OpeningAlpha(SupplyLine, join));
            SupplyLabel(new Rect(face.center.x - 10, face.yMax, 20, 18), "+", 14,
                OpeningAlpha(SupplyGold, join), false, TextAnchor.MiddleCenter);
        }

        void DrawSupplyActions(bool enabled, FormationCampaignRun.ChoiceOption option, bool confirming, bool withheld)
        {
            Fill(SupplyConfirmRect, enabled ? SupplyGold : new Color32(38, 52, 57, 255));
            ApprovedUi.Border(SupplyConfirmRect, enabled ? SupplyPaper : SupplyLine);
            SupplyLabel(SupplyConfirmRect, confirming ? "正在领取" : "确认领取", 16,
                enabled ? SupplyDark : SupplyMuted, true, TextAnchor.MiddleCenter);
            if (option == null) return;
            string label = "已选 " + option.SupplyUnit.Name;
            if (option.SupplyComponent != null && !withheld) label += " + " + FormationCampaignRun.ItemName(option.SupplyComponent);
            SupplyLabel(new Rect(0, 663, 1280, 23), label, 12, SupplyMuted, false, TextAnchor.MiddleCenter);
        }

        static void DrawSupplyTexture(Rect rect, Texture texture, float alpha)
        {
            if (texture == null || alpha <= .005f) return;
            var previous = GUI.color;
            GUI.color = previous * new Color(1, 1, 1, alpha);
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        void SupplyLabel(Rect rect, string value, int size, Color color, bool bold = false,
            TextAnchor align = TextAnchor.MiddleLeft) =>
            OperationsLabel(rect, value ?? "", size, color, bold, align);
    }
}
