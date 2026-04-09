using NUnit.Framework;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for boss intro logic including dialogue skip behavior.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class BossIntroLogicPropertyTests
    {
        private const int Iterations = 200;

        /// <summary>
        /// Character pool for generating random non-whitespace strings.
        /// </summary>
        private static readonly char[] PrintableChars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-=+[]{}|;:',.<>?/~`"
            .ToCharArray();

        /// <summary>
        /// Whitespace characters used to generate whitespace-only strings.
        /// </summary>
        private static readonly char[] WhitespaceChars = { ' ', '\t', '\n', '\r' };

        /// <summary>
        /// Generates a random non-empty string containing at least one non-whitespace character.
        /// </summary>
        private static string GenerateNonEmptyDialogue(System.Random rng)
        {
            int length = rng.Next(1, 50);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = PrintableChars[rng.Next(PrintableChars.Length)];
            return new string(chars);
        }

        /// <summary>
        /// Generates a random whitespace-only string of length 1..20.
        /// </summary>
        private static string GenerateWhitespaceOnly(System.Random rng)
        {
            int length = rng.Next(1, 21);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = WhitespaceChars[rng.Next(WhitespaceChars.Length)];
            return new string(chars);
        }

        #region Property 3: Dialogue Skip on Empty Pre-Fight Dialogue

        /// <summary>
        /// Feature: boss-encounter-system, Property 3: Dialogue Skip on Empty Pre-Fight Dialogue
        ///
        /// For null preFightDialogue, verify cutscene controller skips dialogue.
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        public void Property3_NullDialogue_ShouldSkip()
        {
            Assert.IsTrue(BossCutsceneController.ShouldSkipDialogue(null),
                "Null preFightDialogue should be skipped");
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 3: Dialogue Skip on Empty Pre-Fight Dialogue
        ///
        /// For empty string preFightDialogue, verify cutscene controller skips dialogue.
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        public void Property3_EmptyDialogue_ShouldSkip()
        {
            Assert.IsTrue(BossCutsceneController.ShouldSkipDialogue(string.Empty),
                "Empty preFightDialogue should be skipped");
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 3: Dialogue Skip on Empty Pre-Fight Dialogue
        ///
        /// For any whitespace-only preFightDialogue, verify cutscene controller skips dialogue.
        /// Uses 200 iterations with randomized whitespace strings.
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        public void Property3_WhitespaceOnlyDialogue_ShouldSkip()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                string whitespace = GenerateWhitespaceOnly(rng);
                Assert.IsTrue(BossCutsceneController.ShouldSkipDialogue(whitespace),
                    $"[Iter {i}] Whitespace-only dialogue \"{EscapeForMessage(whitespace)}\" should be skipped");
            }
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 3: Dialogue Skip on Empty Pre-Fight Dialogue
        ///
        /// For any non-empty preFightDialogue (containing at least one non-whitespace character),
        /// verify cutscene controller does NOT skip dialogue.
        /// Uses 200 iterations with randomized string inputs.
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        public void Property3_NonEmptyDialogue_ShouldNotSkip()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                string dialogue = GenerateNonEmptyDialogue(rng);
                Assert.IsFalse(BossCutsceneController.ShouldSkipDialogue(dialogue),
                    $"[Iter {i}] Non-empty dialogue \"{dialogue}\" should NOT be skipped");
            }
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 3: Dialogue Skip on Empty Pre-Fight Dialogue
        ///
        /// For any preFightDialogue with leading/trailing whitespace but non-whitespace content,
        /// verify cutscene controller does NOT skip dialogue.
        /// Uses 200 iterations with randomized padded strings.
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        public void Property3_PaddedNonEmptyDialogue_ShouldNotSkip()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                string core = GenerateNonEmptyDialogue(rng);
                string leading = GenerateWhitespaceOnly(rng);
                string trailing = GenerateWhitespaceOnly(rng);
                string padded = leading + core + trailing;

                Assert.IsFalse(BossCutsceneController.ShouldSkipDialogue(padded),
                    $"[Iter {i}] Padded dialogue should NOT be skipped (has non-whitespace content)");
            }
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 3: Dialogue Skip on Empty Pre-Fight Dialogue
        ///
        /// Verify ShouldSkipDialogue is deterministic: same input always produces same result.
        /// Uses 200 iterations with randomized inputs.
        /// Validates: Requirements 4.4
        /// </summary>
        [Test]
        public void Property3_ShouldSkipDialogue_IsDeterministic()
        {
            var rng = new System.Random(55);

            for (int i = 0; i < Iterations; i++)
            {
                // Randomly pick null, empty, whitespace-only, or non-empty
                string dialogue;
                int kind = rng.Next(4);
                switch (kind)
                {
                    case 0: dialogue = null; break;
                    case 1: dialogue = string.Empty; break;
                    case 2: dialogue = GenerateWhitespaceOnly(rng); break;
                    default: dialogue = GenerateNonEmptyDialogue(rng); break;
                }

                bool first = BossCutsceneController.ShouldSkipDialogue(dialogue);
                bool second = BossCutsceneController.ShouldSkipDialogue(dialogue);
                Assert.AreEqual(first, second,
                    $"[Iter {i}] ShouldSkipDialogue must be deterministic for input \"{EscapeForMessage(dialogue)}\"");
            }
        }

        #endregion

        /// <summary>
        /// Escapes control characters for readable assertion messages.
        /// </summary>
        private static string EscapeForMessage(string s)
        {
            if (s == null) return "(null)";
            return s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
