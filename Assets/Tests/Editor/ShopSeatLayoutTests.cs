using System.Linq;
using NUnit.Framework;

namespace YesterdayConvoy.Editor.Tests
{
    public sealed class ShopSeatLayoutTests
    {
        static ShopTrialModel.Piece Piece(int instance,string id,int star=1)=>new ShopTrialModel.Piece{Instance=instance,Id=id,Star=star};
        static string Snapshot(ShopSeatLayout layout,ShopTrialModel.Piece[] pieces)
            =>string.Join(";",pieces.Select(p=>{var s=layout.For(p.Instance).Value;return $"{p.Instance}:{s.Lineup}:{s.Index}";}));

        [Test]public void ChosenHolesPersistAndOccupiedSeatsSwapWithinAndAcrossRows()
        {
            var pieces=new[]{Piece(1,"A"),Piece(2,"B"),Piece(3,"C")};var layout=new ShopSeatLayout();
            layout.Synchronize(pieces,new[]{"A","B"},4);
            Assert.That(layout.TryMove(pieces,4,1,true,3,out _),Is.True);
            Assert.That(layout.At(true,0),Is.Zero);Assert.That(layout.At(true,1),Is.EqualTo(2));
            layout.Synchronize(pieces,new[]{"A","B"},4);
            Assert.That(layout.At(true,3),Is.EqualTo(1),"Rebuilding a battle does not fill deliberate holes.");
            Assert.That(layout.TryMove(pieces,4,1,true,1,out _),Is.True);
            Assert.That(layout.At(true,1),Is.EqualTo(1));Assert.That(layout.At(true,3),Is.EqualTo(2));
            Assert.That(layout.TryMove(pieces,4,1,false,0,out _),Is.True);
            Assert.That(layout.At(false,0),Is.EqualTo(1));Assert.That(layout.At(true,1),Is.EqualTo(3));
        }
        [Test]public void FullReserveAndFullLineupCanExchangeWithoutTemporaryOverflow()
        {
            var pieces=Enumerable.Range(1,20).Select(i=>Piece(i,"R"+i)).ToArray();var layout=new ShopSeatLayout();
            layout.Synchronize(pieces,pieces.Take(10).Select(p=>p.Id),10);
            Assert.That(layout.TryMove(pieces,10,11,true,4,out _),Is.True);
            Assert.That(layout.At(true,4),Is.EqualTo(11));Assert.That(layout.At(false,0),Is.EqualTo(5));
            Assert.That(layout.Lineup(pieces).Count(),Is.EqualTo(10));
        }
        [Test]public void LockedSeatsAndDuplicateKindsRejectTheWholeExchange()
        {
            var pieces=new[]{Piece(1,"A"),Piece(2,"B"),Piece(3,"A")};var layout=new ShopSeatLayout();
            layout.Synchronize(pieces,new[]{"A","B"},3);string before=Snapshot(layout,pieces);
            Assert.That(layout.TryMove(pieces,3,3,true,1,out _),Is.False);
            Assert.That(Snapshot(layout,pieces),Is.EqualTo(before));
            Assert.That(layout.TryMove(pieces,3,1,true,3,out _),Is.False);
            Assert.That(Snapshot(layout,pieces),Is.EqualTo(before));
            Assert.That(layout.TryMove(pieces,3,3,true,0,out _),Is.True,"Swapping the same kind exchanges exact instances.");
            Assert.That(layout.InLineup(3),Is.True);Assert.That(layout.InLineup(1),Is.False);
            layout.Synchronize(pieces,new[]{"A","B"},3);
            Assert.That(layout.InLineup(3),Is.True,"Sync must retain the exact manually chosen card.");
        }
        [Test]public void NewCardsAndMergeSurvivorsPreserveExistingPositionsAndStableCompaction()
        {
            var pieces=new[]{Piece(1,"A"),Piece(2,"B"),Piece(3,"C")};var layout=new ShopSeatLayout();
            layout.Synchronize(pieces,new[]{"A","B"},5);
            layout.TryMove(pieces,5,1,true,4,out _);layout.TryMove(pieces,5,3,false,8,out _);
            var after=new[]{Piece(4,"A",2),pieces[1],pieces[2],Piece(5,"D")};
            layout.Synchronize(after,new[]{"A","B"},5);
            Assert.That(layout.At(true,4),Is.EqualTo(4));Assert.That(layout.At(false,8),Is.EqualTo(3));
            Assert.That(layout.At(false,0),Is.EqualTo(5));
            layout.CompactLineup();Assert.That(layout.At(true,0),Is.EqualTo(2));Assert.That(layout.At(true,1),Is.EqualTo(4));
            Assert.That(layout.At(false,8),Is.EqualTo(3));
        }
        [Test]public void EmptyLineupTargetCannotOverflowAFullReserve()
        {
            var pieces=Enumerable.Range(1,11).Select(i=>Piece(i,"R"+i)).ToArray();var layout=new ShopSeatLayout();
            layout.Synchronize(pieces,new[]{"R1"},2);string before=Snapshot(layout,pieces);
            Assert.That(layout.TryMove(pieces,2,1,false,10,out _),Is.False);
            Assert.That(Snapshot(layout,pieces),Is.EqualTo(before));
            Assert.That(layout.TryMove(pieces,2,1,false,0,out _),Is.True,"A full reserve still accepts an atomic cross-row swap.");
        }
        [Test]public void ReusedPresetNumbersCannotSelectOrPositionADifferentKind()
        {
            var layout=new ShopSeatLayout();var old=new[]{Piece(1,"B"),Piece(2,"A"),Piece(3,"C")};
            layout.Synchronize(old,new[]{"B"},3);layout.TryMove(old,3,3,false,7,out _);
            var next=new[]{Piece(1,"A"),Piece(2,"A",2),Piece(3,"D")};
            layout.Synchronize(next,new[]{"A"},3);
            Assert.That(layout.InLineup(2),Is.True,"A reused old B instance number must not select low-star A.");
            Assert.That(layout.InLineup(1),Is.False);
            Assert.That(layout.For(3).Value.Index,Is.Not.EqualTo(7),"A new D card cannot inherit old C's reserve position.");
        }
    }
}
