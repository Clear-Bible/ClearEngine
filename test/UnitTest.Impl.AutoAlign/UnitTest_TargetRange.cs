using System;
using System.Collections.Generic;

using NUnit.Framework;

using ClearBible.Clear3.Impl.AutoAlign;
using System.Linq;

namespace ClearBible.Clear3.UnitTest.Impl.AutoAlign
{
    [TestFixture]
    public class UnitTest_TargetRange
    {
        /// <summary>
        /// Basic unit test of TargetRange class.
        /// </summary>
        [Test]
        public static void Basic()
        {
            // Empty target range.
            TargetRange r = new TargetRange();
            List<int> positions = r.Positions();
            Assert.False(positions.Any(), "A");

            // Target range with a bit in the first word.
            TargetRange r2 = new TargetRange(5);
            positions = r2.Positions();
            Assert.True(Enumerable.SequenceEqual(positions, new int[] { 5 }));

            // Target range with a bit in the third word.
            TargetRange r3 = new TargetRange(67);
            positions = r3.Positions();
            Assert.True(Enumerable.SequenceEqual(positions, new int[] { 67 }));

            // Combination of the preceding two.
            (TargetRange r4, bool conflicted) = r3.Combine(r2);
            Assert.False(conflicted);
            positions = r4.Positions();
            Assert.True(Enumerable.SequenceEqual(positions, new int[] { 5, 67 }));

            // Combination in the other order.
            (TargetRange r5, bool conflicted5) = r2.Combine(r3);
            Assert.False(conflicted5);
            positions = r5.Positions();
            Assert.True(Enumerable.SequenceEqual(positions, new int[] { 5, 67 }));

            // Combination of the same range with itself to get a conflict.
            (TargetRange r6, bool conflicted6) = r5.Combine(r5);
            Assert.True(conflicted6);
            positions = r6.Positions();
            Assert.True(Enumerable.SequenceEqual(positions, new int[] { 5, 67 }));

            // Add in a third bit.
            TargetRange r7 = new TargetRange(12);
            (TargetRange r8, bool conflicted8) = r6.Combine(r7);
            positions = r8.Positions();
            Assert.False(conflicted8);
            Assert.True(Enumerable.SequenceEqual(positions, new int[] { 5, 12, 67 }));
        }
    }
}
