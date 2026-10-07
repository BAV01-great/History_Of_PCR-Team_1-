namespace PCR
{
    public static class JourneyContent
    {
        public class Milestone
        {
            public string StepId, Year, Title, Body;
            public string[] Say;
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
                Say = new[] { "m1983_a", "m1983_b1", "m1983_b2", "m1983_b3", "m1983_b4", "m1983_b5" } },
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
            L["s1_welcome"] = "Welcome to the world of unlimited possibilities. Move towards the helix structure to begin your journey.";
            L["s1_start"] = "Inside every sample is an enormous amount of genetic information, but sometimes, we don't need all of it. We need just one specific region. This birthed a problem: how do you find one small region and make enough copies of it to study? Scientists needed a way to selectively amplify that specific piece of DNA. That need is what led to PCR. Move towards the glowing structure to begin.";
            L["m1869"] = "In 1869, Friedrich Miescher isolated a new substance from cells and called it nuclein. We now know it as DNA. But what did it actually look like?";
            L["m1953"] = "In 1953, the DNA's double-helix structure was described, a model for how genetic information could be stored and copied was established. So could we copy it on purpose?";
            L["m1983_a"] = "In 1983, Kary Mullis developed the concept behind PCR: repeatedly copying a specific DNA sequence through cycles of heating, primer binding, and DNA synthesis. The idea offered a way to turn a tiny amount of DNA into many copies.";
            L["m1983_b1"] = "Now, let's try it.";
            L["m1983_b2"] = "Heat the DNA, and the two strands pull apart.";
            L["m1983_b3"] = "Cool it, and the primers find their place.";
            L["m1983_b4"] = "Warm it once more, and the polymerase builds new DNA.";
            L["m1983_b5"] = "One molecule becomes two. Then it all begins again.";
            L["m1985_a"] = "By 1985, the PCR method had been demonstrated experimentally and its amplification of specific DNA sequences was published. This helped move PCR from an idea into a practical laboratory technique.";
            L["m1985_b"] = "But there was a limitation: the heat used to separate DNA also destroyed the DNA polymerase enzyme after each cycle.";
            L["m1988"] = "In 1988, a heat-tolerant enzyme, Taq polymerase, solved the problem. It survived repeated heating, so PCR could be automated and used routinely.";
            L["m1993"] = "PCR was now changing biology. And in 1993, Kary Mullis was awarded half of the Nobel Prize in Chemistry for inventing it.";
            L["m1990s"] = "The story didn't stop there. In the 1990s, scientists learned to watch PCR in real time using fluorescence. Real-time PCR had arrived.";
            L["today"] = "Today, PCR is no longer limited to simply making more copies of DNA. It has become a family of related techniques used across molecular biology and medicine, from genetic testing and infectious disease research to cancer diagnosis, cancer research, forensic analysis, and genomic studies. As scientists began asking different questions, they needed PCR approaches that could do more than simply amplify DNA, for example, detecting RNA, measuring how much DNA is present, or increasing the specificity of amplification. This led to the development of different types of PCR, each adapted for a particular purpose.";
            L["to_scene2"] = "So, let's go back and explore the major types of PCR and what makes each one different.";
        }
    }
}
