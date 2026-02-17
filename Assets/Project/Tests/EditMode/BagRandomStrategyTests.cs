using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Project.Scripts.EditMode.Tests
{
    /// <summary>
    /// Tests for <see cref="BagRandomStrategy"/> 7-bag invariants.
    /// </summary>
    public class BagRandomStrategyTests
    {
        private BagRandomStrategy strategy;

        [SetUp]
        public void SetUp()
        {
            strategy = ScriptableObject.CreateInstance<BagRandomStrategy>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(strategy);
        }

        [Test]
        public void Next_7Calls_AllTypesAppearExactlyOnce()
        {
            HashSet<Tetromino> seen = new();

            for (int i = 0; i < 7; i++)
            {
                Tetromino piece = strategy.Next();
                Assert.IsTrue(seen.Add(piece), $"Duplicate {piece} in first bag at call {i}");
            }

            Assert.AreEqual(7, seen.Count);
        }

        [Test]
        public void Next_14Calls_AllTypesAppearExactlyTwice()
        {
            Dictionary<Tetromino, int> counts = new();

            for (int i = 0; i < 14; i++)
            {
                Tetromino piece = strategy.Next();
                counts.TryGetValue(piece, out int count);
                counts[piece] = count + 1;
            }

            Tetromino[] allTypes = (Tetromino[])Enum.GetValues(typeof(Tetromino));
            foreach (Tetromino type in allTypes)
            {
                Assert.IsTrue(counts.ContainsKey(type), $"{type} never appeared in 14 calls");
                Assert.AreEqual(2, counts[type], $"{type} should appear exactly twice");
            }
        }

        [Test]
        public void Reset_ClearsBag_NextBagIsComplete()
        {
            // Draw 3 pieces (partial bag)
            for (int i = 0; i < 3; i++)
            {
                strategy.Next();
            }

            strategy.Reset();

            // After reset, next 7 should be a complete bag
            HashSet<Tetromino> seen = new();
            for (int i = 0; i < 7; i++)
            {
                Tetromino piece = strategy.Next();
                Assert.IsTrue(seen.Add(piece), $"Duplicate {piece} after reset at call {i}");
            }

            Assert.AreEqual(7, seen.Count);
        }

        [Test]
        public void Next_ReturnsValidEnumValue()
        {
            HashSet<Tetromino> validValues = new((Tetromino[])Enum.GetValues(typeof(Tetromino)));

            for (int i = 0; i < 21; i++)
            {
                Tetromino piece = strategy.Next();
                Assert.IsTrue(validValues.Contains(piece), $"Invalid enum value at call {i}");
            }
        }
    }
}
