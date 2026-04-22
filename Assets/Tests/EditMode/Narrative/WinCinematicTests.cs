using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Unit tests for WinCinematic.IsFinalFloor static helper.
    /// </summary>
    [TestFixture]
    public class WinCinematicTests
    {
        private GameConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void IsFinalFloor_DefaultConfig_ReturnsTrueForFloor75()
        {
            // Default finalFloor is 75
            Assert.IsTrue(WinCinematic.IsFinalFloor(75, config));
        }

        [Test]
        public void IsFinalFloor_DefaultConfig_ReturnsFalseForOtherFloors()
        {
            Assert.IsFalse(WinCinematic.IsFinalFloor(1, config));
            Assert.IsFalse(WinCinematic.IsFinalFloor(74, config));
            Assert.IsFalse(WinCinematic.IsFinalFloor(76, config));
        }

        [Test]
        public void IsFinalFloor_CustomConfig_MatchesConfiguredFloor()
        {
            config.finalFloor = 30;
            Assert.IsTrue(WinCinematic.IsFinalFloor(30, config));
            Assert.IsFalse(WinCinematic.IsFinalFloor(75, config));
        }

        [Test]
        public void IsFinalFloor_NullConfig_FallsBackToDefault75()
        {
            Assert.IsTrue(WinCinematic.IsFinalFloor(75, null));
            Assert.IsFalse(WinCinematic.IsFinalFloor(30, null));
        }
    }
}
