using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        sealed class DecisionRequest
        {
            public string Key, Title, Subtitle, Footer;
            public IReadOnlyList<DecisionOption> Options;
            public Func<int, bool> Confirm;
            public Action Close;
            public bool Navigation, Back;
        }

        sealed class DecisionOption
        {
            public string Name, Outcome, Description, Icon, ArtId, ItemId;
            public int Cost;
            public bool Current, Disabled;
        }

        const float DecisionTop = 77, DecisionHeight = 296, DecisionScrollInset = 18;
        const float DecisionOpenSeconds = .26f, DecisionCloseSeconds = .18f;
        const float DecisionCardGap = 10, DecisionNavigationGap = 7;
        static readonly Rect DecisionPanel = new Rect(270, DecisionTop, 740, DecisionHeight);
        static readonly Rect DecisionOptions = new Rect(288, 157, 704, 158);
        internal static readonly Rect FormationDecisionConfirmRect = new Rect(854, 329, 138, 33);
        static readonly Color DecisionSurface = new Color32(43, 42, 37, 255);
        static readonly Color DecisionCardSurface = new Color32(52, 50, 43, 255);
        static readonly Color DecisionPaper = new Color32(240, 228, 205, 255);
        static readonly Color DecisionMuted = new Color32(190, 182, 163, 255);
        static readonly Color DecisionLine = new Color32(124, 111, 85, 255);
        static readonly Color DecisionAccent = new Color32(239, 186, 106, 255);

        DecisionRequest shownFormationDecision;
        int selectedFormationDecision = -1;
        Vector2 formationDecisionScroll;
        string formationDecisionError;
        double formationDecisionOpenedAt;
        double? formationDecisionClosingAt;

        internal bool FormationDecisionVisible => shownFormationDecision != null && !formationDecisionClosingAt.HasValue;
        internal int FormationDecisionSelectedIndex => selectedFormationDecision;
        internal string FormationDecisionKey => FormationDecisionVisible ? shownFormationDecision.Key : null;
        bool FormationDecisionClosing => formationDecisionClosingAt.HasValue &&
            Time.unscaledTimeAsDouble - formationDecisionClosingAt.Value < DecisionCloseSeconds;

        internal static Rect FormationDecisionCardRect(int index, int columns)
        {
            var local = DecisionCardRect(index, columns, DecisionOptions.width, 151, DecisionCardGap);
            local.position += DecisionOptions.position;
            return local;
        }

        internal Rect FormationDecisionOptionRect(int index)
        {
            if (!FormationDecisionVisible || (uint)index >= (uint)shownFormationDecision.Options.Count) return Rect.zero;
            var request = shownFormationDecision;
            int columns = DecisionColumns(request);
            float height = request.Navigation ? 34 : 151;
            float gap = request.Navigation ? DecisionNavigationGap : DecisionCardGap;
            bool scroll = DecisionContentHeight(request.Options.Count, columns, height, gap) > DecisionOptions.height;
            var local = DecisionCardRect(index, columns, DecisionOptions.width - (scroll ? DecisionScrollInset : 0), height, gap);
            local.position += DecisionOptions.position - formationDecisionScroll;
            float top = Mathf.Max(local.yMin, DecisionOptions.yMin);
            float bottom = Mathf.Min(local.yMax, DecisionOptions.yMax);
            return bottom > top ? Rect.MinMaxRect(local.xMin, top, local.xMax, bottom) : Rect.zero;
        }

        internal void RevealFormationDecisionOption(int index)
        {
            var request=shownFormationDecision;
            if(!FormationDecisionVisible||request==null||(uint)index>=(uint)request.Options.Count)return;
            int columns=DecisionColumns(request);float height=request.Navigation?34:151;
            float gap=request.Navigation?DecisionNavigationGap:DecisionCardGap;
            float content=DecisionContentHeight(request.Options.Count,columns,height,gap);
            formationDecisionScroll.y=Mathf.Clamp(index/columns*(height+gap),0,Mathf.Max(0,content-DecisionOptions.height));
        }

        static float DecisionContentHeight(int count, int columns, float cardHeight, float gap) =>
            Mathf.Ceil(count / (float)columns) * (cardHeight + gap) - gap;

        static Rect DecisionCardRect(int index, int columns, float availableWidth, float height, float gap)
        {
            float width = (availableWidth - (columns - 1) * gap) / columns;
            return new Rect(index % columns * (width + gap), index / columns * (height + gap), width, height);
        }

        DecisionRequest BuildFormationDecision()
        {
            var run = Campaign;
            if (run == null || CombatViewActive || CampaignSettlementKeepsBattleView || FormationBoardUnfoldActive)
                return null;

            bool required = run.HasBlockingChoice;
            if (required)
            {
                var options = run.Choices;
                if (options.Count == 0 || options[0].Strategy != null || run.SupplyPicksRemaining > 0)
                    return null;
                int tallulahFee = run.PendingTalulahChoiceFee;
                return new DecisionRequest
                {
                    Key = "required:" + tallulahFee + ":" + run.ChoiceTitle,
                    Title = run.ChoiceTitle,
                    Subtitle = tallulahFee > 0 ? $"当前 · {tallulahFee}费二星" : null,
                    Footer = "确认后不可撤回",
                    Options = options.Select(CampaignDecisionOption).ToArray(),
                    Confirm = index => ChooseCampaignOption(index).Success
                };
            }

            if (campaignRewardChoiceOpen && run.PendingRewardChoices > 0)
                return new DecisionRequest
                {
                    Key = "reward:" + run.RewardChoiceTitle,
                    Title = run.RewardChoiceTitle,
                    Footer = "可稍后选择",
                    Options = run.RewardChoices.Select(CampaignDecisionOption).ToArray(),
                    Confirm = index => ChooseGroundRewardOption(index).Success,
                    Close = CloseCampaignPanels
                };

            return campaignFormationOpen && Model.Phase == FormationPhase.Preparation
                ? BuildFormationTacticalDecision()
                : null;
        }

        DecisionOption CampaignDecisionOption(FormationCampaignRun.ChoiceOption option)
        {
            var unit = FormationCampaignRules.Find(option.Id);
            return new DecisionOption
            {
                Name = option.Name,
                Outcome = option.Outcome,
                Description = option.Description,
                Icon = option.Icon,
                Cost = option.Cost,
                ArtId = unit?.CardId,
                ItemId = unit == null && SupplyItemArt(option.Id) != null ? option.Id : null
            };
        }

        void ResetFormationDecisionPresentation()
        {
            shownFormationDecision = null;
            formationDecisionClosingAt = null;
            selectedFormationDecision = -1;
            formationDecisionError = null;
            formationDecisionScroll = Vector2.zero;
        }

        void BeginFormationDecisionClose()
        {
            if (shownFormationDecision == null || formationDecisionClosingAt.HasValue) return;
            formationDecisionClosingAt = Time.unscaledTimeAsDouble;
        }

        internal bool TryDismissFormationDecision()
        {
            var request = BuildFormationDecision();
            if (request == null) return FormationDecisionClosing;
            if (request.Close != null)
            {
                request.Close();
                shopInputBlockedFrame = Time.frameCount;
                if (BuildFormationDecision() == null) BeginFormationDecisionClose();
            }
            return true;
        }

        bool DrawFormationDecision()
        {
            var request = BuildFormationDecision();
            double now = Time.unscaledTimeAsDouble;
            if (request == null)
            {
                if (shownFormationDecision == null) return false;
                BeginFormationDecisionClose();
                if (!FormationDecisionClosing)
                {
                    ResetFormationDecisionPresentation();
                    return false;
                }
            }
            else
            {
                if (shownFormationDecision == null || formationDecisionClosingAt.HasValue)
                    formationDecisionOpenedAt = now;
                if (shownFormationDecision?.Key != request.Key)
                {
                    selectedFormationDecision = -1;
                    formationDecisionError = null;
                    formationDecisionScroll = Vector2.zero;
                }
                shownFormationDecision = request;
                formationDecisionClosingAt = null;
            }

            float open = Mathf.Clamp01((float)(now - formationDecisionOpenedAt) / DecisionOpenSeconds);
            float close = formationDecisionClosingAt.HasValue
                ? Mathf.Clamp01((float)(now - formationDecisionClosingAt.Value) / DecisionCloseSeconds)
                : 0;
            float reveal = (1 - Mathf.Pow(1 - open, 3)) * (1 - close * close);
            DrawFormationDecisionPanel(shownFormationDecision, reveal, request != null && open >= 1);
            return true;
        }

        void DrawFormationDecisionPanel(DecisionRequest request, float reveal, bool ready)
        {
            var input = Event.current;
            bool repaint = input.type == EventType.Repaint;
            bool enabled = GUI.enabled;
            bool canInteract = enabled && ready && shopInputBlockedFrame != Time.frameCount;
            if (repaint)
                Fill(new Rect(0, DecisionTop, 1280, 720 - DecisionTop), new Color(0, 0, 0, .18f * reveal));

            float offset = -DecisionHeight * (1 - reveal);
            int hit = -1;
            bool close = false;
            bool confirm = HandleDecisionKey(request, canInteract, out hit);
            GUI.BeginGroup(new Rect(0, DecisionTop, 1280, DecisionHeight + 12));
            GUI.BeginGroup(new Rect(0, offset, 1280, DecisionHeight + 12));
            var panel = DecisionLocal(DecisionPanel);
            var view = DecisionLocal(DecisionOptions);

            if (repaint) DrawDecisionShell(panel, request);

            var closeButton = new Rect(panel.xMax - 44, 34, 29, 28);
            if (request.Close != null)
            {
                if (repaint) DrawDecisionAction(closeButton, request.Back ? "‹" : "×", canInteract, false);
                if (canInteract) close = GUI.Button(closeButton, GUIContent.none, GUIStyle.none);
            }

            int columns = DecisionColumns(request);
            float cardHeight = request.Navigation ? 34 : 151;
            float gap = request.Navigation ? DecisionNavigationGap : DecisionCardGap;
            float contentHeight = DecisionContentHeight(request.Options.Count, columns, cardHeight, gap);
            bool scroll = contentHeight > view.height;
            float cardWidth = view.width - (scroll ? DecisionScrollInset : 0);
            GUI.enabled = canInteract;
            if (scroll)
                formationDecisionScroll = ApprovedUi.BeginDragScrollView(view, formationDecisionScroll,
                    new Rect(0, 0, cardWidth, contentHeight));
            else GUI.BeginGroup(view);

            for (int index = 0; index < request.Options.Count; index++)
            {
                var option = request.Options[index];
                var rect = DecisionCardRect(index, columns, cardWidth, cardHeight, gap);
                bool interactive = canInteract && !option.Disabled;
                bool visible = !scroll || rect.yMax > formationDecisionScroll.y &&
                    rect.yMin < formationDecisionScroll.y + view.height;
                if (repaint && visible)
                    DrawDecisionCard(rect, option, request.Navigation, index == selectedFormationDecision,
                        interactive && rect.Contains(input.mousePosition));
                if (interactive && GUI.Button(rect, GUIContent.none, GUIStyle.none)) hit = index;
            }

            if (scroll) GUI.EndScrollView();
            else GUI.EndGroup();

            var confirmButton = DecisionLocal(FormationDecisionConfirmRect);
            bool canConfirm = canInteract && selectedFormationDecision >= 0 &&
                selectedFormationDecision < request.Options.Count && !request.Options[selectedFormationDecision].Disabled;
            if (!request.Navigation)
            {
                if (repaint)
                {
                    string footer = formationDecisionError ?? request.Footer;
                    if (!string.IsNullOrEmpty(footer))
                        DecisionLabel(new Rect(panel.x + 18, confirmButton.y, confirmButton.x - panel.x - 29, confirmButton.height),
                            footer, 12, formationDecisionError == null ? DecisionMuted : new Color32(255, 171, 143, 255),
                            TextAnchor.MiddleRight, wrap: true);
                    DrawDecisionAction(confirmButton, "确认选择", canConfirm, true);
                }
                if (canConfirm && GUI.Button(confirmButton, GUIContent.none, GUIStyle.none)) confirm = true;
            }
            GUI.enabled = enabled;
            GUI.EndGroup();
            GUI.EndGroup();

            if (close)
            {
                TryDismissFormationDecision();
                return;
            }
            if (hit >= 0 && SelectDecisionCard(request, hit) && !request.Navigation && scroll &&
                input.rawType == EventType.KeyDown)
            {
                float y = DecisionCardRect(hit, columns, cardWidth, cardHeight, gap).y;
                formationDecisionScroll.y = Mathf.Clamp(y, 0, contentHeight - view.height);
            }
            if (confirm && canConfirm) ApplyDecisionCard(request);
        }

        internal string FormationDecisionInputState => $"key={FormationDecisionKey}; request={BuildFormationDecision()?.Key}; menu={formationMenuOpen}; blockedFrame={shopInputBlockedFrame==Time.frameCount}; openAge={Time.unscaledTimeAsDouble-formationDecisionOpenedAt:0.000}";
        internal bool ActivateFormationDecisionCard(int index) => SelectDecisionCard(InteractiveFormationDecision(), index);
        internal bool ConfirmFormationDecisionCard() => ApplyDecisionCard(InteractiveFormationDecision());

        DecisionRequest InteractiveFormationDecision()
        {
            if (!FormationDecisionVisible || formationMenuOpen || shopInputBlockedFrame == Time.frameCount ||
                Time.unscaledTimeAsDouble - formationDecisionOpenedAt < DecisionOpenSeconds) return null;
            var request = BuildFormationDecision();
            return request?.Key == shownFormationDecision.Key ? request : null;
        }

        bool SelectDecisionCard(DecisionRequest request, int index)
        {
            if (request == null || (uint)index >= (uint)request.Options.Count || request.Options[index].Disabled)
                return false;
            selectedFormationDecision = index;
            formationDecisionError = null;
            return !request.Navigation || ApplyDecisionCard(request);
        }

        bool ApplyDecisionCard(DecisionRequest request)
        {
            if (request == null || (uint)selectedFormationDecision >= (uint)request.Options.Count ||
                request.Options[selectedFormationDecision].Disabled) return false;
            if (!request.Confirm(selectedFormationDecision))
            {
                formationDecisionError = notice;
                return false;
            }

            selectedFormationDecision = -1;
            formationDecisionError = null;
            formationDecisionScroll = Vector2.zero;
            shopInputBlockedFrame = Time.frameCount;
            if (BuildFormationDecision() == null) BeginFormationDecisionClose();
            return true;
        }

        static Rect DecisionLocal(Rect rect)
        {
            rect.y -= DecisionTop;
            return rect;
        }

        static int DecisionColumns(DecisionRequest request) => request.Navigation ? 3 : Mathf.Clamp(request.Options.Count, 1, 3);

        bool HandleDecisionKey(DecisionRequest request, bool enabled, out int hit)
        {
            hit = -1;
            var input = Event.current;
            if (!enabled || input.type != EventType.KeyDown || input.alt || input.control || input.command)
                return false;
            var key = input.keyCode;
            int index = key >= KeyCode.Keypad1 && key <= KeyCode.Keypad9
                ? key - KeyCode.Keypad1 : key - KeyCode.Alpha1;
            if (index >= 0 && index < Mathf.Min(9, request.Options.Count) && !request.Options[index].Disabled)
            {
                hit = index;
                input.Use();
                return false;
            }
            if (key != KeyCode.Return && key != KeyCode.KeypadEnter) return false;
            input.Use();
            return true;
        }

        void DrawDecisionShell(Rect panel, DecisionRequest request)
        {
            Fill(new Rect(panel.x - 2, panel.y + 5, panel.width + 4, panel.height), new Color(0, 0, 0, .45f));
            DrawDecisionPlate(panel, DecisionSurface, DecisionLine, 10);
            Fill(new Rect(panel.x - 5, panel.y, panel.width + 10, 4), new Color32(105, 95, 76, 255));
            Fill(new Rect(panel.x - 5, panel.y, panel.width + 10, 1), new Color32(187, 167, 127, 255));
            for (int edge = 0; edge < 2; edge++)
            {
                float x = edge == 0 ? panel.x - 4 : panel.xMax - 8;
                var bracket = new Rect(x, panel.y + 1, 12, 31);
                Fill(bracket, DecisionCardSurface);
                ApprovedUi.Border(bracket, DecisionLine);
                Fill(new Rect(x + 4, panel.y + 6, 4, 4), DecisionLine);
                Fill(new Rect(x + 4, panel.y + 21, 4, 4), DecisionLine);
            }

            var tab = new Rect(panel.center.x - 70, 0, 140, 27);
            DrawDecisionPlate(tab, new Color32(70, 62, 48, 255), DecisionLine, 6);
            var chevron = new Vector2(tab.x + 21, tab.y + 11);
            var chevronTip = chevron + new Vector2(5, 4);
            DrawDecisionLine(chevron, chevronTip, DecisionAccent, 2);
            DrawDecisionLine(chevronTip, chevron + new Vector2(10, 0), DecisionAccent, 2);
            DecisionLabel(new Rect(tab.x + 43, tab.y + 1, 78, tab.height - 2), "待抉择", 14, DecisionPaper, TextAnchor.MiddleLeft, true);

            DecisionLabel(new Rect(panel.x + 18, 35, panel.width * .57f, 34), request.Title, 22, DecisionPaper,
                TextAnchor.MiddleLeft, true);
            float endInset = request.Close == null ? 18 : 48;
            DecisionLabel(new Rect(panel.x + panel.width * .57f, 37, panel.width * .43f - endInset, 30),
                request.Subtitle, 13, DecisionMuted, TextAnchor.MiddleRight);
            Fill(new Rect(panel.x + 17, 74, panel.width - 34, 1), DecisionLine);
            Fill(new Rect(panel.x + 17, 246, panel.width - 34, 1), DecisionLine);
        }

        void DrawDecisionCard(Rect rect, DecisionOption option, bool navigation, bool selected, bool hover)
        {
            var ink = option.Disabled ? DecisionMuted : DecisionPaper;
            var border = selected ? DecisionAccent : hover ? DecisionPaper : DecisionLine;
            var surface = selected ? new Color32(81, 65, 43, 255) : hover ? new Color32(66, 60, 48, 255) : DecisionCardSurface;
            DrawDecisionPlate(rect, surface, border, 7);
            if (selected)
                Fill(new Rect(rect.x + 8, rect.y + 1, rect.width - 9, 1), DecisionAccent);
            if (option.Cost > 0)
                DrawDecisionLine(new Vector2(rect.x + 1, rect.y + 16), new Vector2(rect.x + 16, rect.y + 1),
                    ApprovedUi.Tier(option.Cost), 4);

            if (navigation)
            {
                DrawDecisionIcon(new Rect(rect.x + 11, rect.y + 8, 18, 18), option, ink);
                DecisionLabel(new Rect(rect.x + 38, rect.y + 2, rect.width - 48, 16), option.Name, 13, ink,
                    TextAnchor.MiddleLeft, true);
                DecisionLabel(new Rect(rect.x + 38, rect.y + 17, rect.width - 48, 15), option.Outcome ?? option.Description,
                    10, DecisionMuted, TextAnchor.MiddleLeft);
                return;
            }

            var icon = new Rect(rect.x + 12, rect.y + 12, 37, 37);
            DrawDecisionIcon(icon, option, option.Disabled ? DecisionMuted : DecisionAccent);
            float titleWidth = rect.width - (option.Current ? 107 : 67);
            DecisionLabel(new Rect(icon.xMax + 8, rect.y + 14, titleWidth, 33), option.Name, 17, ink,
                TextAnchor.MiddleLeft, true);
            if (option.Current)
                DecisionLabel(new Rect(rect.xMax - 43, rect.y + 16, 33, 26), "当前", 11, DecisionAccent, TextAnchor.MiddleRight);
            if (!string.IsNullOrEmpty(option.Outcome))
                DecisionLabel(new Rect(rect.x + 13, rect.y + 49, rect.width - 26, 30), option.Outcome, 19, ink,
                    TextAnchor.MiddleLeft, true);
            float descriptionTop = string.IsNullOrEmpty(option.Outcome) ? 58 : 86;
            DecisionLabel(new Rect(rect.x + 13, rect.y + descriptionTop, rect.width - 26, rect.height - descriptionTop - 9),
                option.Description, 12, DecisionMuted, TextAnchor.UpperLeft, wrap: true);
        }

        void DrawDecisionIcon(Rect rect, DecisionOption option, Color color)
        {
            if (option.ArtId != null)
            {
                DrawFormationPortrait(rect, option.ArtId);
                return;
            }
            if (option.ItemId != null)
            {
                DrawSupplyItem(rect, option.ItemId);
                return;
            }

            void Stroke(float x0, float y0, float x1, float y1) => DrawDecisionLine(
                rect.position + Vector2.Scale(rect.size, new Vector2(x0, y0)),
                rect.position + Vector2.Scale(rect.size, new Vector2(x1, y1)), color, 1.8f);

            if (option.Icon == "ascend")
            {
                Stroke(.18f, .83f, .76f, .25f);
                Stroke(.42f, .25f, .76f, .25f);
                Stroke(.76f, .25f, .76f, .59f);
            }
            else if (option.Icon == "ember")
            {
                Stroke(.20f, .66f, .50f, .18f);
                Stroke(.50f, .18f, .80f, .66f);
                Stroke(.80f, .66f, .50f, .87f);
                Stroke(.50f, .87f, .20f, .66f);
                Stroke(.32f, .68f, .61f, .40f);
            }
            else
            {
                Stroke(.12f, .50f, .50f, .12f);
                Stroke(.50f, .12f, .88f, .50f);
                Stroke(.88f, .50f, .50f, .88f);
                Stroke(.50f, .88f, .12f, .50f);
                Stroke(.37f, .50f, .63f, .50f);
            }
        }

        void DrawDecisionAction(Rect button, string name, bool enabled, bool primary)
        {
            bool hovered = enabled && button.Contains(Event.current.mousePosition);
            var surface = enabled && primary ? DecisionAccent : hovered ? DecisionLine : DecisionCardSurface;
            DrawDecisionPlate(button, surface, enabled ? DecisionLine : new Color32(90, 85, 72, 255), 4);
            var ink = enabled && primary ? DecisionSurface : enabled ? DecisionPaper : DecisionMuted;
            DecisionLabel(button, name, primary ? 14 : 19, ink, TextAnchor.MiddleCenter, true);
        }

        void DecisionLabel(Rect rect, string text, int size, Color color, TextAnchor alignment, bool bold = false, bool wrap = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            var style = approvedShop.LoopText(size, alignment, color, bold ? 650 : 400);
            if (wrap) style = new GUIStyle(style) { wordWrap = true, clipping = TextClipping.Clip };
            GUI.Label(rect, text, style);
        }

        static void DrawDecisionPlate(Rect rect, Color surface, Color line, float corner)
        {
            // Square upper corners keep the plate flush with the rail. The lower ones are clipped.
            Fill(new Rect(rect.x, rect.y, rect.width, rect.height - corner), surface);
            Fill(new Rect(rect.x + corner, rect.yMax - corner, rect.width - 2 * corner, corner), surface);
            for (int y = 0; y < corner; y++)
            {
                float width = corner - y;
                Fill(new Rect(rect.x + y, rect.yMax - corner + y, width, 1), surface);
                Fill(new Rect(rect.xMax - corner, rect.yMax - corner + y, width, 1), surface);
            }
            Fill(new Rect(rect.x, rect.y, rect.width, 1), line);
            Fill(new Rect(rect.x, rect.y, 1, rect.height - corner), line);
            Fill(new Rect(rect.xMax - 1, rect.y, 1, rect.height - corner), line);
            Fill(new Rect(rect.x + corner, rect.yMax - 1, rect.width - 2 * corner, 1), line);
            DrawDecisionLine(new Vector2(rect.x, rect.yMax - corner), new Vector2(rect.x + corner, rect.yMax - 1), line, 1);
            DrawDecisionLine(new Vector2(rect.xMax - 1, rect.yMax - corner), new Vector2(rect.xMax - corner, rect.yMax - 1), line, 1);
        }

        static void DrawDecisionLine(Vector2 start, Vector2 end, Color color, float width)
        {
            var matrix = GUI.matrix;
            var delta = end - start;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            Fill(new Rect(start.x, start.y - width / 2f, delta.magnitude, width), color);
            GUI.matrix = matrix;
        }
    }
}
