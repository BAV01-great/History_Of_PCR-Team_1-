// LEGACY (archived): belongs to the old five-scene plan. Compiled only if the scripting define PCR_LEGACY is set.
// Kept for reuse of the station and bench code in later scenes.
#if PCR_LEGACY
using System.Collections;
using UnityEngine;

namespace PCR
{
    /// <summary>
    /// Scene 6: the modern PCR lab (Labster-style). Builds the room from the team's lab reference photo, then runs the lab-coat onboarding and the
    /// guided experiment (see PpeOnboarding / StepManager). This scene is what Scene 5's PCR LAB door loads (Scene6_PCRLab).
    /// </summary>
    public class LabController : MonoBehaviour
    {
        [Tooltip("Hands-free playthrough for screen recording (Unity Recorder).")]
        public bool AutoplayDemo;

        [Tooltip("Experiment file in Resources/Experiments (without .json). New experiments only need a new file here.")]
        public string Experiment = "pcr_setup";

        Transform root;
        public LabAnchors Anchors { get; private set; }
        StepManager steps;

        void Awake() { PcrDemo.Active = AutoplayDemo; }

        void Start()
        {
            root = new GameObject("Lab_World").transform;
            Anchors = LabRoom.Build(root);
            gameObject.AddComponent<GloveHands>();   // first-person gloved hands (bare until the gloves go on)
            PostFx.Apply(0.25f, 1.2f);
            steps = gameObject.AddComponent<StepManager>();
            BuildExperimentObjects();
            StartCoroutine(Intro());
        }

        /// <summary>Creates the interactable objects the experiment file refers to (by Id). Filled in by the onboarding and experiment features.</summary>
        void BuildExperimentObjects()
        {
            var ppe = gameObject.AddComponent<PpeOnboarding>();
            ppe.Build(root, Anchors);
            ppe.Register(steps);
            var exp = gameObject.AddComponent<LabExperiment>();
            exp.Build(root, Anchors);
            exp.Register(steps);
        }

        IEnumerator Intro()
        {
            var fader = ScreenFader.Instance;
            fader.SetAlpha(1f);
            yield return new WaitForSeconds(0.4f);
            yield return fader.FadeTo(0f, 1.6f);
            var def = StepManager.Load(Experiment);
            if (def != null) steps.Begin(def);
        }
    }
}
#endif
