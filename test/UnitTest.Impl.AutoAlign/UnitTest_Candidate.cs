using System;
using System.Linq;
using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.AutoAlign;
using NUnit.Framework;

namespace ClearBible.Clear3.UnitTest.Impl.AutoAlign
{
    [TestFixture]
    public class UnitTest_Candidate
    {
        [Test]
        public static void Basic()
        {
            // Array of 8 source points.
            SourcePoint[] sourcePoints =
                Enumerable.Range(0, 8)
                .Select(n =>
                    new SourcePoint(
                        $"lemma{n}", null, new SourceID(1, 1, 1, n, 1),
                        $"altID{n}", n, n / 8.0, n))
                .ToArray();

            // Array of 8 target points.
            TargetPoint[] targetPoints =
                Enumerable.Range(0, 8)
                .Select(n =>
                    new TargetPoint(
                        $"text{n}", $"lower{n}", new TargetID(1, 1, 1, n),
                        $"altID{n}", n, n / 8.0))
                .ToArray();

            // Candidate1: source0 -> target2, score -0.1
            Candidate c1 = Candidate.NewPoint(
                sourcePoints[0],
                targetPoints[2],
                -0.1);

            Assert.True(c1.IsPoint);
            Assert.False(c1.IsUnion);
            Assert.False(c1.IsAdjusted);
            Assert.AreSame(c1.SourcePoint, sourcePoints[0]);
            Assert.AreSame(c1.TargetPoint, targetPoints[2]);
            Assert.Null(c1.Head);
            Assert.Null(c1.Tail);
            Assert.Null(c1.Underlying);
            Assert.AreEqual(c1.LogScore, -0.1);
            Assert.False(c1.IsConflicted);
            Assert.AreEqual(c1.FirstTargetPosition, 2);
            Assert.AreEqual(c1.LastTargetPosition, 2);
            Assert.AreEqual(c1.TotalMotion, 0);
            Assert.AreEqual(c1.NumberMotions, 0);
            Assert.AreEqual(c1.NumberBackwardMotions, 0);
            Assert.True(Enumerable.SequenceEqual(
                c1.TargetRange.Positions(),
                new int[] { 2 }));

            // Candidate2: source2 -> nothing.
            Candidate c2 = Candidate.NewEmptyPoint(sourcePoints[2]);

            Assert.True(c2.IsPoint);
            Assert.False(c2.IsUnion);
            Assert.False(c2.IsAdjusted);
            Assert.AreSame(c2.SourcePoint, sourcePoints[2]);
            Assert.Null(c2.TargetPoint);
            Assert.Null(c2.Head);
            Assert.Null(c2.Tail);
            Assert.Null(c2.Underlying);
            Assert.AreEqual(c2.LogScore, 0.0);
            Assert.False(c2.IsConflicted);
            Assert.Null(c2.FirstTargetPosition);
            Assert.Null(c2.LastTargetPosition);
            Assert.AreEqual(c2.TotalMotion, 0);
            Assert.AreEqual(c2.NumberMotions, 0);
            Assert.AreEqual(c2.NumberBackwardMotions, 0);
            Assert.False(c2.TargetRange.Positions().Any());

            // Candidate3: union of candidates 1 and 2.
            Candidate c3 = c1.Union(c2);

            Assert.False(c3.IsPoint);
            Assert.True(c3.IsUnion);
            Assert.False(c3.IsAdjusted);
            Assert.Null(c3.SourcePoint);
            Assert.Null(c3.TargetPoint);
            Assert.AreSame(c3.Head, c1);
            Assert.AreSame(c3.Tail, c2);
            Assert.Null(c3.Underlying);
            Assert.AreEqual(c3.LogScore, -0.1);
            Assert.False(c3.IsConflicted);
            Assert.AreEqual(c3.FirstTargetPosition, 2);
            Assert.AreEqual(c3.LastTargetPosition, 2);
            Assert.AreEqual(c3.TotalMotion, 0);
            Assert.AreEqual(c3.NumberMotions, 0);
            Assert.AreEqual(c3.NumberBackwardMotions, 0);
            Assert.True(Enumerable.SequenceEqual(
                c3.TargetRange.Positions(),
                new int[] { 2 }));

            // Candidate4: adjust score of candidate3 to -0.2
            Candidate c4 = c3.WithAdjustedScore(-0.2);

            Assert.False(c4.IsPoint);
            Assert.False(c4.IsUnion);
            Assert.True(c4.IsAdjusted);
            Assert.Null(c4.SourcePoint);
            Assert.Null(c4.TargetPoint);
            Assert.Null(c4.Head);
            Assert.Null(c4.Tail);
            Assert.AreSame(c4.Underlying, c3);
            Assert.AreEqual(c4.LogScore, -0.2);
            Assert.False(c4.IsConflicted);
            Assert.AreEqual(c4.FirstTargetPosition, 2);
            Assert.AreEqual(c4.LastTargetPosition, 2);
            Assert.AreEqual(c4.TotalMotion, 0);
            Assert.AreEqual(c4.NumberMotions, 0);
            Assert.AreEqual(c4.NumberBackwardMotions, 0);
            Assert.True(Enumerable.SequenceEqual(
                c4.TargetRange.Positions(),
                new int[] { 2 }));

        }
    }
}
