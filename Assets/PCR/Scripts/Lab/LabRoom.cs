using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    public class LabAnchors
    {
        public Vector3 Spawn;
        public Vector3 PpeTable;
        public float BenchTopY = 0.92f;
        public float ExperimentX = -1.6f;
        public Vector3 BenchEastEdge;
    }

    public static class LabRoom
    {
        public const float HalfW = 8f, ZMin = -7f, ZMax = 7f, H = 3.4f;
        public const float DoorX = -4.5f, DoorW = 1.0f, DoorH = 2.1f;

        static Material white, beige, cabGrey, black, chrome, navy, navyMid, orange, steel, redM, glassM, yellow;

        static void InitMaterials()
        {
            white = Mats.Lit(new Color(0.95f, 0.96f, 0.97f), null, 0.25f);
            beige = Mats.Lit(new Color(0.86f, 0.82f, 0.74f), null, 0.45f);
            cabGrey = Mats.Lit(new Color(0.75f, 0.78f, 0.81f), null, 0.5f, 0.1f);
            black = Mats.Lit(new Color(0.05f, 0.05f, 0.06f), null, 0.5f);
            chrome = Mats.Lit(new Color(0.8f, 0.82f, 0.85f), null, 0.85f, 0.9f);
            navy = Mats.Lit(Theme.Navy, null, 0.3f);
            navyMid = Mats.Lit(Theme.NavyMid, null, 0.4f);
            orange = Mats.Lit(new Color(0.95f, 0.45f, 0.1f), new Color(0.95f, 0.45f, 0.1f) * 0.4f, 0.3f);
            steel = Mats.Lit(new Color(0.62f, 0.65f, 0.7f), null, 0.7f, 0.8f);
            redM = Mats.Lit(new Color(0.78f, 0.08f, 0.08f), null, 0.45f);
            glassM = Mats.Glass(new Color(0.75f, 0.9f, 0.95f, 0.18f));
            yellow = Mats.Lit(new Color(0.96f, 0.76f, 0.2f), null, 0.35f);
        }

        public const string PrefabResource = "LabPrefab/Lab";

        static LabAnchors Anchors() => new LabAnchors { Spawn = new Vector3(-5.2f, 0f, -5.0f), PpeTable = new Vector3(-6.4f, 0f, -6.3f), BenchEastEdge = new Vector3(-0.55f, 0f, 0.2f) };

        public static LabAnchors BuildOrLoad(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(PrefabResource);
            if (prefab == null) return Build(parent);
            var lab = Object.Instantiate(prefab, parent);
            lab.name = "Lab";
            lab.transform.localPosition = Vector3.zero;
            lab.transform.localRotation = Quaternion.identity;
            var links = lab.GetComponent<LabPrefabLinks>();
            if (links != null) links.Apply();
            ApplyFog();
            return Anchors();
        }

        public static LabAnchors Build(Transform parent)
        {
            var root = new GameObject("Lab").transform;
            root.SetParent(parent, false);
            InitMaterials();
            var a = Anchors();
            Atmosphere(root);
            Shell(root);
            Windows(root);
            Ceiling(root);
            Column(root, new Vector3(-4.6f, 0f, -1.0f));
            FireHoseCabinet(root, new Vector3(HalfW - 0.12f, 0f, -2.6f));

            var islandA = new Vector3(-2.0f, 0f, 0f);
            Bench(root, islandA, 8f, 1.6f, new[] { 1, 6, 11 });
            ShelfRail(root, new Vector3(-2.0f, 0f, 0f), 8f);
            Stool(root, new Vector3(-0.78f, 0f, -3.1f)); Stool(root, new Vector3(-0.78f, 0f, 3.1f));
            Stool(root, new Vector3(-0.78f, 0f, -0.35f), 20f);

            SinkIsland(root, new Vector3(3.6f, 0f, 0.8f));

            Bench(root, new Vector3(-HalfW + 0.45f, 0f, -0.5f), 9f, 0.8f, new[] { 4, 9 });
            Stool(root, new Vector3(-6.75f, 0f, -2.3f), -10f); Stool(root, new Vector3(-6.75f, 0f, 0.7f), 25f);
            Bench(root, new Vector3(3.0f, 0f, ZMax - 0.45f), 10f, 0.8f, new int[0], alongX: true);

            Furniture(root);
            Dressing(root);
            Essentials(root);
            var links = root.gameObject.AddComponent<LabPrefabLinks>();
            links.Capture(root);
            return a;
        }

        public static void ApplyFog()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.75f, 0.80f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(new Color(0.88f, 0.92f, 0.96f), Theme.Navy, 0.25f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 130f;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Theme.DeepNavy; }
        }

        static void Atmosphere(Transform root)
        {
            ApplyFog();

            var sun = new GameObject("Sunlight").AddComponent<Light>();
            LabLighting.Sun = sun;
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(48f, 105f, 0f);
        }

        static void Shell(Transform root)
        {
            var tex = Resources.Load<Texture2D>("LabExtras/Textures/concrete_floor_02_diff_1k");
            var nor = Resources.Load<Texture2D>("LabExtras/Textures/concrete_floor_02_nor_gl_1k");
            var floorM = tex != null ? Mats.LitTextured(tex, nor, new Vector2(5f, 5f), new Color(1.05f, 1.05f, 1.08f), 0.4f)
                                     : Mats.Lit(new Color(0.72f, 0.73f, 0.75f), null, 0.4f);
            float len = ZMax - ZMin;
            Gen.Box("Floor", root, new Vector3(0, -0.1f, 0), new Vector3(HalfW * 2, 0.2f, len), floorM, true);
            Gen.Box("NavyInlay", root, new Vector3(-3.2f, 0.004f, -1.0f), new Vector3(0.12f, 0.008f, 10f), navyMid);
            Gen.Box("NavyInlayX", root, new Vector3(-0.2f, 0.004f, -5.0f), new Vector3(8f, 0.008f, 0.12f), navyMid);

            Gen.Box("WallRight", root, new Vector3(HalfW + 0.15f, H / 2, 0), new Vector3(0.3f, H, len), white, true);
            float dl = DoorX - DoorW / 2f, dr = DoorX + DoorW / 2f;
            Gen.Box("WallFrontL", root, new Vector3((-HalfW + dl) / 2f, H / 2, ZMin - 0.15f), new Vector3(dl + HalfW, H, 0.3f), white, true);
            Gen.Box("WallFrontR", root, new Vector3((dr + HalfW) / 2f, H / 2, ZMin - 0.15f), new Vector3(HalfW - dr, H, 0.3f), white, true);
            Gen.Box("WallFrontLintel", root, new Vector3(DoorX, (DoorH + H) / 2f, ZMin - 0.15f), new Vector3(DoorW, H - DoorH, 0.3f), white, true);
            Gen.Box("WallBack", root, new Vector3(0, H / 2, ZMax + 0.15f), new Vector3(HalfW * 2, H, 0.3f), white, true);
            Gen.Box("DadoRight", root, new Vector3(HalfW - 0.01f, 0.5f, 0), new Vector3(0.02f, 1.0f, len), navyMid);
            Gen.Box("DadoBack", root, new Vector3(0, 0.5f, ZMax - 0.01f), new Vector3(HalfW * 2, 1.0f, 0.02f), navyMid);
            Gen.Box("DadoFrontL", root, new Vector3((-HalfW + dl) / 2f, 0.5f, ZMin + 0.01f), new Vector3(dl + HalfW, 1.0f, 0.02f), navyMid);
            Gen.Box("DadoFrontR", root, new Vector3((dr + HalfW) / 2f, 0.5f, ZMin + 0.01f), new Vector3(HalfW - dr, 1.0f, 0.02f), navyMid);
        }

        static void Windows(Transform root)
        {
            float x = -HalfW;
            Gen.Box("WallLeftLow", root, new Vector3(x - 0.15f, 0.45f, 0), new Vector3(0.3f, 0.9f, ZMax - ZMin), white, true);
            Gen.Box("WallLeftHigh", root, new Vector3(x - 0.15f, 3.05f, 0), new Vector3(0.3f, 0.7f, ZMax - ZMin), white, true);
            Gen.Box("DadoLeft", root, new Vector3(x + 0.01f, 0.45f, 0), new Vector3(0.02f, 0.9f, ZMax - ZMin), navyMid);
            float[][] bays = { new[] { -5.6f, -3.0f }, new[] { -2.6f, 0f }, new[] { 0.4f, 3.0f }, new[] { 3.4f, 6.0f } };
            float prev = ZMin;
            foreach (var b in bays)
            {
                if (b[0] > prev) Gen.Box("Pier", root, new Vector3(x - 0.15f, 1.8f, (prev + b[0]) / 2f), new Vector3(0.3f, 1.8f, b[0] - prev), white, true);
                float cz = (b[0] + b[1]) / 2f, w = b[1] - b[0];
                Gen.Box("FrameTop", root, new Vector3(x + 0.02f, 2.68f, cz), new Vector3(0.06f, 0.05f, w), navyMid);
                Gen.Box("FrameBottom", root, new Vector3(x + 0.02f, 0.92f, cz), new Vector3(0.08f, 0.05f, w), navyMid);
                Gen.Box("Mullion", root, new Vector3(x + 0.02f, 1.8f, cz), new Vector3(0.05f, 1.76f, 0.04f), navyMid);
                Gen.Box("Glass", root, new Vector3(x + 0.03f, 1.8f, cz), new Vector3(0.01f, 1.76f, w - 0.04f), glassM);
                for (int i = 0; i < 12; i++)
                    Gen.Box("Slat", root, new Vector3(x + 0.07f, 2.62f - i * 0.065f, cz), new Vector3(0.015f, 0.04f, w - 0.06f), yellow);
                prev = b[1];
            }
            Gen.Box("PierEnd", root, new Vector3(x - 0.15f, 1.8f, (prev + ZMax) / 2f), new Vector3(0.3f, 1.8f, ZMax - prev), white, true);
            var sky = LabLighting.Sky = Mats.UnlitNew(new Color(0.72f, 0.86f, 0.96f));
            var lawn = LabLighting.Lawn = Mats.UnlitNew(new Color(0.28f, 0.52f, 0.25f));
            var tree = LabLighting.Tree = Mats.UnlitNew(new Color(0.16f, 0.36f, 0.18f));
            Gen.Box("Sky", root, new Vector3(x - 8f, 3f, 3f), new Vector3(0.2f, 8f, 24f), sky);
            Gen.Box("Lawn", root, new Vector3(x - 7.9f, 0.4f, 3f), new Vector3(0.2f, 1.6f, 24f), lawn);
            for (int i = 0; i < 6; i++)
                Gen.Prim(PrimitiveType.Sphere, "Tree", root, new Vector3(x - 7.5f, 1.8f, -6f + i * 2.4f), new Vector3(1.6f, 1.8f + (i % 2) * 0.5f, 1.6f), tree);
        }

        static void Ceiling(Transform root)
        {
            Gen.Box("Ceiling", root, new Vector3(0, H + 0.1f, 0), new Vector3(HalfW * 2, 0.2f, ZMax - ZMin), navy);
            var panel = LabLighting.Panel = Mats.LitNew(Color.white, new Color(1f, 1f, 1f) * 1.4f, 0.2f);
            foreach (float cx in new[] { -5.5f, -2f, 1.5f, 5f })
                for (int i = 0; i < 5; i++)
                    Gen.Box("LightPanel", root, new Vector3(cx, H - 0.025f, -5.2f + i * 2.6f), new Vector3(1.4f, 0.05f, 0.32f), panel);
            Gen.Box("Duct", root, new Vector3(4.2f, H - 0.35f, 0), new Vector3(0.5f, 0.35f, ZMax - ZMin - 0.6f), steel);
            for (int i = 0; i < 6; i++) Gen.Box("DuctFlange", root, new Vector3(4.2f, H - 0.35f, -5.8f + i * 2.3f), new Vector3(0.56f, 0.4f, 0.05f), cabGrey);
            Gen.Box("DuctBranch", root, new Vector3(3.2f, H - 0.35f, 2.3f), new Vector3(2f, 0.25f, 0.3f), steel);
        }

        static void Column(Transform root, Vector3 p)
        {
            Gen.Box("Column", root, p + new Vector3(0, H / 2, 0), new Vector3(0.5f, H, 0.5f), white, true);
            Gen.Box("ColumnBase", root, p + new Vector3(0, 0.5f, 0), new Vector3(0.52f, 1.0f, 0.52f), navyMid);
        }

        static void FireHoseCabinet(Transform root, Vector3 p)
        {
            Gen.Box("FireHoseBox", root, p + new Vector3(0, 1.4f, 0), new Vector3(0.22f, 0.75f, 0.6f), redM);
            Gen.Box("FireHoseWindow", root, p + new Vector3(-0.115f, 1.4f, 0), new Vector3(0.01f, 0.5f, 0.4f), Mats.Lit(new Color(0.9f, 0.9f, 0.92f), null, 0.6f));
            Gen.Prim(PrimitiveType.Cylinder, "RedPipe", root, p + new Vector3(0.0f, H / 2, 0.55f), new Vector3(0.07f, H / 2, 0.07f), redM);
            Gen.Box("FirePanelSign", root, p + new Vector3(-0.115f, 2.0f, 0), new Vector3(0.01f, 0.2f, 0.2f), Mats.Lit(new Color(0.9f, 0.1f, 0.1f), new Color(0.9f, 0.1f, 0.1f) * 0.5f, 0.3f));
        }

        static void Bench(Transform root, Vector3 c, float length, float width, int[] knee, bool alongX = false)
        {
            var b = new GameObject("Bench").transform;
            b.SetParent(root, false);
            b.position = c;
            if (alongX) b.rotation = Quaternion.Euler(0, 90f, 0);
            Gen.Box("Top", b, new Vector3(0, 0.9f, 0), new Vector3(width, 0.04f, length), beige, true);
            bool wall = width < 1f;
            int n = Mathf.FloorToInt(length / 0.6f);
            float z0 = -length / 2f + 0.3f;
            for (int i = 0; i < n; i++)
            {
                float z = z0 + i * 0.6f;
                foreach (int side in wall ? new[] { 1 } : new[] { -1, 1 })
                {
                    if (side > 0 && System.Array.IndexOf(knee, i) >= 0) continue;
                    float x = wall ? 0f : side * (width / 2f - 0.31f);
                    float w = wall ? width - 0.04f : 0.58f;
                    Gen.Box("Cabinet", b, new Vector3(x, 0.44f, z), new Vector3(w, 0.84f, 0.56f), cabGrey, true);
                    float face = x + side * (w / 2f + 0.002f);
                    Gen.Box("Handle", b, new Vector3(face + side * 0.012f, 0.4f, z + 0.18f), new Vector3(0.014f, 0.1f, 0.02f), black);
                    Gen.Box("Lock", b, new Vector3(face + side * 0.012f, 0.55f, z + 0.18f), new Vector3(0.014f, 0.025f, 0.025f), black);
                }
            }
        }

        static void ShelfRail(Transform root, Vector3 c, float length)
        {
            var s = new GameObject("ShelfRail").transform;
            s.SetParent(root, false);
            s.position = c;
            foreach (float z in new[] { -length / 2f + 0.15f, -length / 4f, 0f, length / 4f, length / 2f - 0.15f })
            {
                Gen.Box("Upright", s, new Vector3(0, 1.45f, z), new Vector3(0.06f, 1.1f, 0.06f), cabGrey);
                Gen.Box("UprightBase", s, new Vector3(0, 0.98f, z), new Vector3(0.12f, 0.1f, 0.1f), steel);
            }
            Gen.Box("HighRail", s, new Vector3(0, 2.0f, 0), new Vector3(0.05f, 0.05f, length), cabGrey);
            Gen.Box("MidShelf", s, new Vector3(0, 1.62f, 0), new Vector3(0.34f, 0.025f, length - 0.2f), cabGrey);
            Gen.Box("ShelfEdge", s, new Vector3(0, 1.62f, 0), new Vector3(0.345f, 0.012f, length - 0.19f), navyMid);
            Gen.Box("PowerStrip", s, new Vector3(0.0f, 1.36f, 0), new Vector3(0.05f, 0.06f, length - 0.4f), steel);
            for (int i = 0; i < 8; i++) Gen.Box("Socket", s, new Vector3(0.03f, 1.36f, -length / 2f + 0.6f + i * (length - 1.2f) / 7f), new Vector3(0.012f, 0.045f, 0.06f), orange);
        }

        static void SinkIsland(Transform root, Vector3 c)
        {
            var b = new GameObject("SinkIsland").transform;
            b.SetParent(root, false);
            b.position = c;
            Gen.Box("Body", b, new Vector3(0, 0.44f, 0), new Vector3(1.6f, 0.84f, 3.0f), cabGrey, true);
            for (int i = 0; i < 4; i++)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float z = -1.1f + i * 0.73f;
                    Gen.Box("Handle", b, new Vector3(side * 0.812f, 0.5f, z + 0.25f), new Vector3(0.014f, 0.1f, 0.02f), black);
                }
            }
            Gen.Box("Top", b, new Vector3(0, 0.9f, 0), new Vector3(1.7f, 0.05f, 3.1f), beige, true);
            Gen.Box("Basin", b, new Vector3(0, 0.915f, -0.35f), new Vector3(0.7f, 0.03f, 0.55f), Mats.Lit(new Color(0.18f, 0.2f, 0.23f), null, 0.6f, 0.4f));
            Gen.Box("BasinRim", b, new Vector3(0, 0.935f, -0.35f), new Vector3(0.76f, 0.01f, 0.61f), steel);
            Gen.Box("SplashScreen", b, new Vector3(0, 1.25f, 0.5f), new Vector3(1.5f, 0.6f, 0.02f), glassM);
            Gen.Box("SplashFrame", b, new Vector3(0, 0.95f, 0.5f), new Vector3(1.52f, 0.025f, 0.03f), steel);
            var tapM = Mats.Lit(new Color(0.96f, 0.97f, 0.98f), null, 0.7f, 0.2f);
            Gen.Prim(PrimitiveType.Cylinder, "TapStem", b, new Vector3(0.3f, 1.12f, 0.05f), new Vector3(0.035f, 0.2f, 0.035f), tapM);
            Gen.Prim(PrimitiveType.Cylinder, "TapArc", b, new Vector3(0.3f, 1.33f, -0.1f), new Vector3(0.03f, 0.17f, 0.03f), tapM, false, Quaternion.Euler(90, 0, 0));
            Gen.Prim(PrimitiveType.Cylinder, "TapSpout", b, new Vector3(0.3f, 1.21f, -0.28f), new Vector3(0.028f, 0.1f, 0.028f), tapM);
            Gen.Prim(PrimitiveType.Cylinder, "EyewashPipe", b, new Vector3(-0.3f, 1.05f, 0.05f), new Vector3(0.035f, 0.15f, 0.035f), redM);
            foreach (float dz in new[] { -0.07f, 0.07f })
                Gen.Prim(PrimitiveType.Cylinder, "EyewashNozzle", b, new Vector3(-0.3f, 1.22f, 0.05f + dz), new Vector3(0.05f, 0.03f, 0.05f), redM);
            Gen.Prim(PrimitiveType.Cylinder, "EyewashBowl", b, new Vector3(-0.3f, 1.2f, 0.05f), new Vector3(0.22f, 0.012f, 0.22f), Mats.Lit(new Color(0.9f, 0.1f, 0.1f), null, 0.5f));
            Stool(root, c + new Vector3(0, 0, 2.3f), 0f);
        }

        public static void Stool(Transform root, Vector3 pos, float yaw = 0f)
        {
            const float caster = 0.07f;
            var model = LabProps.Spawn("LabExtras/Kenney/stoolBar", root, pos + Vector3.up * caster, yaw, new Color(0.06f, 0.08f, 0.14f), 1f, 0.78f - caster);
            if (model != null)
            {
                var yawRot = model.transform.rotation;
                model.transform.rotation = Quaternion.identity;
                var bb = LabUtil.BoundsOf(model);
                var rubber = Mats.Lit(new Color(0.04f, 0.04f, 0.05f), null, 0.3f);
                var steel = Mats.Lit(new Color(0.7f, 0.72f, 0.76f), null, 0.8f, 0.6f);
                float ex = bb.extents.x * 0.46f, ez = bb.extents.z * 0.46f;
                foreach (var sx in new[] { -1f, 1f })
                    foreach (var sz in new[] { -1f, 1f })
                    {
                        var c = new Vector3(bb.center.x + sx * ex, pos.y, bb.center.z + sz * ez);
                        Gen.Prim(PrimitiveType.Cylinder, "CasterFork", model.transform, Vector3.zero, new Vector3(0.012f, 0.02f, 0.012f), steel).transform.position = c + Vector3.up * (caster - 0.005f);
                        var w = Gen.Prim(PrimitiveType.Cylinder, "Wheel", model.transform, Vector3.zero, new Vector3(0.05f, 0.012f, 0.05f), rubber, false, Quaternion.Euler(0, 0, 90f));
                        w.transform.position = c + Vector3.up * 0.025f;
                    }
                model.transform.rotation = yawRot;
                return;
            }
            var s = new GameObject("Stool").transform;
            s.SetParent(root, false);
            s.position = pos;
            s.rotation = Quaternion.Euler(0, yaw, 0);
            Gen.Prim(PrimitiveType.Cylinder, "Seat", s, new Vector3(0, 0.66f, 0), new Vector3(0.36f, 0.04f, 0.36f), black);
            Gen.Box("Back", s, new Vector3(0, 0.82f, -0.18f), new Vector3(0.28f, 0.2f, 0.04f), black);
            Gen.Prim(PrimitiveType.Cylinder, "Lift", s, new Vector3(0, 0.36f, 0), new Vector3(0.05f, 0.3f, 0.05f), chrome);
            Gen.Prim(PrimitiveType.Cylinder, "FootRing", s, new Vector3(0, 0.3f, 0), new Vector3(0.42f, 0.008f, 0.42f), chrome);
            for (int i = 0; i < 5; i++)
                Gen.Box("Leg", s, Quaternion.Euler(0, i * 72f, 0) * new Vector3(0, 0.07f, 0.17f), new Vector3(0.04f, 0.03f, 0.34f), black).transform.rotation = Quaternion.Euler(0, i * 72f, 0);
        }

        static void Furniture(Transform root)
        {
            LabProps.Spawn("LabExtras/Kenney/kitchenFridgeLarge", root, new Vector3(-6.6f, 0f, ZMax - 0.45f), 180f, new Color(0.92f, 0.94f, 0.97f), 1f, 1.85f);
            LabProps.Spawn("LabExtras/Kenney/kitchenFridgeLarge", root, new Vector3(-5.5f, 0f, ZMax - 0.45f), 180f, new Color(0.92f, 0.94f, 0.97f), 1f, 1.85f);
            LabProps.Spawn("LabExtras/Kenney/trashcan", root, new Vector3(HalfW - 1.2f, 0f, ZMin + 0.45f), 0f, new Color(0.2f, 0.26f, 0.4f), 1f, 0.7f);
            var bio = LabProps.Spawn("LabExtras/Kenney/trashcan", root, new Vector3(HalfW - 0.55f, 0f, ZMin + 0.45f), 0f, new Color(1f, 0.84f, 0.1f), 1f, 0.9f);
            if (bio != null)
                Gen.Prim(PrimitiveType.Cylinder, "BiohazardMark", bio.transform, new Vector3(0f, 0.5f, 0.2f), new Vector3(0.2f, 0.003f, 0.2f), Mats.Lit(new Color(0.08f, 0.08f, 0.08f), null, 0.4f), false, Quaternion.Euler(90f, 0f, 0f));
            float by = 0.92f;
            LabProps.Spawn("LabExtras/Kenney/computerScreen", root, new Vector3(2.5f, by, ZMax - 0.5f), 180f, null, 1f, 0.5f);
            LabProps.Spawn("LabExtras/Kenney/computerKeyboard", root, new Vector3(2.5f, by, ZMax - 0.85f), 180f, null, 1f, 0.42f);
            LabProps.Spawn("LabExtras/Kenney/computerMouse", root, new Vector3(2.95f, by, ZMax - 0.85f), 180f, null, 1f, 0.09f);
            LabProps.Spawn("LabExtras/PolyHaven/medical_box", root, new Vector3(HalfW - 0.15f, 1.35f, 2.6f), -90f, null, 1f, 0.32f);
            const float fz = -4.2f, fy = 0.8f;
            LabProps.Spawn("ppe_fire_extinguisher_clamp", root, new Vector3(HalfW - 0.12f, fy, fz), -90f, new Color(1.3f, 0.3f, 0.3f));
            Gen.Box("ExtinguisherPlate", root, new Vector3(HalfW - 0.015f, fy + 0.3f, fz), new Vector3(0.03f, 0.62f, 0.22f), redM);
            foreach (float sy in new[] { 0.42f, 0.7f })
                Gen.Box("ExtinguisherStrap", root, new Vector3(HalfW - 0.1f, fy + sy, fz), new Vector3(0.2f, 0.035f, 0.22f), steel);
            Gen.Box("ExtinguisherSign", root, new Vector3(HalfW - 0.012f, fy + 1.15f, fz), new Vector3(0.02f, 0.3f, 0.3f), redM);
            EmergencyExit(root, DoorX);
        }

        static void EmergencyExit(Transform root, float x)
        {
            float z = ZMin;
            var red = new Color(0.8f, 0.08f, 0.08f);
            var steelM = Mats.Lit(new Color(0.75f, 0.77f, 0.8f), null, 0.8f, 0.6f);
            Gen.Box("ExitJambL", root, new Vector3(x - DoorW / 2f + 0.02f, DoorH / 2f, z - 0.15f), new Vector3(0.04f, DoorH, 0.3f), Mats.Lit(new Color(0.08f, 0.1f, 0.13f), null, 0.4f));
            Gen.Box("ExitJambR", root, new Vector3(x + DoorW / 2f - 0.02f, DoorH / 2f, z - 0.15f), new Vector3(0.04f, DoorH, 0.3f), Mats.Lit(new Color(0.08f, 0.1f, 0.13f), null, 0.4f));
            var hinge = new GameObject("ExitDoorHinge").transform;
            hinge.SetParent(root, false);
            hinge.position = new Vector3(x - DoorW / 2f + 0.04f, 0f, z - 0.1f);
            Gen.Box("ExitDoor", hinge, new Vector3((DoorW - 0.08f) / 2f, DoorH / 2f - 0.02f, 0f), new Vector3(DoorW - 0.08f, DoorH - 0.04f, 0.06f), Mats.Lit(new Color(0.5f, 0.07f, 0.07f), null, 0.4f));
            Gen.Box("ExitPushBar", hinge, new Vector3((DoorW - 0.08f) / 2f, 1.0f, 0.06f), new Vector3(0.7f, 0.05f, 0.04f), steelM);
            var opener = hinge.gameObject.AddComponent<DoorOpener>();
            opener.Hinge = hinge;
            opener.Centre = new Vector3(x, 0f, z);
            Gen.Box("ExitSign", root, new Vector3(x, 2.5f, z + 0.05f), new Vector3(1.6f, 0.36f, 0.06f), Mats.Lit(red, red * 0.7f, 0.4f));
            var cv = Ui.Canvas("ExitText", root, new Vector2(1500, 320), new Vector3(x, 2.5f, z + 0.1f));
            cv.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Ui.Label(cv.transform, "EMERGENCY EXIT", 130, Color.white, TextAnchor.MiddleCenter, new Vector2(1500, 320), Vector2.zero, FontStyle.Bold);
            CityOutside.Build(root, x);
        }

        static void Essentials(Transform root)
        {
            float by = 0.92f;
            PipetteStand(root, new Vector3(-2.55f, by, -3.2f));
            TipBox(root, new Vector3(-1.95f, by, -3.35f));
            TipBox(root, new Vector3(-1.65f, by, -3.1f), 12f);
            LabProps.Spawn("bottle_micropipet", root, new Vector3(-2.0f, by + 0.01f, -2.7f), 70f, null, 1f, 0.22f);
            TubeRack(root, new Vector3(-2.0f, by, -1.9f));
            string[] cyl = { "bottle_glassware_graduated_cylinder_small_base", "bottle_glassware_graduated_cylinder_medium_base", "bottle_glassware_graduated_cylinder_large_base" };
            for (int i = 0; i < cyl.Length; i++) Glass(cyl[i], root, new Vector3(-2.55f + i * 0.32f, by, -0.8f));
            string[] bk = { "bottle_glassware_beaker_small", "bottle_glassware_beaker_medium", "bottle_glassware_beaker_large" };
            for (int i = 0; i < bk.Length; i++) Glass(bk[i], root, new Vector3(-2.55f + i * 0.38f, by, 0.35f));
            Glass("bottle_glassware_erlenmeyer_flask_small", root, new Vector3(-2.45f, by, 1.4f));
            Glass("bottle_glassware_erlenmeyer_flask_medium", root, new Vector3(-2.0f, by, 1.5f));
            Glass("bottle_glassware_erlenmeyer_flask_large", root, new Vector3(-1.5f, by, 1.45f));
            string[] rg = { "bottle_glassware_reagent_bottle_medium", "bottle_glassware_reagent_bottle_large", "bottle_glassware_reagent_bottle_medium" };
            for (int i = 0; i < rg.Length; i++) Glass(rg[i], root, new Vector3(-2.5f + i * 0.35f, by, 2.5f), 20f * i);
            LabProps.Spawn("misc_wash_bottle", root, new Vector3(-1.5f, by, 3.2f), 30f);
            LabProps.Spawn("dish_petridish", root, new Vector3(-2.2f, by, 3.3f), 0f);
            LabProps.Spawn("dish_petridish", root, new Vector3(-2.2f, by + 0.02f, 3.3f), 10f);

            LabProps.Spawn("heating_equipment_bunsen_burner", root, new Vector3(-7.7f, by, 3.1f), 90f);
            LabProps.Spawn("heating_equipment_thermometer", root, new Vector3(-7.3f, by, 3.5f), 40f);
            TubeRack(root, new Vector3(-7.6f, by, -4.1f), 90f);

            LabProps.Spawn("bottle_micropipet", root, new Vector3(1.4f, by + 0.01f, ZMax - 0.7f), 100f, null, 1f, 0.22f);
            LabProps.Spawn("bottle_glassware_beaker_medium", root, new Vector3(0.6f, by, ZMax - 0.5f), 0f, new Color(0.8f, 0.95f, 1f));
            LabProps.Spawn("bottle_glassware_reagent_bottle_small", root, new Vector3(0.2f, by, ZMax - 0.5f), 0f, new Color(0.9f, 0.8f, 0.55f));
        }

        static GameObject Glass(string model, Transform root, Vector3 pos, float yaw = 0f)
        {
            return LabProps.Spawn(model, root, pos, yaw, new Color(0.8f, 0.95f, 1f));
        }

        static void PipetteStand(Transform root, Vector3 p)
        {
            var steelM = Mats.Lit(new Color(0.72f, 0.74f, 0.78f), null, 0.7f, 0.5f);
            var dark = Mats.Lit(new Color(0.12f, 0.14f, 0.2f), null, 0.4f);
            Gen.Prim(PrimitiveType.Cylinder, "StandBase", root, p + new Vector3(0, 0.01f, 0), new Vector3(0.24f, 0.01f, 0.24f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "StandPost", root, p + new Vector3(0, 0.16f, 0), new Vector3(0.025f, 0.15f, 0.025f), steelM);
            Gen.Prim(PrimitiveType.Cylinder, "StandDisc", root, p + new Vector3(0, 0.30f, 0), new Vector3(0.22f, 0.008f, 0.22f), dark);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                LabProps.Spawn("bottle_micropipet", root, p + new Vector3(Mathf.Cos(a) * 0.08f, 0.02f, Mathf.Sin(a) * 0.08f), 0f, null, 1f, 0.27f);
            }
        }

        static void TipBox(Transform root, Vector3 p, float yaw = 0f)
        {
            var g = new GameObject("TipBox").transform;
            g.SetParent(root, false); g.position = p; g.rotation = Quaternion.Euler(0, yaw, 0);
            Gen.Box("Tray", g, new Vector3(0, 0.025f, 0), new Vector3(0.15f, 0.05f, 0.1f), Mats.Lit(new Color(0.15f, 0.4f, 0.8f), null, 0.5f));
            var tip = Mats.Lit(new Color(0.95f, 0.97f, 1f), null, 0.4f);
            for (int ix = 0; ix < 8; ix++)
                for (int iz = 0; iz < 5; iz++)
                    Gen.Prim(PrimitiveType.Cylinder, "Tip", g, new Vector3(-0.063f + ix * 0.018f, 0.062f, -0.036f + iz * 0.018f), new Vector3(0.008f, 0.012f, 0.008f), tip);
        }

        static void TubeRack(Transform root, Vector3 p, float yaw = 0f)
        {
            var g = new GameObject("TubeRack").transform;
            g.SetParent(root, false); g.position = p; g.rotation = Quaternion.Euler(0, yaw, 0);
            var rack = Mats.Lit(new Color(0.1f, 0.3f, 0.7f), null, 0.4f);
            var glassT = Mats.Glass(new Color(0.85f, 0.95f, 1f, 0.22f));
            Gen.Box("RackBase", g, new Vector3(0, 0.006f, 0), new Vector3(0.36f, 0.012f, 0.13f), rack);
            Gen.Box("RackTop", g, new Vector3(0, 0.075f, 0), new Vector3(0.36f, 0.01f, 0.13f), rack);
            foreach (var sx in new[] { -0.175f, 0.175f }) Gen.Box("RackSide", g, new Vector3(sx, 0.04f, 0), new Vector3(0.01f, 0.08f, 0.13f), rack);
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 6; i++)
                    Gen.Prim(PrimitiveType.Cylinder, "Tube", g, new Vector3(-0.125f + i * 0.05f, 0.07f, -0.028f + row * 0.056f), new Vector3(0.026f, 0.06f, 0.026f), glassT);
        }

        static void Dressing(Transform root)
        {
            float by = 0.92f;
            LabProps.Spawn("machine_microscope", root, new Vector3(-7.7f, by, -3.2f), 90f);
            LabProps.Spawn("machine_centrifuge", root, new Vector3(-7.7f, by, -1.4f), 90f);
            LabProps.Spawn("machine_electronic_scale", root, new Vector3(-7.7f, by, 0.2f), 90f);
            LabProps.Spawn("machine_hot_plate", root, new Vector3(-7.7f, by, 1.6f), 90f);
            LabProps.Spawn("bottle_glassware_beaker_medium", root, new Vector3(3.1f, by + 0.03f, 1.6f), 0f, new Color(0.7f, 0.9f, 1f));
            LabProps.Spawn("bottle_glassware_erlenmeyer_flask_medium", root, new Vector3(3.9f, by + 0.03f, 1.8f), 0f, new Color(0.7f, 1f, 0.8f));
            Color[] liq = { new Color(0.3f, 0.8f, 1f), new Color(1f, 0.6f, 0.25f), new Color(0.4f, 1f, 0.55f), new Color(0.85f, 0.45f, 1f), new Color(1f, 0.9f, 0.3f) };
            for (int i = 0; i < 9; i++)
                LabProps.Spawn("bottle_glassware_reagent_bottle_small", root, new Vector3(-2.0f, 1.64f, -3.4f + i * 0.85f), 0f, liq[i % liq.Length]);
            LabProps.Spawn("bottle_test_tube_rack", root, new Vector3(4.6f, by, ZMax - 0.5f), 180f, new Color(0.95f, 0.55f, 0.2f));
            LabProps.Spawn("bottle_glassware_florence_flask_medium", root, new Vector3(5.6f, by, ZMax - 0.5f), 0f, new Color(1f, 0.7f, 0.85f));
        }
    }
}
