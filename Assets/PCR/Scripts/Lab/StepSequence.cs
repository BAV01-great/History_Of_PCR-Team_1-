using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    [Serializable]
    public class StepDef
    {
        public string id;          // unique step id
        public string target;      // StepTargets id the player must press (or the zone, for "approach")
        public string action;      // wear | approach | press | click | (any action registered with StepManager.Register)
        public string dest;        // optional destination id, for actions that need one
        [TextArea] public string prompt;   // short instruction shown in the HUD
        public string say;         // optional narration id (Resources/Voiceover/<id> if present; text from PcrText)
        [TextArea] public string hint;     // shown when the player presses the wrong thing
        public bool gated;         // true: the target is not offered until StepManager.ReleaseGate() (e.g. NEXT after a milestone's beat)
    }

    /// <summary>
    /// An ordered list of guided steps. Create with Assets > Create > PCR > Step Sequence, or from code (see PcrSceneBuilders).
    /// Every scene uses the same StepManager with its own sequence asset, so new scenes need data, not new systems.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStepSequence", menuName = "PCR/Step Sequence")]
    public class StepSequence : ScriptableObject
    {
        public string sequenceId;
        public string title;
        [TextArea] public string finishPrompt;
        public List<StepDef> steps = new List<StepDef>();
    }
}
