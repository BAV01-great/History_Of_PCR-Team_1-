# History of PCR: Team 1

An interactive VR experience on the history of PCR (the polymerase chain reaction), built in Unity for the Meta Quest 3 and playable on a mid-range laptop. Made by Team 1 for the IGH XR & AI Genomics Hackathon.

## The project in brief

**The task.** Team 1 was tasked with building an interactive VR experience on the history of PCR. It takes the user from Kary Mullis's idea in 1983 through to today's applications of PCR, and it also introduces how PCR works and its main types.

**The challenges, and how we met them**

| Challenge | Our solution |
| :-- | :-- |
| Sourcing accurate information | Consulted scientific literature and verified information across credible sources. |
| Translating scientific knowledge into simple analogies | Converted complex mechanisms into familiar, relatable concepts. |
| Simplifying the content for a wider audience while keeping the core science | Identified the essential concepts and removed unnecessary technical detail. |
| Optimising models for VR | Light scenes: simple shapes, shared materials, no realtime shadows. |
| Staying engaging without losing scientific relevance | Storytelling, visualisation and interaction, without compromising accuracy. |

## What you do in Scene 1

1. **The landing.** A dark room with a glowing DNA helix. Walk towards it and a portal opens around it.
2. **The lab.** Put on the lab coat, approach the DNA, then press **START JOURNEY**.
3. **The timeline.** Eight milestones, each with a short visual, a text panel and narration:
   1869 DNA isolated, 1953 the double helix, 1983 the PCR concept, 1985 PCR published, 1988 Taq polymerase, 1993 the Nobel Prize, the 1990s real-time PCR, and PCR today.
4. **The 1983 scene** plays on its own: denaturation, annealing and extension, then the DNA doubling (x2, x4, x8).
5. **Back to the lab.** The lights power up, ready for Scene 2.

It is meant to read like a story that flows, not a game with a score. Navy and dark is the hackathon theme, used in small, quiet ways.

## Run it

You need **Unity 6000.3.25f1** (Universal Render Pipeline 17.3). Open the project, then:

1. Exit Safe Mode if Unity offers it.
2. `PCR Tools > 1 - Setup Project`.
3. `PCR Tools > Build Scene 1 (Journey Through Time)`. This also refreshes the lab prefab.
4. Press Play.

On a laptop you walk with **WASD** and the mouse, and press buttons with a click or **E**. **Space** skips a narration line. In a headset the controllers do the same. The XR Interaction Simulator can stand in for a headset in the editor.

`PCR Tools > Save Lab Prefab` saves the lab as `Assets/Resources/LabPrefab/Lab.prefab`. If the prefab is missing, the game builds the lab from code instead.

## Built with

- Unity 6000.3, URP 17.3, the new Input System
- XR Interaction Toolkit 3.6.1, OpenXR and Meta OpenXR
- Most of the world is built from code at runtime; the scene file only holds the player rig and one controller object.

## Where things are

| Folder | What is in it |
| :-- | :-- |
| `Assets/PCR/Scripts/Journey/` | The story: `JourneyController` (the flow), `LandingStage`, `TimelineStage`, `MilestoneVisuals`, `JourneyContent`, `JourneyAudio` |
| `Assets/PCR/Scripts/Lab/` | The lab room, lighting, the street outside, the step system, the lab coat step |
| `Assets/PCR/Scripts/Core/` | UI, the rig, narration, materials, procedural audio |
| `Assets/PCR/Editor/` | Editor tools: project setup, scene build, lab prefab, test runner |
| `Assets/Resources/` | Loaded by name at run time: voice-overs, fonts, the lab prefab, team models, photos |
| `3D Designs/` | The team's Blender exports |

## The team

| Member | Role |
| :-- | :-- |
| Ayomide Jonathan Jegede (Captain) | Ensured scientific accuracy, content and experience design, team coordination, task research |
| Abuoma Ejikeme (Vice-captain) | Ensured scientific accuracy, voice-over artist, team coordination |
| Christiana Iyanuoluwapo Kolapo | Science communicator and translator, social media content writer, constructive feedback |
| Ajayi Samuel Adebanji | Science communicator and translator, content design, constructive feedback |
| Victor Inioluwa Abadunni | Scene development, animation development |
| Bamidele Ayomide Victor | 3D model development, task research, constructive feedback |
| Comfort Funmilayo Oyebamiji | Ensured scientific accuracy, task research, constructive feedback |
| Ayodeji Adesegun | Scene development, animation development |

The team is a mix of scientists, science communicators, a Blender artist and Unity developers.

## Credits

- **3D DNA, strand, primer, extension and Taq models:** the team, made in Blender.
- **Lab equipment and furniture:** the "Lab Assets" pack (CC0), plus models from Kenney and Poly Haven.
- **Photos on the "PCR Today" screen** (Wikimedia Commons):
  - Molecular diagnostics: *PCR machine* by Tinojasontran (public domain)
  - Genetic testing: *DNA sequencing interferogram* (public domain)
  - Infectious disease research: *MERS-CoV thin section* by Maureen Metcalfe and Azaibi Tamin (public domain)
  - Cancer research: *Grade 2 clear cell renal cell carcinoma* by Mikael Haggstrom, M.D. (CC0)
  - Forensics: *Autoradiograph of the first genetic fingerprint, 1984*, Wellcome Collection (CC BY 4.0)
  - Research laboratories: *Pipetting* by Diane A. Reid (public domain)
  - Genomic analysis: *Illumina Genome Analyzer II System* by Jon Callas (CC BY 2.0)
- **Typeface:** Play, by Jonas Hecksher, under the SIL Open Font License (`Assets/Resources/Fonts/OFL.txt`).
- **Voice-overs:** recorded by the team's voice-over artists.
