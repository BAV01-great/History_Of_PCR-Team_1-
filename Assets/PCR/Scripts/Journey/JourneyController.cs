using System.Collections;
using UnityEngine;

namespace PCR
{
    public class JourneyController : MonoBehaviour
    {
        [Tooltip("Guided steps for this scene (Assets/Data/Steps/Scene1_Steps). Built by PCR Tools > Build Scene 1.")]
        public StepSequence Steps;
        [Tooltip("Scene loaded after the transition. If it is not in Build Settings the lab just stays lit.")]
        public string NextScene = "Scene2_Stations";
        [Tooltip("Hands-free playthrough for screen recording (Unity Recorder).")]
        public bool AutoplayDemo;
        public const bool UseTeamHelixForHero = true;

        static readonly Vector3 DnaBase = new Vector3(1.2f, 0.88f, -2.0f);
        static readonly Vector3 CoatHook = new Vector3(-6.3f, 0f, -6.9f);
        static readonly Vector3 StageSpot = new Vector3(0f, 0f, 300f);
        static readonly Vector3 LandingSpot = new Vector3(0f, 0f, 600f);
        const float ZoneRadius = 2.0f;
        FogMode labFogMode; float labFogDensity, labFogStart, labFogEnd;

        Transform root;
        StepManager steps;
        TimelineStage stage;
        MilestoneVisuals visuals;
        JourneyAudio sound;
        DnaHelix dna;
        Light dnaLight;
        ParticleSystem sparkles;
        HoloButton startBtn;
        RigSelector rig;
        LandingStage landing;
        Vector3 labPos; float labYaw;
        bool stageReady, dnaShown;
        Coroutine milestoneCo;
        int firstTimelineIndex = -1;

        void Awake() { PcrDemo.Active = AutoplayDemo; StepTargets.Clear(); }

        void Start()
        {
            JourneyContent.RegisterNarration();
            root = new GameObject("Journey_World").transform;
            LabRoom.BuildOrLoad(root);
            LabLighting.Apply(0f);
            labFogMode = RenderSettings.fogMode; labFogDensity = RenderSettings.fogDensity; labFogStart = RenderSettings.fogStartDistance; labFogEnd = RenderSettings.fogEndDistance;
            PostFx.Apply(0.3f, 1.1f);

            gameObject.AddComponent<GloveHands>();
            sound = gameObject.AddComponent<JourneyAudio>();
            steps = gameObject.AddComponent<StepManager>();
            rig = FindFirstObjectByType<RigSelector>();

            BuildDna();
            landing = LandingStage.Build(root, LandingSpot);
            if (rig != null && rig.ActiveRoot != null) { labPos = rig.ActiveRoot.position; labYaw = rig.ActiveRoot.eulerAngles.y; }
            stage = TimelineStage.Build(root, StageSpot);
            visuals = gameObject.AddComponent<MilestoneVisuals>();
            visuals.Init(stage, sound);
            WireStageButtons();

            var ppe = gameObject.AddComponent<PpeOnboarding>();
            ppe.Build(root, CoatHook);
            ppe.Register(steps);
            steps.Register("press", PressAction);
            steps.StepStarted += OnStepStarted;
            steps.StepCompleted += OnStepCompleted;
            steps.Finished += () => StartCoroutine(ToScene2());

            if (Steps == null) Steps = Resources.Load<StepSequence>("Steps/Scene1_Steps");
            if (Steps == null) { Debug.LogError("[PCR] Scene1_Steps asset missing. Run PCR Tools > Build Scene 1."); return; }
            for (int i = 0; i < Steps.steps.Count; i++) if (Steps.steps[i].id == "m1869") firstTimelineIndex = i;

            StartCoroutine(Intro());
            if (AutoplayDemo) StartCoroutine(DemoRun());
        }

        void BuildDna()
        {
            var teamHelix = UseTeamHelixForHero ? Resources.Load<GameObject>("PCRModels/DNA_Helix_Whole") : null;
            dna = teamHelix != null ? DnaHelix.WrapModel(root, "CentralDNA", DnaBase, teamHelix, 1.45f)
                                    : DnaHelix.Create(root, "CentralDNA", DnaBase, 18, 0.28f, 0.085f, 36f, 7, 1.1f);
            dna.SpinDegPerSec = 14f;
            dna.SetGlow(0.65f);
            StepTargets.Register("dna", dna.gameObject);
            dnaLight = new GameObject("DnaLight").AddComponent<Light>();
            dnaLight.transform.SetParent(root, false);
            dnaLight.type = LightType.Point; dnaLight.color = new Color(0.35f, 0.75f, 1f); dnaLight.range = 8f; dnaLight.intensity = 0f; dnaLight.shadows = LightShadows.None;
            dnaLight.transform.position = DnaBase + new Vector3(0, 0.75f, 0);

            var sp = new GameObject("DnaSparkles");
            sp.transform.SetParent(root, false);
            sp.transform.position = DnaBase + new Vector3(0, 0.75f, 0);
            sparkles = sp.AddComponent<ParticleSystem>();
            var main = sparkles.main;
            main.loop = false; main.playOnAwake = false; main.startLifetime = 1.4f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f); main.startColor = new Color(0.6f, 0.95f, 1f, 1f); main.maxParticles = 120;
            var em = sparkles.emission; em.enabled = false;
            var sh = sparkles.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.35f;
            sp.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Glow(Color.white);

            var pos = DnaBase + new Vector3(0, 0.33f, -0.9f);
            startBtn = HoloButton.Create(root, pos, "START JOURNEY", new Color(0.1f, 0.85f, 1f), new Vector2(1.1f, 0.3f), 54);
            startBtn.StepId = "start_button";
            startBtn.gameObject.SetActive(false);
            dna.gameObject.SetActive(false);
        }

        IEnumerator Intro()
        {
            var fader = ScreenFader.Instance;
            fader.SetAlpha(1f);
            sound.Begin();
            if (rig != null) rig.Teleport(LandingSpot, 0f);
            landing.Atmosphere();
            yield return new WaitForSeconds(0.5f);
            yield return fader.FadeTo(0f, 2.2f);
            NarrationManager.Instance.SetObjective("Walk to the DNA helix");
            NarrationManager.Instance.Say("s1_welcome");
            landing.PortalOpened += () =>
            {
                sound.Cue("chime");
                NarrationManager.Instance.SetObjective("The portal has opened around the DNA. Step into it to enter the lab");
            };
            yield return new WaitUntil(() => landing.Entered);
            NarrationManager.Instance.SetObjective("");
            sound.Cue("whoosh");
            fader.SetTint(Color.white);
            yield return fader.FadeTo(1f, 0.5f);
            if (rig != null) rig.Teleport(labPos, labYaw);
            RenderSettings.fogMode = labFogMode; RenderSettings.fogDensity = labFogDensity; RenderSettings.fogStartDistance = labFogStart; RenderSettings.fogEndDistance = labFogEnd; LabLighting.Apply(0f);
            fader.SetTint(Theme.DeepNavy);
            yield return fader.FadeTo(0f, 1.6f);
            Debug.Log("[PCR] Intro: landing done, in the lab");
            var mc = Camera.main;
            Debug.Log("[PCR] Lab view: cam=" + (mc != null ? mc.transform.position.ToString("F2") + " bg=" + mc.backgroundColor + " mask=" + mc.cullingMask + " far=" + mc.farClipPlane : "none") + " fader=" + fader.Alpha.ToString("F2") + " rigAt=" + (rig != null ? rig.ActiveRoot.position.ToString("F2") : "?") + " ambient=" + RenderSettings.ambientLight + " fog=" + RenderSettings.fogMode);
            steps.Begin(Steps);
        }

        void Update()
        {
            if (steps == null || !steps.Running) return;
            var s = steps.Current;
            var cam = Camera.main;
            if (cam == null || !dnaShown) return;
            var d = cam.transform.position - (DnaBase + new Vector3(0, 0.7f, 0)); d.y = 0;
            float dist = d.magnitude;
            if (stageReady) return;
            float near = Mathf.InverseLerp(7f, ZoneRadius, dist);
            sound.SetHum(dna.transform, near);
            if (s != null && s.id == "approach_dna")
            {
                dna.SetGlow(0.65f + near * 0.2f + 0.05f * Mathf.Sin(Time.time * 2f));
                dnaLight.intensity = 0.8f + near * 1.6f;
                if (dist < ZoneRadius) steps.CompleteCurrent("approach_dna");
            }
        }

        void OnStepStarted(StepDef s)
        {
            if (s.id == "start_journey") { startBtn.gameObject.SetActive(true); startBtn.transform.rotation = Quaternion.LookRotation(startBtn.transform.position - Camera.main.transform.position); sound.Cue("pop"); }
            foreach (var m in JourneyContent.Milestones)
                if (m.StepId == s.id)
                {
                    if (milestoneCo != null) StopCoroutine(milestoneCo);
                    milestoneCo = StartCoroutine(PlayMilestone(System.Array.IndexOf(JourneyContent.Milestones, m)));
                    break;
                }
        }

        void OnStepCompleted(StepDef s)
        {
            if (s.id == "wear_coat") StartCoroutine(ShowDna());
            else if (s.id == "approach_dna") StartCoroutine(DnaActivated());
            else if (s.id == "start_journey") StartCoroutine(ToTimeline());
        }

        IEnumerator ShowDna()
        {
            dnaShown = true;
            dna.gameObject.SetActive(true);
            sound.Cue("shimmer");
            NarrationManager.Instance.Say("s1_start");
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 2.5f);
                dna.transform.localScale = Vector3.one * k;
                dna.SetGlow(0.65f * k);
                dnaLight.intensity = 0.8f * k;
                yield return null;
            }
            dna.transform.localScale = Vector3.one;
        }

        IEnumerator DnaActivated()
        {
            sound.Cue("chime");
            sparkles.Emit(90);
            startBtn.gameObject.SetActive(false);
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                dna.SpinDegPerSec = Mathf.Lerp(14f, 3f, t / 1.2f);
                dna.SetGlow(Mathf.Lerp(0.85f, 1.1f, t / 1.2f));
                dnaLight.intensity = Mathf.Lerp(2.4f, 3.4f, t / 1.2f);
                yield return null;
            }
        }

        IEnumerator ToTimeline()
        {
            NarrationManager.Instance.StopAll();
            startBtn.gameObject.SetActive(false);
            sound.Cue("whoosh");
            var cam = Camera.main;
            var toward = cam.transform.position + cam.transform.forward * 1.3f;
            var from = dna.transform.position; var fromScale = dna.transform.localScale;
            var fader = ScreenFader.Instance;
            fader.SetTint(Color.white);
            for (float t = 0; t < 1.7f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 1.7f);
                dna.transform.position = Vector3.Lerp(from, toward + Vector3.down * 0.35f, k);
                dna.transform.localScale = fromScale * Mathf.Lerp(1f, 1.9f, k);
                dna.SetGlow(Mathf.Lerp(0.8f, 0.55f, k));
                yield return null;
            }
            sound.Cue("flash");
            yield return fader.FadeTo(1f, 0.45f);
            if (rig != null) rig.Teleport(StageSpot, 0f);
            dna.gameObject.SetActive(false);
            LabLighting.Apply(0f);
            stageReady = true;
            sound.SetHum(null, 0f);
            fader.SetTint(Theme.DeepNavy);
            yield return fader.FadeTo(0f, 1.3f);
        }

        IEnumerator PlayMilestone(int i)
        {
            while (!stageReady) yield return null;
            var m = JourneyContent.Milestones[i];
            bool last = i == JourneyContent.Milestones.Length - 1;
            stage.ShowButtons(false, false);
            stage.SetCurrent(i);
            stage.ShowPanel(m.Year, m.Title, m.Body);
            sound.Cue("tick");
            sound.OnMilestone(i);
            yield return visuals.Play(i);
            while (NarrationManager.Instance.IsSpeaking) yield return null;
            sound.Cue("riser");
            stage.ShowButtons(!last, last);
            steps.ReleaseGate();
        }

        void WireStageButtons()
        {
            stage.Previous.Pressed += () => { if (steps.Index > firstTimelineIndex) { CancelBeat(); steps.Rewind(steps.Index - 1); } };
            stage.Replay.Pressed += () => { CancelBeat(); steps.Rewind(steps.Index); };
        }

        void CancelBeat()
        {
            if (milestoneCo != null) StopCoroutine(milestoneCo);
            milestoneCo = null;
            NarrationManager.Instance.StopAll();
            visuals.Stop();
        }

        IEnumerator PressAction(StepDef s, GameObject target)
        {
            var hands = GloveHands.Instance;
            if (hands == null || target == null) yield break;
            hands.SetPose(true, HandPose.Point);
            yield return hands.ReachTo(true, LabUtil.Approach(target, 0.16f), 0.45f);
            yield return new WaitForSeconds(0.12f);
            hands.SetPose(true, HandPose.Relaxed);
            yield return hands.ReturnToRest(true, 0.4f);
            if (s.id != "explore") sound.Cue("next");
        }

        IEnumerator ToScene2()
        {
            NarrationManager.Instance.Say("to_scene2");
            sound.Cue("revwhoosh");
            visuals.Stop();
            stage.HidePanel();
            stage.ShowButtons(false, false);
            yield return new WaitForSeconds(1.2f);
            var cam = Camera.main;
            dna.gameObject.SetActive(true);
            dna.transform.localScale = Vector3.one * 0.2f;
            dna.transform.position = StageSpot + new Vector3(0, 0.9f, 3f);
            dna.SpinDegPerSec = 40f;
            sound.SetHum(dna.transform, 1f);
            var fader = ScreenFader.Instance;
            fader.SetTint(Color.white);
            for (float t = 0; t < 2.6f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 2.6f);
                dna.transform.position = Vector3.Lerp(StageSpot + new Vector3(0, 0.9f, 3f), cam.transform.position + cam.transform.forward * 1.4f + Vector3.down * 0.4f, k);
                dna.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.0f, k);
                dna.SetGlow(Mathf.Lerp(0.8f, 0.55f, k));
                yield return null;
            }
            sound.Cue("flash");
            yield return fader.FadeTo(1f, 0.45f);
            if (rig != null) rig.Teleport(new Vector3(-0.2f, 0f, -3.6f), 0f);
            startBtn.gameObject.SetActive(false);
            dna.gameObject.SetActive(false);
            sound.SetHum(null, 0f);
            LabLighting.Apply(0.15f);
            fader.SetTint(Theme.DeepNavy);
            yield return fader.FadeTo(0f, 0.6f);
            sound.Cue("powerup");
            yield return LabLighting.Fade(0.15f, 1f, 3.5f);
            sound.OnSceneTwo();
            SceneFlow.LoadNext(this, NextScene);
        }

        IEnumerator WaitStep(string id) { yield return new WaitUntil(() => steps.Current != null && steps.Current.id == id); }

        IEnumerator DemoRun()
        {
            yield return new WaitForSeconds(3.5f);
            var walk = StartCoroutine(rig.WalkTo(LandingSpot + new Vector3(0, 0, 6.4f), 1.8f));
            yield return new WaitUntil(() => landing.Opened);
            StopCoroutine(walk);
            yield return new WaitForSeconds(2.2f);
            walk = StartCoroutine(rig.WalkTo(landing.HelixPos, 1.6f));
            yield return new WaitUntil(() => landing.Entered);
            StopCoroutine(walk);
            yield return WaitStep("wear_coat");
            yield return rig.WalkTo(CoatHook + new Vector3(0.6f, 0, 2.2f), 1.6f);
            yield return rig.FaceTowards(CoatHook + Vector3.up);
            yield return new WaitForSeconds(1f);
            StepTargets.Raise("lab_coat");
            yield return WaitStep("approach_dna");
            yield return rig.WalkTo(DnaBase + new Vector3(-1.6f, 0, -0.6f), 1.5f);
            yield return rig.FaceTowards(DnaBase + Vector3.up * 0.7f);
            yield return WaitStep("start_journey");
            while (NarrationManager.Instance.IsSpeaking) yield return null;
            yield return new WaitForSeconds(1.5f);
            StepTargets.Raise("start_button");
            foreach (var m in JourneyContent.Milestones)
            {
                yield return WaitStep(m.StepId);
                yield return new WaitUntil(() => steps.GateOpen);
                yield return new WaitForSeconds(1.2f);
                StepTargets.Raise(m.StepId == "today" ? "explore_button" : "next_button");
            }
        }
    }
}
