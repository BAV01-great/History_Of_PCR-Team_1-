using System.Collections.Generic;

namespace PCR
{
    /// <summary>
    /// Every narration line in Scenes 1 and 5, keyed by ID. Subtitles come from here.
    /// To add voiceover, drop an audio file named exactly like the ID into
    /// Assets/Resources/Voiceover/ (e.g. s1_welcome.wav). No code changes needed.
    /// Text follows Script.docx with typos fixed.
    /// </summary>
    public static class PcrText
    {
        public static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            // ---- Scene 1: Landing ----
            ["s1_welcome"] =
                "Welcome to the world of unlimited possibility. This journey will take you through how one of the " +
                "greatest molecular biology techniques came to be. Move towards the DNA helix to begin your journey.",
            ["s1_start"] =
                "PCR, the Polymerase Chain Reaction, is a laboratory technique used to make many copies of a specific " +
                "DNA sequence. But PCR wasn't always the technology we know today. Let's explore how it developed, " +
                "how it works, and how it evolved.",

            // ---- Scene 5: Types of PCR ----
            ["s5_intro"] =
                "The basic principle of PCR remains the same. But over time, scientists have adapted it for different purposes.",
            ["s5_endpoint"] =
                "First, end-point PCR. The classic approach, where amplification is assessed at the end of the reaction.",
            ["s5_qpcr"] =
                "Then there is qPCR, or quantitative PCR, which monitors DNA amplification in real time and can be used " +
                "to measure how much target DNA is present.",
            ["s5_rtpcr"] =
                "RT-PCR begins with RNA. The RNA is first converted into complementary DNA, which can then be amplified by PCR.",
            ["s5_nested"] =
                "And nested PCR uses two successive rounds of amplification to improve the specificity of the target sequence.",
            ["s5_outro"] =
                "These are just some of the ways PCR has evolved to meet different experimental needs.",
            ["s5_door"] =
                "But understanding PCR isn't complete without seeing what makes these reactions possible.",
            ["s5_open"] =
                "So, what does a PCR laboratory actually need to make all of this happen?",
        };

        public static string Get(string id) => Lines.TryGetValue(id, out var t) ? t : id;

        public struct Quiz
        {
            public string Question;
            public string[] Options;
            public int Correct;
            public string Explain;
        }

        // Scene 5 knowledge checks (an addition to the script, Labster-style).
        public static readonly Quiz[] PcrTypeQuizzes =
        {
            new Quiz {
                Question = "In end-point PCR, when is the amplified DNA assessed?",
                Options = new[] { "After the final cycle", "After every cycle", "Before the reaction starts" },
                Correct = 0,
                Explain = "End-point PCR reads the result once, when the reaction has finished." },
            new Quiz {
                Question = "What does qPCR let you measure?",
                Options = new[] { "The colour of the primers", "How much target DNA is present", "The length of the polymerase" },
                Correct = 1,
                Explain = "qPCR tracks fluorescence in real time, so the amount of starting DNA can be quantified." },
            new Quiz {
                Question = "What is RNA converted into before RT-PCR amplifies it?",
                Options = new[] { "Protein", "A second RNA strand", "Complementary DNA (cDNA)" },
                Correct = 2,
                Explain = "Reverse transcriptase makes cDNA, which PCR can then amplify." },
            new Quiz {
                Question = "Why does nested PCR use two rounds of amplification?",
                Options = new[] { "To improve specificity", "To lower the temperature", "To skip denaturation" },
                Correct = 0,
                Explain = "The second, inner primer pair only amplifies the true target from round one." },
        };
    }
}
