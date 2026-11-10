using UnityEngine;

namespace YesterdayConvoy
{
    public readonly struct DefenderHealthBarState
    {
        public readonly bool Visible,IsDebt;
        public readonly float Amount,Capacity,Fraction;
        public readonly Color Color;
        internal DefenderHealthBarState(bool debt,float amount,float capacity,Color color)
        {Visible=true;IsDebt=debt;Amount=amount;Capacity=capacity;Fraction=Mathf.Clamp01(amount/capacity);Color=color;}
    }

    /// <summary>Read-only HUD snapshot. Red represents accumulated negative life, not ordinary HP or SP.</summary>
    public static class DefenderHealthPresentation
    {
        public static readonly Color HealthColor=new Color(.96f,.66f,.3f);
        public static readonly Color DebtColor=new Color(.98f,.14f,.2f);
        public static DefenderHealthBarState Read(DefenderState defender)
        {
            if(defender==null||!defender.Alive||defender.MaxHealth<=0)return default;
            return defender.DebtLife
                ?new DefenderHealthBarState(true,Mathf.Max(0,defender.Debt),defender.MaxHealth*2,DebtColor)
                :new DefenderHealthBarState(false,Mathf.Max(0,defender.Health),defender.MaxHealth,HealthColor);
        }
    }

    public sealed partial class ManualBattleController
    {
        static void DrawDefenderHealthBar(DefenderState defender,Rect bounds,float inset)
        {
            var state=DefenderHealthPresentation.Read(defender);if(!state.Visible)return;
            Fill(bounds,new Color(.03f,.05f,.06f));
            // A zero-debt entry still has a visible red outline; the fill itself remains an honest zero.
            if(state.IsDebt)ShopBorder(bounds,state.Color,1);
            float width=Mathf.Max(0,bounds.width-inset*2),height=Mathf.Max(0,bounds.height-inset*2);
            if(state.Fraction>0)Fill(new Rect(bounds.x+inset,bounds.y+inset,width*state.Fraction,height),state.Color);
        }
        void DrawCompactDefenderDebtLabel(DefenderState defender,Vector2 anchor)
        {
            var state=DefenderHealthPresentation.Read(defender);if(!state.Visible||!state.IsDebt)return;
            GUI.Label(new Rect(anchor.x-52,anchor.y-19,104,17),"我执 · 负生命",new GUIStyle(smallStyle){fontSize=10,alignment=TextAnchor.MiddleCenter,normal={textColor=state.Color}});
        }
    }
}
