namespace PCR
{
    /// <summary>
    /// Scene 1 content from the final script (panel text from section 6, spoken lines from the Sound Design section).
    /// Voice-over files go in Assets/Resources/Voiceover/ named by the ids below.
    /// </summary>
    public static class JourneyContent
    {
        public class Milestone
        {
            public string StepId, Year, Title, Body;
            public string[] Say;   // narration ids, played in order
        }

        public static readonly string[] Years = { "1869", "1953", "1983", "1985", "1988", "1993", "1990s", "TODAY" };

        public static readonly Milestone[] Milestones =
        {
            new Milestone { StepId = "m1869", Year = "1869", Title = "DNA ISOLATED",
                Body = "Friedrich Miescher isolated a previously unknown substance from white blood cells, which he called nuclein.\n\nThis substance was later recognised as DNA.",
                Say = new[] { "m1869" } },
            new Milestone { StepId = "m1953", Year = "1953", Title = "THE DNA DOUBLE HELIX",
                Body = "The double-helical structure of DNA was described by James Watson and Francis Crick, building on critical experimental evidence from Rosalind Franklin, Maurice Wilkins and others.",
                Say = new[] { "m1953" } },
            new Milestone { StepId = "m1983", Year = "1983", Title = "THE BIRTH OF PCR",
                Body = "Kary Mullis conceived the basic idea of the polymerase chain reaction, a method for selectively amplifying a specific DNA sequence.\n\nThe story shifts from understanding DNA to deliberately making many copies of it.",
                Say = new[] { "m1983_a", "m1983_b" } },
            new Milestone { StepId = "m1985", Year = "1985", Title = "PCR IS PUBLISHED",
                Body = "PCR was publicly demonstrated through published scientific work.\n\nA landmark Science paper reported enzymatic amplification of beta-globin genomic sequences, relevant to sickle-cell anemia.\n\nScience, 1985",
                Say = new[] { "m1985_a", "m1985_b" } },
            new Milestone { StepId = "m1988", Year = "1988", Title = "TAQ POLYMERASE TRANSFORMS PCR",
                Body = "A thermostable DNA polymerase from Thermus aquaticus (Taq) could withstand the repeated high temperatures of PCR.\n\nThat made PCR far more practical and automatable.\n\nScience, 1988",
                Say = new[] { "m1988" } },
            new Milestone { StepId = "m1993", Year = "1993", Title = "THE NOBEL PRIZE",
                Body = "1993\nNOBEL PRIZE IN CHEMISTRY\nKary B. Mullis\nFor his invention of the polymerase chain reaction (PCR) method.\n\nHalf the prize; the other half went to Michael Smith for different work.",
                Say = new[] { "m1993" } },
            new Milestone { StepId = "m1990s", Year = "1990s", Title = "REAL-TIME PCR",
                Body = "Scientists learned to watch PCR as it happened, using fluorescence.\n\nThe amount of signal rises as the target is copied.",
                Say = new[] { "m1990s" } },
            new Milestone { StepId = "today", Year = "TODAY", Title = "PCR TODAY",
                Body = "A family of related techniques used across:\n  Molecular diagnostics\n  Genetic testing\n  Infectious disease research\n  Cancer research\n  Forensics\n  Research laboratories\n  Genomic analysis",
                Say = new[] { "today" } },
        };

        public static void RegisterNarration()
        {
            var L = PcrText.Lines;
            L["s1_start"] = "Every story begins with a single molecule.";
            L["m1869"] = "In 1869, Friedrich Miescher isolated a new substance from cells and called it nuclein. We now know it as DNA. But what did it actually look like?";
            L["m1953"] = "In 1953, its double-helix structure was described, a model for how genetic information could be stored and copied. So could we copy it on purpose?";
            L["m1983_a"] = "In 1983, Kary Mullis conceived the polymerase chain reaction: a way to selectively amplify one chosen DNA sequence.";
            L["m1983_b"] = "Separate the strands. Bind the primers. Build new DNA. Repeat.";
            L["m1985_a"] = "In 1985, PCR moved from an idea into published science, amplifying specific DNA sequences for genetic analysis.";
            L["m1985_b"] = "But there was a catch: the heat that split the DNA kept destroying the enzyme.";
            L["m1988"] = "In 1988, a heat-tolerant enzyme, Taq polymerase, solved the problem. It survived repeated heating, so PCR could be automated and used routinely.";
            L["m1993"] = "PCR was now changing biology. And in 1993, Kary Mullis was awarded half of the Nobel Prize in Chemistry for inventing it.";
            L["m1990s"] = "The story didn't stop there. In the 1990s, scientists learned to watch PCR as it happened, using fluorescence. Real-time PCR had arrived.";
            L["today"] = "Today, PCR is a family of methods, each built for a different question. Let's meet them.";
            L["to_scene2"] = "Now, let's see how PCR is used today.";
        }
    }
}
