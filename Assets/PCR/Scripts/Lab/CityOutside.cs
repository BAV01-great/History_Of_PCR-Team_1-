using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    public static class CityOutside
    {
        static Material Tint(Color dim, Color bright)
        {
            var m = Mats.UnlitNew(Color.Lerp(dim, bright, LabLighting.Level));
            LabLighting.Tinted.Add((m, dim, bright));
            return m;
        }

        public static void Build(Transform root, float doorX)
        {
            var city = new GameObject("City").transform;
            city.SetParent(root, false);
            LabLighting.Tinted.Clear();

            var asphalt = Tint(new Color(0.02f, 0.025f, 0.04f), new Color(0.24f, 0.25f, 0.28f));
            var paving = Tint(new Color(0.05f, 0.06f, 0.09f), new Color(0.66f, 0.66f, 0.64f));
            var curb = Tint(new Color(0.07f, 0.08f, 0.11f), new Color(0.80f, 0.80f, 0.78f));
            var paint = Tint(new Color(0.10f, 0.10f, 0.12f), new Color(0.94f, 0.94f, 0.90f));
            var yellowLine = Tint(new Color(0.12f, 0.10f, 0.03f), new Color(0.95f, 0.78f, 0.18f));
            var trunk = Tint(new Color(0.02f, 0.02f, 0.025f), new Color(0.32f, 0.22f, 0.15f));
            var leaf = Tint(new Color(0.015f, 0.035f, 0.040f), new Color(0.20f, 0.46f, 0.24f));
            var lampPole = Tint(new Color(0.04f, 0.05f, 0.07f), new Color(0.30f, 0.32f, 0.36f));
            var lampHead = Tint(new Color(1.0f, 0.86f, 0.55f), new Color(0.90f, 0.92f, 0.95f));
            var windows = Tint(new Color(1.0f, 0.82f, 0.45f), new Color(0.40f, 0.56f, 0.70f));
            var roof = Tint(new Color(0.03f, 0.035f, 0.05f), new Color(0.38f, 0.40f, 0.44f));
            var bodies = new[]
            {
                Tint(new Color(0.035f, 0.045f, 0.075f), new Color(0.58f, 0.63f, 0.71f)),
                Tint(new Color(0.045f, 0.05f, 0.075f), new Color(0.74f, 0.69f, 0.62f)),
                Tint(new Color(0.03f, 0.04f, 0.07f), new Color(0.46f, 0.52f, 0.62f)),
                Tint(new Color(0.05f, 0.05f, 0.07f), new Color(0.78f, 0.75f, 0.70f)),
                Tint(new Color(0.025f, 0.035f, 0.06f), new Color(0.36f, 0.42f, 0.52f)),
                Tint(new Color(0.04f, 0.04f, 0.06f), new Color(0.66f, 0.54f, 0.48f)),
            };
            var carColors = new[]
            {
                Tint(new Color(0.05f, 0.02f, 0.02f), new Color(0.72f, 0.16f, 0.14f)),
                Tint(new Color(0.02f, 0.03f, 0.06f), new Color(0.18f, 0.32f, 0.62f)),
                Tint(new Color(0.06f, 0.06f, 0.07f), new Color(0.88f, 0.88f, 0.86f)),
                Tint(new Color(0.03f, 0.03f, 0.035f), new Color(0.16f, 0.17f, 0.19f)),
                Tint(new Color(0.06f, 0.05f, 0.02f), new Color(0.92f, 0.74f, 0.18f)),
            };
            var glass = Tint(new Color(0.02f, 0.03f, 0.05f), new Color(0.22f, 0.30f, 0.38f));

            float wallOuter = LabRoom.ZMin - 0.3f;
            float walkFront = wallOuter, walkBack = wallOuter - 4.8f;
            float roadBack = walkBack - 8.4f;
            float walkFar = roadBack - 4.6f;

            Gen.Box("Ground", city, new Vector3(0, -0.2f, -110f), new Vector3(360f, 0.16f, 260f), asphalt);
            Gen.Box("PavementNear", city, new Vector3(0, -0.06f, (walkFront + walkBack) / 2f), new Vector3(300f, 0.12f, walkFront - walkBack), paving);
            Gen.Box("CurbNear", city, new Vector3(0, -0.06f, walkBack - 0.1f), new Vector3(300f, 0.12f, 0.2f), curb);
            Gen.Box("PavementFar", city, new Vector3(0, -0.06f, (roadBack + walkFar) / 2f), new Vector3(300f, 0.12f, roadBack - walkFar), paving);
            Gen.Box("CurbFar", city, new Vector3(0, -0.06f, roadBack + 0.1f), new Vector3(300f, 0.12f, 0.2f), curb);
            Gen.Box("Plaza", city, new Vector3(0, -0.06f, walkFar - 60f), new Vector3(300f, 0.12f, 120f), paving);
            Gen.Box("Threshold", city, new Vector3(doorX, -0.05f, LabRoom.ZMin - 0.15f), new Vector3(1.0f, 0.1f, 0.3f), curb);

            float roadMid = (walkBack + roadBack) / 2f;
            for (float x = -110f; x <= 110f; x += 6f)
                Gen.Box("Dash", city, new Vector3(x, 0.006f - 0.1f, roadMid), new Vector3(2.6f, 0.01f, 0.16f), yellowLine);
            for (int i = 0; i < 9; i++)
                Gen.Box("Crosswalk", city, new Vector3(doorX - 1.6f + i * 0.4f, 0.006f - 0.1f, roadMid), new Vector3(0.24f, 0.01f, walkBack - roadBack - 0.6f), paint);

            for (float x = -63f; x <= 63f; x += 14f)
            {
                if (Mathf.Abs(x - doorX) < 3.5f) continue;
                Lamp(city, new Vector3(x, 0, walkBack + 0.6f), lampPole, lampHead);
                Lamp(city, new Vector3(x + 7f, 0, roadBack - 0.6f), lampPole, lampHead);
            }
            for (float x = -58f; x <= 58f; x += 11f)
            {
                if (Mathf.Abs(x - doorX) < 3.5f) continue;
                Tree(city, new Vector3(x + 3f, 0, walkBack + 1.4f), trunk, leaf);
                Tree(city, new Vector3(x - 2f, 0, roadBack - 1.4f), trunk, leaf);
            }

            var tyreM0 = Tint(new Color(0.01f, 0.01f, 0.015f), new Color(0.06f, 0.06f, 0.07f));
            var rnd = new System.Random(11);
            for (int i = 0; i < 16; i++)
            {
                float x = -64f + i * 8.6f + (float)rnd.NextDouble() * 2f;
                if (Mathf.Abs(x - doorX) < 3f) continue;
                bool near = i % 2 == 0;
                Car(city, new Vector3(x, 0, near ? walkBack - 1.4f : roadBack + 1.4f), carColors[rnd.Next(carColors.Length)], glass, tyreM0, rnd.Next(3));
            }

            var tyreM = Tint(new Color(0.01f, 0.01f, 0.015f), new Color(0.06f, 0.06f, 0.07f));
            for (int i = 0; i < 7; i++)
            {
                bool east = i % 2 == 0;
                var car = Car(city, new Vector3(-60f + i * 17f, 0, roadMid + (east ? -1.9f : 1.9f)), i == 3 ? carColors[4] : carColors[rnd.Next(carColors.Length)], glass, tyreM, i == 3 ? 3 : i == 5 ? 2 : rnd.Next(2));
                if (!east) car.transform.rotation = Quaternion.Euler(0, 180f, 0);
                var mv = car.AddComponent<Mover>();
                mv.Speed = (east ? 1f : -1f) * (5f + (float)rnd.NextDouble() * 4f); mv.Min = -70f; mv.Max = 70f;
            }
            var skins = new[] { Tint(new Color(0.03f, 0.02f, 0.02f), new Color(0.45f, 0.30f, 0.22f)), Tint(new Color(0.04f, 0.03f, 0.03f), new Color(0.85f, 0.65f, 0.52f)), Tint(new Color(0.02f, 0.015f, 0.015f), new Color(0.28f, 0.18f, 0.12f)) };
            for (int i = 0; i < 12; i++)
            {
                bool near = i % 2 == 0;
                float x = -40f + i * 7.3f;
                if (Mathf.Abs(x - doorX) < 4f) x += 8f;
                Person(city, new Vector3(x, 0, near ? walkBack + 1.2f + (float)rnd.NextDouble() * 1.8f : roadBack - 1.2f - (float)rnd.NextDouble() * 1.8f), carColors[rnd.Next(carColors.Length)], skins[rnd.Next(skins.Length)], (near ? 1f : -1f) * (0.9f + (float)rnd.NextDouble() * 0.6f));
            }
            BusShelter(city, new Vector3(-22f, 0, walkBack + 1.6f), lampPole, glass, curb);
            BusShelter(city, new Vector3(24f, 0, roadBack - 1.6f), lampPole, glass, curb);
            TrafficLight(city, new Vector3(doorX + 2.4f, 0, walkBack + 0.5f), lampPole);
            TrafficLight(city, new Vector3(doorX - 2.4f, 0, roadBack - 0.5f), lampPole);
            var sign = Ui.Canvas("LabSign", city, new Vector2(2000, 200), new Vector3(0, 2.9f, wallOuter - 0.10f));
            sign.transform.localScale = Vector3.one * 0.009f;
            Ui.Label(sign.transform, "Cetus Corporation (Molecular Biology Lab)", 40, Color.white, TextAnchor.MiddleCenter, new Vector2(2000, 200), Vector2.zero, FontStyle.Bold, false);
            Gen.Box("LabSignBand", city, new Vector3(0, 2.9f, wallOuter - 0.02f), new Vector3(16f, 0.8f, 0.04f), roof);
            Gen.Box("DoorMat", city, new Vector3(doorX, 0.005f, wallOuter - 0.7f), new Vector3(1.8f, 0.01f, 1.2f), paint);
            foreach (var bx in new[] { doorX - 1.3f, doorX + 1.3f })
                Gen.Prim(PrimitiveType.Cylinder, "Bollard", city, new Vector3(bx, 0.45f, walkBack + 0.5f), new Vector3(0.18f, 0.45f, 0.18f), curb);

            Row(city, rnd, walkFar - 0.1f, 11, 14f, 26f, 12f, 44f, bodies, windows, roof, true);
            Row(city, rnd, walkFar - 40f, 9, 18f, 32f, 30f, 70f, bodies, windows, roof, true);
            Row(city, rnd, walkFar - 86f, 8, 24f, 40f, 40f, 90f, bodies, windows, roof, false);
        }

        static void Row(Transform parent, System.Random rnd, float zFront, int count, float wMin, float wMax, float hMin, float hMax,
                        Material[] bodies, Material windows, Material roof, bool windowed)
        {
            float x = -118f;
            for (int i = 0; i < count * 2 && x < 118f; i++)
            {
                float w = Mathf.Lerp(wMin, wMax, (float)rnd.NextDouble());
                float h = Mathf.Lerp(hMin, hMax, (float)rnd.NextDouble());
                float d = Mathf.Lerp(12f, 22f, (float)rnd.NextDouble());
                var body = bodies[rnd.Next(bodies.Length)];
                var b = new Vector3(x + w / 2f, 0, zFront - d / 2f);
                Gen.Box("Building", parent, new Vector3(b.x, h / 2f, b.z), new Vector3(w, h, d), body);
                Gen.Box("Roof", parent, new Vector3(b.x, h + 0.3f, b.z), new Vector3(w + 0.3f, 0.6f, d + 0.3f), roof);
                if (windowed)
                {
                    Gen.Box("Shopfront", parent, new Vector3(b.x, 1.4f, zFront + 0.04f), new Vector3(w * 0.88f, 2.5f, 0.06f), windows);
                    Gen.Box("Awning", parent, new Vector3(b.x, 3.0f, zFront + 0.6f), new Vector3(w * 0.7f, 0.1f, 1.2f), bodies[rnd.Next(bodies.Length)]);
                    if (h > 24f)
                    {
                        Gen.Prim(PrimitiveType.Cylinder, "Antenna", parent, new Vector3(b.x + w * 0.25f, h + 3.6f, b.z), new Vector3(0.18f, 3.2f, 0.18f), roof);
                        Gen.Box("RoofPlant", parent, new Vector3(b.x - w * 0.2f, h + 1.4f, b.z), new Vector3(w * 0.3f, 1.6f, d * 0.4f), roof);
                    }
                    for (float y = 3.4f; y < h - 2f; y += 3.6f)
                        Gen.Box("Windows", parent, new Vector3(b.x, y, zFront + 0.03f), new Vector3(w * 0.9f, 1.5f, 0.06f), windows);
                }
                x += w + 1.5f + (float)rnd.NextDouble() * 3f;
            }
        }

        static void Lamp(Transform parent, Vector3 p, Material pole, Material head)
        {
            Gen.Prim(PrimitiveType.Cylinder, "LampPole", parent, p + new Vector3(0, 2.6f, 0), new Vector3(0.12f, 2.6f, 0.12f), pole);
            Gen.Box("LampArm", parent, p + new Vector3(0, 5.15f, 0.4f), new Vector3(0.1f, 0.1f, 0.9f), pole);
            Gen.Box("LampHead", parent, p + new Vector3(0, 5.08f, 0.8f), new Vector3(0.35f, 0.1f, 0.6f), head);
        }

        static void Tree(Transform parent, Vector3 p, Material trunk, Material leaf)
        {
            Gen.Prim(PrimitiveType.Cylinder, "TreeTrunk", parent, p + new Vector3(0, 1.1f, 0), new Vector3(0.28f, 1.1f, 0.28f), trunk);
            Gen.Prim(PrimitiveType.Sphere, "TreeCrown", parent, p + new Vector3(0, 3.1f, 0), new Vector3(2.6f, 2.8f, 2.6f), leaf);
        }

        static Material headM, tailM, trimM, bumperM;

        static GameObject Car(Transform parent, Vector3 p, Material body, Material glass, Material tyre = null, int kind = 0)
        {
            headM = headM != null ? headM : Mats.UnlitNew(new Color(1f, 0.96f, 0.8f));
            tailM = tailM != null ? tailM : Mats.UnlitNew(new Color(1f, 0.12f, 0.1f));
            trimM = trimM != null ? trimM : Mats.Lit(new Color(0.03f, 0.03f, 0.035f), null, 0.4f);
            bumperM = bumperM != null ? bumperM : Mats.Lit(new Color(0.55f, 0.57f, 0.6f), null, 0.7f, 0.6f);
            var car = new GameObject("Car").transform;
            car.SetParent(parent, false);
            car.position = p;
            float L = kind == 3 ? 10.5f : kind == 2 ? 4.8f : kind == 1 ? 3.9f : 4.5f;
            float W = kind == 3 ? 2.5f : 1.85f;
            float bodyH = kind == 3 ? 2.6f : kind == 2 ? 1.5f : 0.75f, floorY = 0.32f;
            Gen.Box("Body", car, new Vector3(0, floorY + bodyH / 2f, 0), new Vector3(L, bodyH, W), body);
            Gen.Box("Sill", car, new Vector3(0, floorY + 0.06f, 0), new Vector3(L - 0.5f, 0.14f, W + 0.02f), trimM);
            Gen.Box("BumperF", car, new Vector3(L / 2f + 0.04f, floorY + 0.2f, 0), new Vector3(0.14f, 0.26f, W - 0.1f), bumperM);
            Gen.Box("BumperR", car, new Vector3(-L / 2f - 0.04f, floorY + 0.2f, 0), new Vector3(0.14f, 0.26f, W - 0.1f), bumperM);
            if (kind == 3)
            {
                foreach (var sz in new[] { -1f, 1f }) Gen.Box("BusWindows", car, new Vector3(0, floorY + 1.7f, sz * (W / 2f + 0.01f)), new Vector3(L - 1.4f, 0.9f, 0.04f), glass);
                Gen.Box("Windscreen", car, new Vector3(L / 2f + 0.01f, floorY + 1.55f, 0), new Vector3(0.04f, 1.2f, W - 0.3f), glass);
                Gen.Box("RoofPod", car, new Vector3(-1f, floorY + bodyH + 0.18f, 0), new Vector3(2.4f, 0.36f, 1.4f), bumperM);
            }
            else
            {
                float cabL = kind == 2 ? L - 0.9f : kind == 1 ? L * 0.62f : L * 0.5f, cabH = kind == 2 ? 0.55f : 0.62f;
                float cabX = kind == 2 ? -0.25f : kind == 1 ? -0.35f : -0.25f, cabY = floorY + bodyH + cabH / 2f;
                if (kind == 2) Gen.Box("VanRoof", car, new Vector3(cabX, floorY + bodyH + 0.04f, 0), new Vector3(cabL, 0.7f, W - 0.1f), body);
                else
                {
                    Gen.Box("Cabin", car, new Vector3(cabX, cabY, 0), new Vector3(cabL, cabH, W - 0.22f), glass);
                    Gen.Box("CabinRoof", car, new Vector3(cabX, floorY + bodyH + cabH + 0.03f, 0), new Vector3(cabL - 0.5f, 0.07f, W - 0.28f), body);
                    foreach (var px in new[] { cabX + cabL * 0.18f, cabX - cabL * 0.18f })
                        Gen.Box("Pillar", car, new Vector3(px, cabY, 0), new Vector3(0.1f, cabH + 0.02f, W - 0.18f), body);
                    var ws = Gen.Box("Windscreen", car, new Vector3(cabX + cabL / 2f + 0.02f, cabY - 0.02f, 0), new Vector3(0.07f, cabH * 1.25f, W - 0.3f), glass);
                    ws.transform.localRotation = Quaternion.Euler(0, 0, 38f);
                    var rw = Gen.Box("RearWindow", car, new Vector3(cabX - cabL / 2f - 0.02f, cabY - 0.02f, 0), new Vector3(0.07f, cabH * 1.25f, W - 0.3f), glass);
                    rw.transform.localRotation = Quaternion.Euler(0, 0, kind == 1 ? -18f : -38f);
                }
                if (kind == 2)
                {
                    Gen.Box("VanGlass", car, new Vector3(L / 2f - 0.75f, floorY + bodyH + 0.12f, 0), new Vector3(0.05f, 0.5f, W - 0.3f), glass);
                    Gen.Box("VanSideGlass", car, new Vector3(L / 2f - 1.2f, floorY + bodyH - 0.15f, 0), new Vector3(1.3f, 0.5f, W + 0.01f), glass);
                }
            }
            foreach (var sz in new[] { -1f, 1f })
            {
                Gen.Box("Headlight", car, new Vector3(L / 2f + 0.01f, floorY + bodyH - 0.2f, sz * (W / 2f - 0.28f)), new Vector3(0.05f, 0.14f, 0.38f), headM);
                Gen.Box("Taillight", car, new Vector3(-L / 2f - 0.01f, floorY + bodyH - 0.2f, sz * (W / 2f - 0.28f)), new Vector3(0.05f, 0.14f, 0.38f), tailM);
                if (kind != 3) Gen.Box("Mirror", car, new Vector3(L * 0.2f, floorY + bodyH + 0.1f, sz * (W / 2f + 0.1f)), new Vector3(0.14f, 0.12f, 0.1f), body);
                float doorSpan = kind == 3 ? 1.6f : 1.0f;
                foreach (var dx in kind == 3 ? new[] { 2.5f, -1.5f } : new[] { 0.35f, -0.85f })
                    Gen.Box("DoorLine", car, new Vector3(dx, floorY + bodyH / 2f + 0.05f, sz * (W / 2f + 0.005f)), new Vector3(0.015f, bodyH - 0.2f, 0.01f), trimM);
            }
            if (kind == 2) Gen.Box("TaxiSign", car, new Vector3(-0.2f, floorY + bodyH + 0.82f, 0), new Vector3(0.5f, 0.16f, 0.26f), headM);
            if (tyre != null)
            {
                float wr = kind == 3 ? 0.5f : 0.34f;
                foreach (var wx in kind == 3 ? new[] { 3.2f, -3.3f } : new[] { L * 0.31f, -L * 0.31f })
                    foreach (var wz in new[] { -1f, 1f })
                    {
                        float z = wz * (W / 2f - 0.02f);
                        Gen.Prim(PrimitiveType.Cylinder, "Wheel", car, new Vector3(wx, wr, z), new Vector3(wr * 2f, 0.12f, wr * 2f), tyre, false, Quaternion.Euler(90f, 0, 0));
                        Gen.Prim(PrimitiveType.Cylinder, "Hub", car, new Vector3(wx, wr, z + wz * 0.07f), new Vector3(wr * 1.1f, 0.02f, wr * 1.1f), bumperM, false, Quaternion.Euler(90f, 0, 0));
                    }
            }
            return car.gameObject;
        }

        static void Person(Transform parent, Vector3 p, Material shirt, Material skin, float speed)
        {
            var g = new GameObject("Person").transform;
            g.SetParent(parent, false);
            g.position = p;
            Gen.Prim(PrimitiveType.Capsule, "Body", g, new Vector3(0, 0.9f, 0), new Vector3(0.38f, 0.6f, 0.26f), shirt);
            Gen.Prim(PrimitiveType.Sphere, "Head", g, new Vector3(0, 1.62f, 0), Vector3.one * 0.23f, skin);
            var m = g.gameObject.AddComponent<Mover>();
            m.Speed = speed; m.Min = -45f; m.Max = 45f;
        }

        static void BusShelter(Transform parent, Vector3 p, Material frame, Material glass, Material bench)
        {
            Gen.Box("ShelterRoof", parent, p + new Vector3(0, 2.5f, 0), new Vector3(3.2f, 0.1f, 1.4f), frame);
            Gen.Box("ShelterBack", parent, p + new Vector3(0, 1.3f, -0.65f), new Vector3(3.0f, 2.2f, 0.04f), glass);
            foreach (var sx in new[] { -1.5f, 1.5f })
                Gen.Box("ShelterPost", parent, p + new Vector3(sx, 1.25f, 0.6f), new Vector3(0.07f, 2.5f, 0.07f), frame);
            Gen.Box("ShelterBench", parent, p + new Vector3(0, 0.5f, -0.4f), new Vector3(2.4f, 0.08f, 0.4f), bench);
        }

        static void TrafficLight(Transform parent, Vector3 p, Material pole)
        {
            Gen.Prim(PrimitiveType.Cylinder, "TLPole", parent, p + new Vector3(0, 1.8f, 0), new Vector3(0.12f, 1.8f, 0.12f), pole);
            Gen.Box("TLBox", parent, p + new Vector3(0, 3.5f, 0), new Vector3(0.35f, 1.0f, 0.3f), pole);
            var red = Mats.UnlitNew(new Color(1f, 0.15f, 0.1f)); var amb = Mats.UnlitNew(new Color(1f, 0.7f, 0.1f)); var grn = Mats.UnlitNew(new Color(0.15f, 0.9f, 0.35f));
            Gen.Prim(PrimitiveType.Sphere, "Red", parent, p + new Vector3(0, 3.8f, -0.16f), Vector3.one * 0.2f, red);
            Gen.Prim(PrimitiveType.Sphere, "Amber", parent, p + new Vector3(0, 3.5f, -0.16f), Vector3.one * 0.2f, amb);
            Gen.Prim(PrimitiveType.Sphere, "Green", parent, p + new Vector3(0, 3.2f, -0.16f), Vector3.one * 0.2f, grn);
        }
    }
}
