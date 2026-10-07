using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    public static class StepTargets
    {
        static readonly Dictionary<string, GameObject> Map = new Dictionary<string, GameObject>();
        public static event Action<string> Clicked;

        public static void Register(string id, GameObject go) { if (!string.IsNullOrEmpty(id) && go != null) Map[id] = go; }
        public static void Unregister(string id, GameObject go) { if (id != null && Map.TryGetValue(id, out var g) && g == go) Map.Remove(id); }
        public static GameObject Find(string id) => id != null && Map.TryGetValue(id, out var g) && g != null ? g : null;
        public static void Raise(string id) { if (!string.IsNullOrEmpty(id)) Clicked?.Invoke(id); }
        public static void Clear() => Map.Clear();
    }
}
