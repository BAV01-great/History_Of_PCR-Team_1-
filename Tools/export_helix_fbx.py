#!/usr/bin/env python3
"""
Exports the "neon" DNA double helix (the same geometry Assets/PCR/Scripts/Dna/DnaHelix.cs builds at runtime)
as an ASCII FBX 7.4 file, so it can be imported into Unity, Blender or any DCC tool.

  python3 Tools/export_helix_fbx.py [out.fbx] [pairs] [radius] [rise] [twist_deg] [seed]

One mesh, six materials (one draw call each in Unity), team colour key from the 3D asset handoff:
  Mat_Backbone_A dark grey, Mat_Backbone_B light grey, Mat_Base_A red, Mat_Base_T blue, Mat_Base_G yellow, Mat_Base_C green.
Units: metres. Axis: Y up. Origin on the helix axis at the bottom base pair. Right-handed helix, A pairs with T and G with C.
Coordinates are written so that Unity's importer reproduces exactly the geometry built in code (Unity mirrors X on import).
"""
import math
import random
import sys

# ---------------------------------------------------------------- geometry (Unity-space, mirrors MeshBuilder.cs)
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def norm(a):
    l = math.sqrt(dot(a, a))
    return (a[0] / l, a[1] / l, a[2] / l) if l > 1e-12 else (0.0, 0.0, 0.0)
def lerp(a, b, t): return add(mul(a, 1 - t), mul(b, t))

class Mesh:
    """Triangles with an outward normal each, tagged with a material index."""
    def __init__(self):
        self.tris = []  # (p0, p1, p2, outward, material)

    def quad(self, a, b, c, d, outward, mat):
        self.tris.append((a, b, c, outward, mat))
        self.tris.append((a, c, d, outward, mat))

    def prism(self, a, b, r, sides, mat):
        axis = sub(b, a)
        if dot(axis, axis) < 1e-8: return
        axis = norm(axis)
        up = (0, 1, 0) if abs(axis[1]) < 0.9 else (1, 0, 0)
        u = norm(cross(axis, up)); w = cross(axis, u)
        for s in range(sides):
            a0 = 2 * math.pi * s / sides; a1 = 2 * math.pi * (s + 1) / sides
            d0 = add(mul(u, math.cos(a0)), mul(w, math.sin(a0)))
            d1 = add(mul(u, math.cos(a1)), mul(w, math.sin(a1)))
            self.quad(add(a, mul(d0, r)), add(a, mul(d1, r)), add(b, mul(d1, r)), add(b, mul(d0, r)), add(d0, d1), mat)

    def blob(self, c, r, mat):
        px = add(c, (r, 0, 0)); nx = add(c, (-r, 0, 0)); py = add(c, (0, r, 0)); ny = add(c, (0, -r, 0))
        pz = add(c, (0, 0, r)); nz = add(c, (0, 0, -r))
        for t in [(py, px, pz), (py, pz, nx), (py, nx, nz), (py, nz, px), (ny, pz, px), (ny, nx, pz), (ny, nz, nx), (ny, px, nz)]:
            cen = mul(add(add(t[0], t[1]), t[2]), 1 / 3)
            self.tris.append((t[0], t[1], t[2], sub(cen, c), mat))

MATERIALS = [  # name, diffuse (sRGB-ish 0..1), emissive scale
    ("Mat_Backbone_A", (0.42, 0.46, 0.52)),
    ("Mat_Backbone_B", (0.80, 0.84, 0.90)),
    ("Mat_Base_A", (1.00, 0.25, 0.25)),   # A red
    ("Mat_Base_T", (0.25, 0.45, 1.00)),   # T blue
    ("Mat_Base_G", (1.00, 0.85, 0.20)),   # G yellow
    ("Mat_Base_C", (0.25, 0.90, 0.40)),   # C green
]
BASE_MAT = [2, 3, 4, 5]  # A, T, G, C   (index ^ 1 is the complementary base)

def build_helix(pairs, radius, rise, twist_deg, seed):
    m = Mesh(); rnd = random.Random(seed)
    bb = radius * 0.16; rr = radius * 0.075
    prev1 = prev2 = None
    for i in range(pairs):
        ang = math.radians(i * twist_deg); y = i * rise
        p1 = (math.cos(ang) * radius, y, math.sin(ang) * radius)
        p2 = (-p1[0], y, -p1[2])
        if i > 0:
            m.prism(prev1, p1, bb, 5, 0); m.prism(prev2, p2, bb, 5, 1)
        m.blob(p1, bb * 1.25, 0); m.blob(p2, bb * 1.25, 1)
        prev1, prev2 = p1, p2
        b = rnd.randrange(4); comp = b ^ 1
        mid = mul(add(p1, p2), 0.5)
        m.prism(lerp(p1, p2, 0.08), mid, rr, 4, BASE_MAT[b])
        m.prism(lerp(p2, p1, 0.08), mid, rr, 4, BASE_MAT[comp])
    return m

# ---------------------------------------------------------------- FBX 7.4 ASCII writer
def fbx_coords(p):  # Unity-space -> FBX-space (Unity's importer mirrors X)
    return (-p[0], p[1], p[2])

def write_fbx(path, mesh, name="DNA_Helix_Neon"):
    verts, normals, indices, mats = [], [], [], []
    for (p0, p1, p2, outward, mat) in mesh.tris:
        a, b, c = fbx_coords(p0), fbx_coords(p1), fbx_coords(p2)
        n_out = fbx_coords(outward)
        face_n = cross(sub(b, a), sub(c, a))
        if dot(face_n, n_out) < 0:   # FBX front faces are counter-clockwise (right-handed)
            b, c = c, b
            face_n = cross(sub(b, a), sub(c, a))
        n = norm(face_n)
        k = len(verts)
        verts += [a, b, c]; normals += [n, n, n]
        indices += [k, k + 1, -(k + 2) - 1]
        mats.append(mat)

    fmt = lambda t: ",".join("%.6f" % x for tup in t for x in tup)
    GEO, MOD = 1000000, 2000000
    lines = []
    w = lines.append
    w("; FBX 7.4.0 project file")
    w("FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreator: \"History of PCR - export_helix_fbx.py\"\n}")
    w("GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {")
    for p in [('UpAxis', 'int', 'Integer', '', 1), ('UpAxisSign', 'int', 'Integer', '', 1), ('FrontAxis', 'int', 'Integer', '', 2),
              ('FrontAxisSign', 'int', 'Integer', '', 1), ('CoordAxis', 'int', 'Integer', '', 0), ('CoordAxisSign', 'int', 'Integer', '', 1),
              ('OriginalUpAxis', 'int', 'Integer', '', 1), ('OriginalUpAxisSign', 'int', 'Integer', '', 1),
              ('UnitScaleFactor', 'double', 'Number', '', 100), ('OriginalUnitScaleFactor', 'double', 'Number', '', 100)]:
        w('\t\tP: "%s", "%s", "%s", "",%s' % (p[0], p[1], p[2], p[4]))
    w("\t}\n}")
    w("Definitions:  {\n\tVersion: 100\n\tCount: %d" % (2 + len(MATERIALS)))
    w('\tObjectType: "Model" {\n\t\tCount: 1\n\t}')
    w('\tObjectType: "Geometry" {\n\t\tCount: 1\n\t}')
    w('\tObjectType: "Material" {\n\t\tCount: %d\n\t}' % len(MATERIALS))
    w("}")
    w("Objects:  {")
    w('\tGeometry: %d, "Geometry::%s", "Mesh" {' % (GEO, name))
    w("\t\tVertices: *%d {\n\t\t\ta: %s\n\t\t}" % (len(verts) * 3, fmt(verts)))
    w("\t\tPolygonVertexIndex: *%d {\n\t\t\ta: %s\n\t\t}" % (len(indices), ",".join(str(i) for i in indices)))
    w("\t\tGeometryVersion: 124")
    w('\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygonVertex"\n\t\t\tReferenceInformationType: "Direct"')
    w("\t\t\tNormals: *%d {\n\t\t\t\ta: %s\n\t\t\t}\n\t\t}" % (len(normals) * 3, fmt(normals)))
    w('\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: ""\n\t\t\tMappingInformationType: "ByPolygon"\n\t\t\tReferenceInformationType: "IndexToDirect"')
    w("\t\t\tMaterials: *%d {\n\t\t\t\ta: %s\n\t\t\t}\n\t\t}" % (len(mats), ",".join(str(i) for i in mats)))
    w('\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementNormal"\n\t\t\t\tTypedIndex: 0\n\t\t\t}')
    w('\t\t\tLayerElement:  {\n\t\t\t\tType: "LayerElementMaterial"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}')
    w("\t}")
    w('\tModel: %d, "Model::%s", "Mesh" {\n\t\tVersion: 232\n\t\tProperties70:  {\n\t\t\tP: "DefaultAttributeIndex", "int", "Integer", "",0\n\t\t}\n\t\tShading: T\n\t\tCulling: "CullingOff"\n\t}' % (MOD, name))
    for i, (mname, col) in enumerate(MATERIALS):
        w('\tMaterial: %d, "Material::%s", "" {\n\t\tVersion: 102\n\t\tShadingModel: "phong"\n\t\tMultiLayer: 0\n\t\tProperties70:  {' % (3000000 + i, mname))
        w('\t\t\tP: "DiffuseColor", "Color", "", "A",%.4f,%.4f,%.4f' % col)
        w('\t\t\tP: "Diffuse", "Vector3D", "Vector", "",%.4f,%.4f,%.4f' % col)
        w('\t\t\tP: "EmissiveColor", "Color", "", "A",%.4f,%.4f,%.4f' % tuple(c * 0.5 for c in col))
        w('\t\t\tP: "EmissiveFactor", "Number", "", "A",1')
        w('\t\t\tP: "Shininess", "Number", "", "A",40')
        w("\t\t}\n\t}")
    w("}")
    w("Connections:  {")
    w('\tC: "OO",%d,0' % MOD)
    w('\tC: "OO",%d,%d' % (GEO, MOD))
    for i in range(len(MATERIALS)):
        w('\tC: "OO",%d,%d' % (3000000 + i, MOD))
    w("}")
    with open(path, "w") as f:
        f.write("\n".join(lines) + "\n")
    return len(mesh.tris), len(verts)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "DNA_Helix_Neon.fbx"
    pairs = int(sys.argv[2]) if len(sys.argv) > 2 else 34
    radius = float(sys.argv[3]) if len(sys.argv) > 3 else 0.42
    rise = float(sys.argv[4]) if len(sys.argv) > 4 else 0.095
    twist = float(sys.argv[5]) if len(sys.argv) > 5 else 34.0
    seed = int(sys.argv[6]) if len(sys.argv) > 6 else 11
    tris, v = write_fbx(out, build_helix(pairs, radius, rise, twist, seed))
    print(f"wrote {out}: {tris} triangles, {v} vertices, {len(MATERIALS)} materials, height {(pairs - 1) * rise:.2f} m")
