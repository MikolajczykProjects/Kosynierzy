using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Tiny procedural meshes for the effects (white vertex colours so vertex-coloured particle shaders tint them).
    /// Front faces are clockwise as Unity expects.
    /// </summary>
    public static class VfxMeshes
    {
        /// <summary>Unit cube whose faces sample a 4×1 shade strip: top bright, sides mid, bottom dark.</summary>
        public static Mesh ShadedCube()
        {
            var v = new Vector3[24];
            var n = new Vector3[24];
            var uv = new Vector2[24];
            var col = new Color32[24];
            var tri = new int[36];
            int vi = 0, ti = 0;
            Face(Vector3.up, Vector3.right, Vector3.back, 0.125f);
            Face(Vector3.down, Vector3.right, Vector3.forward, 0.875f);
            Face(Vector3.right, Vector3.back, Vector3.up, 0.375f);
            Face(Vector3.left, Vector3.forward, Vector3.up, 0.625f);
            Face(Vector3.forward, Vector3.right, Vector3.up, 0.375f);
            Face(Vector3.back, Vector3.left, Vector3.up, 0.625f);

            // u × v = normal; (0,2,1)(0,3,2) is clockwise seen from outside.
            void Face(Vector3 normal, Vector3 u, Vector3 w, float shadeU)
            {
                Vector3 c = normal * 0.5f;
                Vector3 a = u * 0.5f, b = w * 0.5f;
                // Make sure u × w points along the normal.
                if (Vector3.Dot(Vector3.Cross(u, w), normal) < 0f) b = -b;
                v[vi + 0] = c - a - b;
                v[vi + 1] = c - a + b;
                v[vi + 2] = c + a + b;
                v[vi + 3] = c + a - b;
                for (int k = 0; k < 4; k++)
                {
                    n[vi + k] = normal;
                    uv[vi + k] = new Vector2(shadeU, 0.5f);
                    col[vi + k] = new Color32(255, 255, 255, 255);
                }
                tri[ti++] = vi; tri[ti++] = vi + 2; tri[ti++] = vi + 1;
                tri[ti++] = vi; tri[ti++] = vi + 3; tri[ti++] = vi + 2;
                vi += 4;
            }

            var mesh = new Mesh { name = "vfx_cube" };
            mesh.vertices = v;
            mesh.normals = n;
            mesh.uv = uv;
            mesh.colors32 = col;
            mesh.triangles = tri;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>1×1 quad lying in XZ (normal +Y), UV 0..1 — ground rings and bullet holes.</summary>
        public static Mesh QuadXZ()
        {
            var mesh = new Mesh { name = "vfx_quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f),
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            var white = new Color32(255, 255, 255, 255);
            mesh.colors32 = new[] { white, white, white, white };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Cylinder along Y, height 1, diameter 1, smooth sides, flat caps — shell casings.</summary>
        public static Mesh Cylinder(int sides)
        {
            int ring = sides + 1;
            var v = new System.Collections.Generic.List<Vector3>();
            var n = new System.Collections.Generic.List<Vector3>();
            var uv = new System.Collections.Generic.List<Vector2>();
            var tri = new System.Collections.Generic.List<int>();

            // Sides: bottom/top pairs.
            for (int i = 0; i < ring; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(dir * 0.5f + Vector3.down * 0.5f); n.Add(dir); uv.Add(new Vector2(i / (float)sides, 0f));
                v.Add(dir * 0.5f + Vector3.up * 0.5f); n.Add(dir); uv.Add(new Vector2(i / (float)sides, 1f));
            }
            for (int i = 0; i < sides; i++)
            {
                int b0 = i * 2, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
                tri.Add(b0); tri.Add(t0); tri.Add(t1);
                tri.Add(b0); tri.Add(t1); tri.Add(b1);
            }

            Cap(0.5f, Vector3.up, true);
            Cap(-0.5f, Vector3.down, false);

            void Cap(float y, Vector3 normal, bool top)
            {
                int centre = v.Count;
                v.Add(new Vector3(0f, y, 0f)); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i < sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * 0.5f, y, Mathf.Sin(a) * 0.5f));
                    n.Add(normal);
                    uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < sides; i++)
                {
                    int p = centre + 1 + i, q = centre + 1 + (i + 1) % sides;
                    if (top) { tri.Add(centre); tri.Add(q); tri.Add(p); }
                    else { tri.Add(centre); tri.Add(p); tri.Add(q); }
                }
            }

            var mesh = new Mesh { name = "vfx_cylinder" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            var cols = new Color32[v.Count];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, 255);
            mesh.colors32 = cols;
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
