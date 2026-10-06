using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    /// <summary>
    /// Runs a StepSequence: highlights the next target with a pulsing outline and marker, shows a short "Step n of N" prompt, waits for the
    /// player to press it, plays the action's animation, then moves on by itself. Wrong presses get a hint. A step can be gated (the scene
    /// releases it when its beat is over) and an "approach" step is completed by the scene when the player reaches a zone.
    /// Actions are plugged in with Register(); an action with no handler is a plain press.
    /// </summary>
    public class StepManager : MonoBehaviour
    {
        public static StepManager Instance { get; private set; }

        public delegate IEnumerator ActionHandler(StepDef step, GameObject target);

        readonly Dictionary<string, ActionHandler> actions = new Dictionary<string, ActionHandler>();
        StepSequence seq;
        int index;
        bool busy, running, gateOpen;

        public event Action<StepDef> StepStarted;
        public event Action<StepDef> StepCompleted;
        public event Action Finished;
        public int Index => index;
        public bool Running => running;
        public bool GateOpen => gateOpen;
        public StepDef Current => seq != null && index < seq.steps.Count ? seq.steps[index] : null;

        void Awake() { Instance = this; StepTargets.Clicked += OnClicked; }
        void OnDestroy() { StepTargets.Clicked -= OnClicked; if (Instance == this) Instance = null; }

        public void Register(string action, ActionHandler handler) { actions[action] = handler; }

        public void Begin(StepSequence sequence)
        {
            seq = sequence; index = 0; running = true; busy = false;
            Debug.Log($"[PCR] Steps begin: {seq.sequenceId} ({seq.steps.Count} steps)");
            ShowStep();
        }

        void ShowStep()
        {
            var s = Current;
            if (s == null) { Finish(); return; }
            if (StepTargets.Find(s.target) == null)
            {
                Debug.LogWarning($"[PCR] Step '{s.id}': target '{s.target}' is not registered, skipping.");
                index++; ShowStep(); return;
            }
            gateOpen = !s.gated;
            Debug.Log($"[PCR] Step {index + 1}/{seq.steps.Count}: {s.id} -> {s.target}{(s.gated ? " (gated)" : "")}");
            if (!string.IsNullOrEmpty(s.say)) NarrationManager.Instance.Say(s.say);
            StepStarted?.Invoke(s);
            if (gateOpen) Activate(s);
        }

        void Activate(StepDef s)
        {
            var t = StepTargets.Find(s.target);
            if (t != null) Highlighter.Show(t);
            NarrationManager.Instance.SetObjective($"Step {index + 1} of {seq.steps.Count}:  {s.prompt}");
        }

        /// <summary>Go back to an earlier step (PREVIOUS / REPLAY): the step starts again, its gate closed.</summary>
        public void Rewind(int toIndex)
        {
            if (!running || busy || seq == null) return;
            Highlighter.Clear();
            index = Mathf.Clamp(toIndex, 0, seq.steps.Count - 1);
            ShowStep();
        }

        /// <summary>Open a gated step: the target is highlighted and accepts the press.</summary>
        public void ReleaseGate()
        {
            var s = Current;
            if (!running || s == null || gateOpen) return;
            gateOpen = true;
            Activate(s);
        }

        /// <summary>Complete the current step from the scene (for "approach" steps, or steps finished by an event).</summary>
        public void CompleteCurrent(string stepId)
        {
            var s = Current;
            if (!running || busy || s == null || s.id != stepId) return;
            StartCoroutine(RunStep(s, StepTargets.Find(s.target), false));
        }

        void OnClicked(string id)
        {
            if (!running || busy) return;
            var s = Current;
            if (s == null || s.action == "approach") return;
            if (id != s.target)
            {
                var go = StepTargets.Find(id);
                Sfx.Wrong(go != null ? go.transform.position : Vector3.zero);
                NarrationManager.Instance.ShowMessage(string.IsNullOrEmpty(s.hint) ? "Not that one yet. " + s.prompt : s.hint, 3f);
                return;
            }
            if (!gateOpen) return;
            StartCoroutine(RunStep(s, StepTargets.Find(s.target), true));
        }

        IEnumerator RunStep(StepDef s, GameObject target, bool pressed)
        {
            busy = true;
            Highlighter.Clear();
            NarrationManager.Instance.SetObjective("");
            if (pressed && actions.TryGetValue(s.action ?? "click", out var handler)) yield return handler(s, target);
            else yield return null;
            StepCompleted?.Invoke(s);
            index++;
            busy = false;
            ShowStep();
        }

        void Finish()
        {
            running = false;
            Debug.Log("[PCR] Steps finished");
            Highlighter.Clear();
            NarrationManager.Instance.SetObjective("");
            Finished?.Invoke();
        }
    }
}
