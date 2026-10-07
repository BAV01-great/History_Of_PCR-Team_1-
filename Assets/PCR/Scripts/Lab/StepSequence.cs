using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    [Serializable]
    public class StepDef
    {
        public string id;
        public string target;
        public string action;
        public string dest;
        [TextArea] public string prompt;
        public string say;
        [TextArea] public string hint;
        public bool gated;
    }

    [CreateAssetMenu(fileName = "NewStepSequence", menuName = "PCR/Step Sequence")]
    public class StepSequence : ScriptableObject
    {
        public string sequenceId;
        public string title;
        [TextArea] public string finishPrompt;
        public List<StepDef> steps = new List<StepDef>();
    }
}
