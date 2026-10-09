#if PCR_LEGACY
using System.Collections;
using UnityEngine;

namespace PCR
{
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
            Anchors = LabRoom.BuildOrLoad(root);
            gameObject.AddComponent<GloveHands>();
            PostFx.Apply(0.25f, 1.2f);
            steps = gameObject.AddComponent<StepManager>();
            BuildExperimentObjects();
            StartCoroutine(Intro());
        }

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
