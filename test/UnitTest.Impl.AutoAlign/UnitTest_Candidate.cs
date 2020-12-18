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

            Assert.AreEqual(c1.Kind, CandidateKind.Point);
            Assert.True(c1.IsPoint);
            Assert.False(c1.IsUnion);
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

            Assert.AreEqual(c2.Kind, CandidateKind.EmptyPoint);
            Assert.True(c2.IsPoint);
            Assert.False(c2.IsUnion);
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

            Assert.AreEqual(c3.Kind, CandidateKind.Union);
            Assert.False(c3.IsPoint);
            Assert.True(c3.IsUnion);
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

            // Candidate 5: union of candidates 2 and 1 (the
            // other order from above).
            Candidate c5 = c2.Union(c1);

            Assert.AreEqual(c5.Kind, CandidateKind.Union);
            Assert.False(c5.IsPoint);
            Assert.True(c5.IsUnion);
            Assert.Null(c5.SourcePoint);
            Assert.Null(c5.TargetPoint);
            Assert.AreSame(c5.Head, c2);
            Assert.AreSame(c5.Tail, c1);
            Assert.Null(c5.Underlying);
            Assert.AreEqual(c5.LogScore, -0.1);
            Assert.False(c5.IsConflicted);
            Assert.AreEqual(c5.FirstTargetPosition, 2);
            Assert.AreEqual(c5.LastTargetPosition, 2);
            Assert.AreEqual(c5.TotalMotion, 0);
            Assert.AreEqual(c5.NumberMotions, 0);
            Assert.AreEqual(c5.NumberBackwardMotions, 0);
            Assert.True(Enumerable.SequenceEqual(
                c5.TargetRange.Positions(),
                new int[] { 2 }));

            // Candidate 6: source4 -> target6.
            // Candidate 7: union of 5 and 6.
            Candidate
                c6 = Candidate.NewPoint(sourcePoints[4], targetPoints[6], -0.2),
                c7 = c5.Union(c6);

            Assert.AreEqual(c7.Kind, CandidateKind.Union);
            Assert.False(c7.IsPoint);
            Assert.True(c7.IsUnion);
            Assert.Null(c7.SourcePoint);
            Assert.Null(c7.TargetPoint);
            Assert.AreSame(c7.Head, c5);
            Assert.AreSame(c7.Tail, c6);
            Assert.Null(c7.Underlying);
            Assert.True(Math.Abs(c7.LogScore - -0.3) < 1e-6);
            Assert.False(c7.IsConflicted);
            Assert.AreEqual(c7.FirstTargetPosition, 2);
            Assert.AreEqual(c7.LastTargetPosition, 6);
            Assert.AreEqual(c7.TotalMotion, 4);
            Assert.AreEqual(c7.NumberMotions, 1);
            Assert.AreEqual(c7.NumberBackwardMotions, 0);
            Assert.True(Enumerable.SequenceEqual(
                c7.TargetRange.Positions(),
                new int[] { 2, 6 }));

            // Candidate 8: union of 6 and 5 (the other order from above).
            Candidate c8 = c6.Union(c5);

            Assert.AreEqual(c8.Kind, CandidateKind.Union);
            Assert.False(c8.IsPoint);
            Assert.True(c8.IsUnion);
            Assert.Null(c8.SourcePoint);
            Assert.Null(c8.TargetPoint);
            Assert.AreSame(c8.Head, c6);
            Assert.AreSame(c8.Tail, c5);
            Assert.Null(c8.Underlying);
            Assert.True(Math.Abs(c7.LogScore - -0.3) < 1e-6);
            Assert.False(c8.IsConflicted);
            Assert.AreEqual(c8.FirstTargetPosition, 6);
            Assert.AreEqual(c8.LastTargetPosition, 2);
            Assert.AreEqual(c8.TotalMotion, 4);
            Assert.AreEqual(c8.NumberMotions, 1);
            Assert.AreEqual(c8.NumberBackwardMotions, 1);
            Assert.True(Enumerable.SequenceEqual(
                c8.TargetRange.Positions(),
                new int[] { 2, 6 }));

            // Make something that is conflicted.
            Candidate c9 = c8.Union(c1);

            Assert.AreEqual(c9.Kind, CandidateKind.Union);
            Assert.False(c9.IsPoint);
            Assert.True(c9.IsUnion);
            Assert.Null(c9.SourcePoint);
            Assert.Null(c9.TargetPoint);
            Assert.AreSame(c9.Head, c8);
            Assert.AreSame(c9.Tail, c1);
            Assert.Null(c9.Underlying);
            Assert.True(Math.Abs(c9.LogScore - -0.4) < 1e-6);
            Assert.True(c9.IsConflicted);
            Assert.AreEqual(c9.FirstTargetPosition, 6);
            Assert.AreEqual(c9.LastTargetPosition, 2);
            Assert.AreEqual(c9.TotalMotion, 4);
            Assert.AreEqual(c9.NumberMotions, 1);
            Assert.AreEqual(c9.NumberBackwardMotions, 1);
            Assert.True(Enumerable.SequenceEqual(
                c9.TargetRange.Positions(),
                new int[] { 2, 6 }));
        }
    }
}
