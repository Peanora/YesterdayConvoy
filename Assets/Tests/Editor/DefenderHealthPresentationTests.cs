using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderHealthPresentationTests
    {
        static DefenderState Guard()
        {
            var settings=new ManualBattleSettings("card","unit",10,1,1,1,1,100,1,1,1,1,1);
            var guard=new DefenderSettings("D25","Hoshiguma","D25",Vector3.forward,100,10,0,1,1);
            return new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*10},Vector3.forward,
                new EngagementSettings(unitAttack:1),roster:new[]{guard}).Defender;
        }
        static void State(DefenderState defender,string property,object value)
            =>typeof(DefenderState).GetProperty(property).GetSetMethod(true).Invoke(defender,new[]{value});

        [Test]public void HealthDebtEntryAndRecoveryChooseTheirActualSeparateResource()
        {
            var guard=Guard();State(guard,"Health",50f);
            var normal=DefenderHealthPresentation.Read(guard);
            Assert.That(normal.Visible,Is.True);Assert.That(normal.IsDebt,Is.False);Assert.That(normal.Fraction,Is.EqualTo(.5f));Assert.That(normal.Color,Is.EqualTo(DefenderHealthPresentation.HealthColor));
            State(guard,"Health",0f);State(guard,"DebtLife",true);
            var entry=DefenderHealthPresentation.Read(guard);
            Assert.That(entry.Visible&&entry.IsDebt,Is.True);Assert.That(entry.Fraction,Is.Zero);Assert.That(entry.Color,Is.EqualTo(DefenderHealthPresentation.DebtColor),"Exact lethal damage must already select the red phase even with zero excess debt.");
            State(guard,"Debt",50f);var debt=DefenderHealthPresentation.Read(guard);
            Assert.That(debt.Amount,Is.EqualTo(50));Assert.That(debt.Capacity,Is.EqualTo(200));Assert.That(debt.Fraction,Is.EqualTo(.25f),"The red resource is Debt / (2 * current MaxHealth), rather than the zero Health field.");
            State(guard,"DebtLife",false);State(guard,"Debt",0f);State(guard,"Health",1f);
            var recovered=DefenderHealthPresentation.Read(guard);
            Assert.That(recovered.IsDebt,Is.False);Assert.That(recovered.Fraction,Is.EqualTo(.01f));Assert.That(recovered.Color,Is.EqualTo(normal.Color));
        }

        [Test]public void ProtectedDebtBeyondCapacityClampsOnlyTheBarAndReadingDoesNotChangeCombat()
        {
            var guard=Guard();State(guard,"Health",-10f);State(guard,"DebtLife",true);State(guard,"Debt",250f);State(guard,"ProtectionUntil",100d);
            for(int i=0;i<3;i++)
            {
                var state=DefenderHealthPresentation.Read(guard);
                Assert.That(state.Visible&&state.IsDebt,Is.True);Assert.That(state.Fraction,Is.EqualTo(1));Assert.That(state.Amount,Is.EqualTo(250));
            }
            Assert.That(guard.Health,Is.EqualTo(-10));Assert.That(guard.MaxHealth,Is.EqualTo(100));Assert.That(guard.Debt,Is.EqualTo(250));
            Assert.That(guard.DebtLife,Is.True);Assert.That(guard.ProtectionUntil,Is.EqualTo(100));Assert.That(guard.Alive,Is.True);
        }

        [TestCase("dead")][TestCase("reserve")][TestCase("departed")]
        public void AbsentDefendersDoNotExposeAHealthOrDebtBar(string kind)
        {
            var guard=Guard();
            if(kind=="dead")State(guard,"Health",0f);
            if(kind=="reserve")State(guard,"Reserve",true);
            if(kind=="departed"){State(guard,"DebtLife",true);State(guard,"Departed",true);}
            Assert.That(DefenderHealthPresentation.Read(guard).Visible,Is.False);
            Assert.That(DefenderHealthPresentation.Read(null).Visible,Is.False);
        }
    }
}
