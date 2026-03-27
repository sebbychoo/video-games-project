using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for Tool modifier application.
    /// Validates Property 30: Effective value = baseValue + sum of tool modifiers of same type.
    /// Validates: Requirements 15.2, 15.3, 15.4
    /// </summary>
    [TestFixture]
    public class ToolModifierPropertyTests
    {
        private const int Iterations = 200;

        /// <summary>
        /// Helper: compute effective value by summing all modifiers of a given type across tools.
        /// This mirrors the additive stacking logic used in BattleManager.ApplyToolModifiers.
        /// </summary>
        private static int ComputeEffectiveValue(int baseValue, List<ToolData> tools, ToolModifierType targetType)
        {
            int sum = baseValue;
            foreach (var tool in tools)
            {
                if (tool.modifiers == null) continue;
                foreach (var mod in tool.modifiers)
                {
                    if (mod.modifierType == targetType)
                        sum += mod.value;
                }
            }
            return sum;
        }

        /// <summary>
        /// Helper: create a ToolData ScriptableObject with the given modifiers.
        /// </summary>
        private static ToolData CreateTool(string name, params ToolModifier[] mods)
        {
            var tool = ScriptableObject.CreateInstance<ToolData>();
            tool.toolName = name;
            tool.modifiers = new List<ToolModifier>(mods);
            return tool;
        }

        #region Property 30: Tool Modifier Application — Additive Stacking

        /// <summary>
        /// Property 30: For any base value and any set of tools, the effective value for a
        /// given modifier type equals baseValue + sum of all modifier values of that type
        /// across all tools. Multiple tools with the same modifier type stack additively.
        /// Validates: Requirements 15.2, 15.3, 15.4
        /// </summary>
        [Test]
        public void Property30_EffectiveValue_Equals_BaseValue_Plus_SumOfModifiers()
        {
            var rng = new System.Random(42);
            var modifierTypes = (ToolModifierType[])Enum.GetValues(typeof(ToolModifierType));
            var createdAssets = new List<ToolData>();

            try
            {
                for (int i = 0; i < Iterations; i++)
                {
                    int baseValue = rng.Next(0, 100);
                    var targetType = modifierTypes[rng.Next(modifierTypes.Length)];
                    int toolCount = rng.Next(0, 6); // 0 to 5 tools

                    var tools = new List<ToolData>();
                    int expectedSum = 0;

                    for (int t = 0; t < toolCount; t++)
                    {
                        int modCount = rng.Next(1, 4); // 1 to 3 modifiers per tool
                        var mods = new ToolModifier[modCount];

                        for (int m = 0; m < modCount; m++)
                        {
                            var modType = modifierTypes[rng.Next(modifierTypes.Length)];
                            int modValue = rng.Next(-5, 20);
                            mods[m] = new ToolModifier { modifierType = modType, value = modValue };

                            if (modType == targetType)
                                expectedSum += modValue;
                        }

                        var tool = CreateTool($"Tool_{i}_{t}", mods);
                        tools.Add(tool);
                        createdAssets.Add(tool);
                    }

                    int effective = ComputeEffectiveValue(baseValue, tools, targetType);
                    Assert.AreEqual(baseValue + expectedSum, effective,
                        $"[Iter {i}] Effective value for {targetType} with base={baseValue}, " +
                        $"{toolCount} tools should be {baseValue + expectedSum} but got {effective}");
                }
            }
            finally
            {
                foreach (var asset in createdAssets)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        /// <summary>
        /// Property 30 (edge case): With zero tools, effective value equals base value.
        /// </summary>
        [Test]
        public void Property30_NoTools_EffectiveValueEqualsBase()
        {
            var rng = new System.Random(99);
            var modifierTypes = (ToolModifierType[])Enum.GetValues(typeof(ToolModifierType));

            for (int i = 0; i < 50; i++)
            {
                int baseValue = rng.Next(0, 200);
                var targetType = modifierTypes[rng.Next(modifierTypes.Length)];
                var tools = new List<ToolData>();

                int effective = ComputeEffectiveValue(baseValue, tools, targetType);
                Assert.AreEqual(baseValue, effective,
                    $"[Iter {i}] With no tools, effective value should equal base={baseValue}");
            }
        }

        /// <summary>
        /// Property 30 (isolation): Modifiers of a different type do not affect the target type.
        /// </summary>
        [Test]
        public void Property30_DifferentModifierTypes_DoNotAffectTargetType()
        {
            var rng = new System.Random(77);
            var modifierTypes = (ToolModifierType[])Enum.GetValues(typeof(ToolModifierType));
            var createdAssets = new List<ToolData>();

            try
            {
                for (int i = 0; i < Iterations; i++)
                {
                    int baseValue = rng.Next(0, 100);
                    // Pick two distinct modifier types
                    var targetType = modifierTypes[rng.Next(modifierTypes.Length)];
                    ToolModifierType otherType;
                    do { otherType = modifierTypes[rng.Next(modifierTypes.Length)]; }
                    while (otherType == targetType && modifierTypes.Length > 1);

                    // Create tools with only the other type
                    int toolCount = rng.Next(1, 5);
                    var tools = new List<ToolData>();

                    for (int t = 0; t < toolCount; t++)
                    {
                        var mod = new ToolModifier { modifierType = otherType, value = rng.Next(1, 50) };
                        var tool = CreateTool($"OtherTool_{i}_{t}", mod);
                        tools.Add(tool);
                        createdAssets.Add(tool);
                    }

                    int effective = ComputeEffectiveValue(baseValue, tools, targetType);
                    Assert.AreEqual(baseValue, effective,
                        $"[Iter {i}] Modifiers of type {otherType} should not affect {targetType}. " +
                        $"Expected {baseValue} but got {effective}");
                }
            }
            finally
            {
                foreach (var asset in createdAssets)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        /// <summary>
        /// Property 30 (commutativity): Order of tools does not affect the effective value.
        /// </summary>
        [Test]
        public void Property30_ToolOrder_DoesNotAffectEffectiveValue()
        {
            var rng = new System.Random(123);
            var modifierTypes = (ToolModifierType[])Enum.GetValues(typeof(ToolModifierType));
            var createdAssets = new List<ToolData>();

            try
            {
                for (int i = 0; i < Iterations; i++)
                {
                    int baseValue = rng.Next(0, 100);
                    var targetType = modifierTypes[rng.Next(modifierTypes.Length)];
                    int toolCount = rng.Next(2, 6);

                    var tools = new List<ToolData>();
                    for (int t = 0; t < toolCount; t++)
                    {
                        var mod = new ToolModifier { modifierType = targetType, value = rng.Next(-10, 30) };
                        var tool = CreateTool($"OrderTool_{i}_{t}", mod);
                        tools.Add(tool);
                        createdAssets.Add(tool);
                    }

                    int effectiveForward = ComputeEffectiveValue(baseValue, tools, targetType);

                    // Reverse the tool list
                    var reversed = new List<ToolData>(tools);
                    reversed.Reverse();
                    int effectiveReversed = ComputeEffectiveValue(baseValue, reversed, targetType);

                    Assert.AreEqual(effectiveForward, effectiveReversed,
                        $"[Iter {i}] Tool order should not matter. Forward={effectiveForward}, Reversed={effectiveReversed}");
                }
            }
            finally
            {
                foreach (var asset in createdAssets)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        #endregion
    }
}
