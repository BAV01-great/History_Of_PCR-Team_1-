#if PCR_LEGACY
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PCR
{
    public class LabExperiment : MonoBehaviour
    {
        const float ThermocyclerYaw = 90f;

        Transform root;
        LabAnchors anchors;
        GloveHands hands;

        GameObject pipette, waterBottle, mastermixTube, pcrTube, thermocycler, lid, startButton;
        Transform pipetteLiquid, mixLiquid, pcrLiquid, blockPoint;
        Vector3 pipetteHome, waterHome, tubeHome;
        Quaternion pipetteHomeRot;
        Text display;
        Material ledMat;
        bool pipetteHeld;

        public void Build(Transform parent, LabAnchors a)
        {
            root = new GameObject("ExperimentBench").transform;
            root.SetParent(parent, false);
            anchors = a;
            hands = GloveHands.Instance;
            float y = a.BenchTopY, x = a.ExperimentX;

            var standM = Mats.Lit(Theme.NavyMid, null, 0.4f);
            Gen.Box("PipetteStandBase", root, new Vector3(x, y + 0.01f, -2.7f), new Vector3(0.16f, 0.02f, 0.16f), standM);
            Gen.Prim(PrimitiveType.Cylinder, "PipetteStandPost", root, new Vector3(x - 0.05f, y + 0.12f, -2.7f), new Vector3(0.012f, 0.11f, 0.012f), standM);
            Gen.Box("TipBox", root, new Vector3(x - 0.45f, y + 0.03f, -2.7f), new Vector3(0.12f, 0.06f, 0.09f), Mats.Lit(new Color(0.3f, 0.7f, 1f), null, 0.5f));
            Gen.Box("TipBoxLid", root, new Vector3(x - 0.45f, y + 0.065f, -2.7f), new Vector3(0.125f, 0.012f, 0.095f), Mats.Glass(new Color(0.8f, 0.92f, 1f, 0.4f)));
            pipetteHome = new Vector3(x, y + 0.02f, -2.7f);
            pipette = LabProps.Spawn("bottle_micropipet", root, pipetteHome, 0f, new Color(0.9f, 0.95f, 1f), 1.9f)
                      ?? Gen.Prim(PrimitiveType.Capsule, "Micropipette", root, pipetteHome + Vector3.up * 0.1f, new Vector3(0.03f, 0.1f, 0.03f), Mats.Lit(Color.white, null, 0.5f));
            pipetteHomeRot = pipette.transform.rotation;
            pipetteLiquid = Liquid(pipette.transform, new Color(0.3f, 0.8f, 1f), 0.004f, 0f, LabLocalBottom(pipette));
            LabInteractable.Make(pipette, "micropipette", "Micropipette");

            Gen.Box("IceBlock", root, new Vector3(x, y + 0.02f, -1.2f), new Vector3(0.34f, 0.04f, 0.11f), Mats.Glass(new Color(0.55f, 0.8f, 1f, 0.7f)));
            mastermixTube = Tube(new Vector3(x - 0.1f, y + 0.04f, -1.2f), 0.0095f, 0.046f, new Color(0.95f, 0.8f, 0.2f), "mastermix_tube", "Master mix", out mixLiquid, 0.55f);
            pcrTube = Tube(new Vector3(x + 0.08f, y + 0.04f, -1.2f), 0.0065f, 0.034f, new Color(0.6f, 0.9f, 1f), "pcr_tube", "PCR tube", out pcrLiquid, 0f);
            tubeHome = pcrTube.transform.position;
            Tube(new Vector3(x + 0.02f, y + 0.04f, -1.2f), 0.0065f, 0.034f, new Color(0.6f, 0.9f, 1f), null, null, out _, 0.3f);
            Tube(new Vector3(x + 0.14f, y + 0.04f, -1.2f), 0.0065f, 0.034f, new Color(0.6f, 0.9f, 1f), null, null, out _, 0.3f);

            waterHome = new Vector3(x, y, 0.9f);
            waterBottle = LabProps.Spawn("bottle_glassware_reagent_bottle_medium", root, waterHome, 0f, new Color(0.7f, 0.92f, 1.1f), 1f)
                          ?? Gen.Prim(PrimitiveType.Cylinder, "WaterBottle", root, waterHome + Vector3.up * 0.1f, new Vector3(0.07f, 0.1f, 0.07f), Mats.Glass(new Color(0.7f, 0.92f, 1f, 0.5f)));
            LabInteractable.Make(waterBottle, "water_bottle", "Nuclease-free water");

            var tcPos = new Vector3(x, y, 2.3f);
            thermocycler = LabProps.Spawn("PCRModels/PCR", root, tcPos, ThermocyclerYaw, null, 1f, 0.62f);
            if (thermocycler == null)
            {
                thermocycler = new GameObject("Thermocycler");
                thermocycler.transform.SetParent(root, false);
                thermocycler.transform.position = tcPos;
                Gen.Box("Base", thermocycler.transform, new Vector3(0, 0.07f, 0), new Vector3(0.42f, 0.14f, 0.62f), Mats.Lit(new Color(0.72f, 0.75f, 0.8f), null, 0.8f, 0.6f));
                lid = Gen.Box("Lid", thermocycler.transform, new Vector3(0, 0.165f, 0), new Vector3(0.4f, 0.05f, 0.6f), Mats.Lit(Theme.NavyMid, null, 0.4f));
            }
            var tb = LabUtil.BoundsOf(thermocycler);
            if (lid == null) lid = FindByName(thermocycler.transform, "lid");
            blockPoint = new GameObject("ThermocyclerBlock").transform;
            blockPoint.SetParent(thermocycler.transform, true);
            blockPoint.position = new Vector3(tb.center.x, tb.min.y + tb.size.y * 0.62f, tb.center.z);
            if (lid != null)
            {
                OpenLid(true, tb);
                LabInteractable.Make(lid, "thermocycler_lid", "Thermocycler lid");
            }
            else Debug.LogWarning("[PCR] Thermocycler lid not found; step 'close_lid' will be skipped.");

            ledMat = Mats.LitNew(new Color(0.1f, 0.4f, 0.15f), new Color(0.2f, 1f, 0.4f) * 1.2f, 0.3f);
            startButton = Gen.Box("StartButton", thermocycler.transform, Vector3.zero, new Vector3(0.05f, 0.035f, 0.07f), ledMat);
            startButton.transform.position = new Vector3(tb.max.x + 0.015f, tb.min.y + tb.size.y * 0.35f, tb.center.z);
            LabInteractable.Make(startButton, "thermocycler_start", "START");
            var canvas = Ui.Canvas("ThermoDisplay", thermocycler.transform, new Vector2(300, 120), Vector3.zero);
            canvas.transform.position = new Vector3(tb.max.x + 0.012f, tb.min.y + tb.size.y * 0.55f, tb.center.z);
            canvas.transform.rotation = Quaternion.Euler(0, 90f, 0);
            Ui.Rect(canvas.transform, new Color(0.02f, 0.05f, 0.1f, 0.95f), new Vector2(300, 120), Vector2.zero);
            display = Ui.Label(canvas.transform, "READY", 38, new Color(0.3f, 1f, 0.55f), TextAnchor.MiddleCenter, new Vector2(290, 110), Vector2.zero, FontStyle.Bold);
        }

        public void Register(StepManager steps)
        {
            steps.Register("pickup", PickUp);
            steps.Register("pour", Pour);
            steps.Register("aspirate", Aspirate);
            steps.Register("dispense", Dispense);
            steps.Register("place", Place);
            steps.Register("close", CloseLid);
            steps.Register("press", PressStart);
        }

        GameObject Tube(Vector3 pos, float radius, float height, Color tint, string id, string label, out Transform liquid, float fill)
        {
            var g = new GameObject(id ?? "Tube");
            g.transform.SetParent(root, false);
            g.transform.position = pos;
            Gen.Prim(PrimitiveType.Cylinder, "Body", g.transform, new Vector3(0, height / 2f, 0), new Vector3(radius * 2f, height / 2f, radius * 2f), Mats.Glass(new Color(0.85f, 0.95f, 1f, 0.35f)));
            Gen.Prim(PrimitiveType.Cylinder, "Cap", g.transform, new Vector3(0, height + 0.003f, 0), new Vector3(radius * 2.3f, 0.003f, radius * 2.3f), Mats.Lit(Color.white, null, 0.5f));
            liquid = Liquid(g.transform, tint, radius * 0.8f, height * fill, Vector3.zero);
            if (id != null) LabInteractable.Make(g, id, label);
            return g;
        }

        static Transform Liquid(Transform parent, Color c, float radius, float height, Vector3 bottomLocal)
        {
            var pivot = new GameObject("Liquid").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = bottomLocal;
            var cyl = Gen.Prim(PrimitiveType.Cylinder, "Fill", pivot, new Vector3(0, 0.5f, 0), new Vector3(radius * 2f, 0.5f, radius * 2f), Mats.Lit(c * 0.7f, c * 0.8f, 0.8f));
            pivot.localScale = new Vector3(1, Mathf.Max(0.0001f, height), 1);
            return pivot;
        }

        static void SetFill(Transform liquid, float h) { if (liquid != null) liquid.localScale = new Vector3(1, Mathf.Max(0.0001f, h), 1); }

        static Vector3 LabLocalBottom(GameObject go) => go.transform.InverseTransformPoint(new Vector3(LabUtil.BoundsOf(go).center.x, LabUtil.BoundsOf(go).min.y + 0.002f, LabUtil.BoundsOf(go).center.z));

        static GameObject FindByName(Transform t, string part)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
                if (c != t && c.name.ToLowerInvariant().Contains(part)) return c.gameObject;
            return null;
        }

        Vector3 lidHinge; float lidAngle = 38f;
        void OpenLid(bool open, Bounds tb)
        {
            if (lid == null) return;
            lidHinge = new Vector3(tb.min.x, tb.max.y, tb.center.z);
            if (open)
            {
                var probe = new Vector3(tb.max.x, tb.max.y, tb.center.z) - lidHinge;
                lidAngle = (Quaternion.AngleAxis(Mathf.Abs(lidAngle), Vector3.forward) * probe).y > probe.y ? Mathf.Abs(lidAngle) : -Mathf.Abs(lidAngle);
                lid.transform.RotateAround(lidHinge, Vector3.forward, lidAngle);
            }
        }

        bool FreeIsRight => hands == null || hands.Held(true) == null;

        IEnumerator PickUp(StepDef s, LabInteractable li)
        {
            if (hands == null) yield break;
            var item = li.gameObject;
            hands.SetPose(true, HandPose.Open);
            yield return hands.ReachTo(true, LabUtil.Approach(item, 0.14f), 0.5f);
            hands.SetPose(true, HandPose.PipetteGrip);
            yield return new WaitForSeconds(0.2f);
            Sfx.Press(item.transform.position);
            hands.Hold(true, item.transform, new Vector3(0f, 0.01f, 0.01f), new Vector3(-65f, 0f, 0f));
            pipetteHeld = true;
            yield return hands.ReturnToRest(true, 0.55f);
            hands.SetPose(true, HandPose.PipetteGrip);
        }

        IEnumerator CarryTip(Vector3 tipTarget, float seconds)
        {
            var tip = new Vector3(LabUtil.BoundsOf(pipette).center.x, LabUtil.BoundsOf(pipette).min.y, LabUtil.BoundsOf(pipette).center.z);
            var offset = tip - hands.HoldWorldPosition(true);
            yield return hands.CarryTo(true, tipTarget - offset, seconds);
        }

        IEnumerator Aspirate(StepDef s, LabInteractable li)
        {
            if (hands == null || !pipetteHeld) yield break;
            var tubeTop = mastermixTube.transform.position + Vector3.up * 0.05f;
            hands.SetPose(true, HandPose.PipetteGrip);
            yield return CarryTip(tubeTop + Vector3.up * 0.04f, 0.6f);
            yield return CarryTip(tubeTop - Vector3.up * 0.02f, 0.3f);
            hands.SetThumbPress(true, 1f);
            yield return new WaitForSeconds(0.15f);
            for (float t = 0; t < 0.5f; t += Time.deltaTime) { hands.SetThumbPress(true, 1f - t / 0.5f); yield return null; }
            hands.SetThumbPress(true, 0f);
            yield return Fill(pipetteLiquid, 0f, 0.012f, 0.5f);
            SetFill(mixLiquid, 0.03f);
            Sfx.Press(tubeTop);
            yield return CarryTip(tubeTop + Vector3.up * 0.1f, 0.4f);
            yield return hands.ReturnToRest(true, 0.5f);
            hands.SetPose(true, HandPose.PipetteGrip);
        }

        IEnumerator Dispense(StepDef s, LabInteractable li)
        {
            if (hands == null || !pipetteHeld) yield break;
            var tubeTop = pcrTube.transform.position + Vector3.up * 0.04f;
            yield return CarryTip(tubeTop + Vector3.up * 0.05f, 0.6f);
            yield return CarryTip(tubeTop, 0.25f);
            hands.SetThumbPress(true, 1f);
            yield return Fill(pipetteLiquid, 0.012f, 0f, 0.6f);
            yield return Fill(pcrLiquid, 0f, 0.018f, 0.4f);
            Sfx.Press(tubeTop);
            yield return new WaitForSeconds(0.15f);
            hands.SetThumbPress(true, 0f);
            yield return CarryTip(tubeTop + Vector3.up * 0.12f, 0.4f);
            yield return hands.ReturnToRest(true, 0.5f);
            hands.SetPose(true, HandPose.PipetteGrip);
        }

        IEnumerator Fill(Transform liquid, float from, float to, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) { SetFill(liquid, Mathf.Lerp(from, to, t / seconds)); yield return null; }
            SetFill(liquid, to);
        }

        IEnumerator Pour(StepDef s, LabInteractable li)
        {
            if (hands == null) yield break;
            bool right = FreeIsRight;
            var dest = LabInteractable.Find(s.dest) != null ? LabInteractable.Find(s.dest).gameObject : mastermixTube;
            hands.SetPose(right, HandPose.Open);
            yield return hands.ReachTo(right, LabUtil.Approach(waterBottle, 0.15f), 0.5f);
            hands.SetPose(right, HandPose.Grab);
            yield return new WaitForSeconds(0.2f);
            hands.Hold(right, waterBottle.transform, new Vector3(0, -0.04f, 0.03f), Vector3.zero);
            yield return hands.CarryTo(right, dest.transform.position + new Vector3(0.05f, 0.2f, 0), 0.7f);
            var stream = Gen.Prim(PrimitiveType.Cylinder, "Stream", null, dest.transform.position + Vector3.up * 0.1f, new Vector3(0.004f, 0.09f, 0.004f), Mats.Glow(new Color(0.5f, 0.85f, 1f, 0.9f)));
            stream.transform.localScale = new Vector3(0.006f, 0.09f, 0.006f);
            stream.SetActive(false);
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { waterBottle.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 105f, t / 0.6f)); yield return null; }
            stream.SetActive(true);
            yield return Fill(mixLiquid, 0.03f, 0.042f, 1.2f);
            stream.SetActive(false);
            Destroy(stream);
            for (float t = 0; t < 0.5f; t += Time.deltaTime) { waterBottle.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(105f, 0, t / 0.5f)); yield return null; }
            yield return hands.CarryTo(right, waterHome + new Vector3(0, 0.1f, 0), 0.7f);
            hands.Release(right);
            waterBottle.transform.SetPositionAndRotation(waterHome, Quaternion.identity);
            hands.SetPose(right, HandPose.Open);
            yield return hands.ReturnToRest(right, 0.5f);
        }

        IEnumerator Place(StepDef s, LabInteractable li)
        {
            if (hands == null) yield break;
            bool right = FreeIsRight;
            var item = li.gameObject;
            hands.SetPose(right, HandPose.Open);
            yield return hands.ReachTo(right, LabUtil.Approach(item, 0.12f), 0.5f);
            hands.SetPose(right, HandPose.Pinch);
            yield return new WaitForSeconds(0.2f);
            hands.Hold(right, item.transform, new Vector3(0, -0.01f, 0.012f), Vector3.zero);
            var dest = blockPoint != null ? blockPoint.position : thermocycler.transform.position + Vector3.up * 0.2f;
            yield return hands.CarryTo(right, dest + Vector3.up * 0.14f, 0.8f);
            yield return hands.CarryTo(right, dest + Vector3.up * 0.04f, 0.4f);
            hands.Release(right);
            item.transform.SetParent(thermocycler.transform, true);
            item.transform.position = dest;
            item.transform.rotation = Quaternion.identity;
            Sfx.Press(dest);
            hands.SetPose(right, HandPose.Open);
            yield return hands.ReturnToRest(right, 0.5f);
        }

        IEnumerator CloseLid(StepDef s, LabInteractable li)
        {
            if (lid == null) yield break;
            bool right = FreeIsRight;
            if (hands != null)
            {
                hands.SetPose(right, HandPose.Open);
                yield return hands.ReachTo(right, LabUtil.Center(lid) + Vector3.up * 0.1f + (Camera.main.transform.position - LabUtil.Center(lid)).normalized * 0.08f, 0.5f);
            }
            float done = 0f;
            while (done < lidAngle * Mathf.Sign(lidAngle))
            {
                float step = Mathf.Min(Time.deltaTime * 70f, lidAngle * Mathf.Sign(lidAngle) - done);
                lid.transform.RotateAround(lidHinge, Vector3.forward, -Mathf.Sign(lidAngle) * step);
                done += step;
                yield return null;
            }
            Sfx.Press(lid.transform.position);
            if (hands != null) yield return hands.ReturnToRest(right, 0.5f);
        }

        IEnumerator PressStart(StepDef s, LabInteractable li)
        {
            bool right = FreeIsRight;
            if (hands != null)
            {
                hands.SetPose(right, HandPose.Point);
                yield return hands.ReachTo(right, LabUtil.Approach(startButton, 0.13f), 0.55f);
                var home = startButton.transform.position;
                yield return LabUtil.Move(startButton.transform, home + Vector3.left * 0.012f, 0.12f);
                Sfx.Press(home);
                yield return LabUtil.Move(startButton.transform, home, 0.12f);
                yield return hands.ReturnToRest(right, 0.5f);
            }
            string[] phase = { "DENATURE", "ANNEAL", "EXTEND" };
            int[] temp = { 95, 58, 72 };
            for (int cycle = 1; cycle <= 5; cycle++)
                for (int p = 0; p < 3; p++)
                {
                    display.text = $"{temp[p]} C  {phase[p]}\nCYCLE {cycle}/30";
                    ledMat.SetColor("_EmissionColor", Color.Lerp(new Color(0.2f, 0.5f, 1f), new Color(1f, 0.3f, 0.15f), (temp[p] - 58) / 37f) * 1.4f);
                    yield return new WaitForSeconds(0.7f);
                }
            display.text = "RUN COMPLETE\n30 CYCLES";
            ledMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 1.6f);
        }
    }
}
#endif
