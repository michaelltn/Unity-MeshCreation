using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class MultiColorProgressBar : MonoBehaviour
{
    #region Inspector Properties

    /// <summary>
    /// Border texture to be used for rendering.
    /// This will be 9-sliced and the middle will not be rendered.
    /// </summary>
    //public Material material;
    public string colorProperty = "_Color";

    public Vector2 size = Vector2.one;
    Vector2 _size;

    float _progress = 0;
    public float progress { get { return _progress; } }
    List<float> fillValues = new List<float>();
    List<Color> fillColors = new List<Color>();

    #endregion



    #region Values

    public void addValue(float value, Color color)
    {
        if (_progress < 1f)
        {
            float v = value;
            if (_progress + value > 1f)
                v = 1f - _progress;
            _progress += v;
            fillValues.Add(v);
            fillColors.Add(color);
            dirty = true;
        }
    }

    public void resetValue()
    {
        _progress = 0;
        fillValues.Clear();
        fillColors.Clear();
        dirty = true;
    }

    #endregion


    #region Visibility

    public bool showing { get { return meshRenderer.enabled; } }

    public void show()
    {
        meshRenderer.enabled = true;
    }

    public void hide()
    {
        meshRenderer.enabled = false;
    }

    public Color getColor()
    {
        if (meshRenderer.material.HasProperty(colorProperty))
        {
            return meshRenderer.material.GetColor(colorProperty);
        }
        return Color.white;
    }

    public void setColor(Color c)
    {
        if (meshRenderer.material.HasProperty(colorProperty))
            meshRenderer.material.SetColor(colorProperty, c);
    }

    public float getAlpha()
    {
        if (meshRenderer.material.HasProperty(colorProperty))
        {
            Color c = meshRenderer.material.GetColor(colorProperty);
            return c.a;
        }
        return 1f;
    }

    public void setAlpha(float a)
    {
        if (meshRenderer.material.HasProperty(colorProperty))
        {
            Color c = meshRenderer.material.GetColor(colorProperty);
            if (c.a != Mathf.Clamp01(a))
            {
                c.a = Mathf.Clamp01(a);
                meshRenderer.material.SetColor(colorProperty, c);
            }
        }
    }

    #endregion


    #region Mesh

    //Mesh mesh;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    private Mesh meshA, meshB;
    private Mesh backMesh { get { return aIsFront ? meshB : meshA; } }
    private bool aIsFront = true;

    private void swapMeshBuffers()
    {
        aIsFront = !aIsFront;
        meshFilter.sharedMesh = aIsFront ? meshA : meshB;
    }

    List<Vector3> verts;
    List<Vector2> uvs;
    List<Color> colors;
    List<int> tris;

    /*
     * Quad vert order
     *      1 2
     *      0 3
     * Quad tris
     *      { 0, 1, 2, 0, 2, 3 }
     *      { -4, -3, -2, -4, -2, -1 }
     * */

    bool dirty = false;

    bool isDirty
    {
        get
        {
            return dirty ||
                size.x != _size.x ||
                size.y != _size.y;
        }
    }

    void clean()
    {
        _size = size;
        dirty = false;
    }

    /// <summary>
    /// Use from behaviours that change the properties of this gauge in LateUpdate after this gauge has already run its LateUpdate.
    /// </summary>
    public void commit()
    {
        if (isDirty)
        {
            buildMesh();
        }
    }

    void buildMesh()
    {
        clean();

        verts.Clear();
        uvs.Clear();
        colors.Clear();
        tris.Clear();

        float x1, y1, x2, y2;

        x1 = size.x * -0.5f;
        y1 = size.y * -0.5f;
        y2 = size.y * 0.5f;

        for (int i = 0; i < fillValues.Count; i++)
        {
            x2 = x1 + size.x * Mathf.Clamp01(fillValues[i]);

            verts.Add(new Vector3(x1, y1, 0));
            verts.Add(new Vector3(x1, y2, 0));
            verts.Add(new Vector3(x2, y2, 0));
            verts.Add(new Vector3(x2, y1, 0));

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0));

            colors.Add(fillColors[i]);
            colors.Add(fillColors[i]);
            colors.Add(fillColors[i]);
            colors.Add(fillColors[i]);

            tris.Add(i * 4);
            tris.Add(i * 4 + 1);
            tris.Add(i * 4 + 2);
            tris.Add(i * 4);
            tris.Add(i * 4 + 2);
            tris.Add(i * 4 + 3);

            x1 = x2;
        }

        backMesh.Clear();
        backMesh.vertices = verts.ToArray();
        backMesh.uv = uvs.ToArray();
        backMesh.colors = colors.ToArray();
        backMesh.triangles = tris.ToArray();
        backMesh.RecalculateBounds();

        swapMeshBuffers();
    }

    #endregion



    #region MonoBehaviour

    void Awake()
    {
        //mesh = new Mesh();
        meshA = new Mesh();
        meshA.hideFlags = HideFlags.DontSave;
        meshB = new Mesh();
        meshB.hideFlags = HideFlags.DontSave;

        verts = new List<Vector3>();
        uvs = new List<Vector2>();
        colors = new List<Color>();
        tris = new List<int>();
    }

    void OnEnable()
    {
        if (meshFilter == null) meshFilter = this.gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = this.gameObject.AddComponent<MeshFilter>();
        meshFilter.hideFlags = HideFlags.HideInInspector;

        if (meshRenderer == null) meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.hideFlags = HideFlags.None;

        //meshRenderer.material = material;
        buildMesh();
    }

    void LateUpdate()
    {
        if (isDirty)
        {
            buildMesh();
        }
    }

    void OnDestroy()
    {
        if (meshA != null)
        {
            DestroyImmediate(meshA);
            meshA = null;
        }

        if (meshB != null)
        {
            DestroyImmediate(meshB);
            meshB = null;
        }
    }

    #endregion



    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(this.transform.position, new Vector3(this.size.x, this.size.y, 0));
    }
}
