using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ArcGauge : MonoBehaviour
{
    #region Inspector Properties

    /// <summary>
    /// Texture to be used for rendering.  This should be created so that it can be 9-sliced twice.
    /// The first slice separates the border from the fill.  The second slice is for stretching
    /// the fill.
    /// </summary>
    public Material material;
    public string colorProperty = "_Color";


    public string sortingLayerName = "default";
    string _sortingLayerName;
    public int sortingOrderInLayer = 0;
    int _sortingOrderInLayer;

    /// <summary>
    /// Left, top, right, and bottom uv offsets from the edge to the border.
    /// </summary>
    public Rect borderUV;
    Rect _borderUV;

    /// <summary>
    /// Left, top, right, and bottom uv offsets used to 9-slice from the border.
    /// </summary>
    public Rect fillUV;
    Rect _fillUV;


    /// <summary>
    /// Number of sections in the gauge.  Must be > 0.
    /// </summary>
    public int sections = 3;
    int _sections;

    /// <summary>
    /// Distance of the inner edge from the origin.
    /// </summary>
    public float distance = 1f;
    float _distance;
    
    /// <summary>
    /// Distance from the inner edge to the outer edge.
    /// </summary>
    public float thickness = 0.25f;
    float _thickness;
    
    /// <summary>
    /// Starting angle.  If spread < 0, this is the left edge.  If spread > 0, this is the right edge.
    /// </summary>
    public float angle = 135f;
    float _angle;
    
    /// <summary>
    /// Angle between the left and right edges.  Enter a negative value for a clockwise gauge.
    /// </summary>
    public float spread = -90f;
    float _spread;

    /// <summary>
    /// World width of the border.  For lines parallel to the normal, this is the average thickness, as the quad
    /// will be wider at the outer edge and more narrow at the inner edge.  This is also used for the fill border.
    /// If borderWidth is not greater than 0, the border will not be created.
    /// </summary>
    public float borderWidth = 0.05f;
    float _borderWidth;

    /// <summary>
    /// Space in degrees between each section.
    /// </summary>
    public float sectionBuffer = 6f;
    float _sectionBuffer;

    /// <summary>
    /// Number of quads to compose the filled section from.  Higer values produce smoother arcs.  Minimum 3.
    /// </summary>
    public int slices = 6;
    int _slices;

    /// <summary>
    /// Color of the border.
    /// </summary>
    public Color borderColor = Color.white;
    Color _borderColor;

    /// <summary>
    /// Color of the fill.
    /// </summary>
    public Color fillColor = Color.white;
    Color _fillColor;

    /// <summary>
    /// Fill is determined by value.  Each section represents 1 so a gauge with 3 sections and a value of 2.5
    /// will have the first two sections completely filled with the last section half filled.
    /// </summary>
    public float value = 1f;
    float _value;

    #endregion



    #region Helper Methods

    /// <summary>
    /// Adds a section to the gauge that is the same size as the existing sections.
    /// If maintainCenter is true, the angle of the gauge will be adjusted to keep its
    /// center angle in the same place.
    /// </summary>
    public void addSection(bool maintainCenter = true)
    {
        float oldSpread = spread;
        float oldSectionSpread = this.sectionSpread();

        this.sections++;
        this.spread += (Mathf.Sign(this.spread) * (oldSectionSpread + sectionBuffer));
        if (maintainCenter)
        {
            this.angle -= ((spread - oldSpread) * 0.5f);
        }
    }

    /// <summary>
    /// Removes a section from the gauge keeping the remaining sections the same size.
    /// If maintainCenter is true, the angle of the gauge will be adjusted to keep its
    /// center angle in the same place.
    /// </summary>
    public bool removeSection(bool maintainCenter = true)
    {
        if (sections > 1)
        {
            float oldSpread = spread;
            float oldSectionSpread = this.sectionSpread();

            this.sections--;
            this.spread -= (Mathf.Sign(this.spread) * (oldSectionSpread + sectionBuffer));
            if (maintainCenter)
            {
                this.angle -= ((spread - oldSpread) * 0.5f);
            }
            return true;
        }
        return false;
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


    #region Maths

    float direction { get { return Mathf.Sign(spread); } }

    // directionless angle of section slice
    float sectionSpread()
    {
        return (Mathf.Abs(spread) - ((sections - 1) * sectionBuffer)) / sections;
    }

    // starting angle of a section
    float sectionAngle(int index)
    {
        return angle + index * direction * (sectionSpread() + sectionBuffer);
    }

    // directionless angle of border slice
    float borderSpread()
    {
        return angleFromIsosceles(borderWidth, distance + (thickness * 0.5f), true);
        //return 2 * distance * Mathf.Sin(a * 0.5f);
    }

    // directionless angle of middle slice
    float sliceSpread()
    {
        return (sectionSpread() - (2 * borderSpread())) / slices;
    }

    // directionless angle of fill based on value
    float fillSpread(int sectionIndex)
    {
        return Mathf.Clamp01(value - sectionIndex) * (sectionSpread() - (borderSpread() * 2));
    }

    // directionless angle of fill slice.
    // keeps left and right borders full until value creates a
    // fillSpread < 2(sliceSpread).
    float fillSliceSpread(int sectionIndex, int sliceIndex)
    {
        float fs = fillSpread(sectionIndex);
        if (sliceIndex == 0 || sliceIndex == slices - 1)
        {
            if (fs > borderSpread() * 2)
            {
                return borderSpread();
            }
            else
            {
                return fs / 2;
            }
        }
        else
        {
            if (fs > borderSpread() * 2)
            {
                return (fs - (borderSpread() * 2)) / (slices - 2);
            }
            else
            {
                return 0;
            }
        }
    }

    Vector3 polarToCartesian(float r, float angle, bool degrees = false)
    {
        float a = angle * (degrees ? Mathf.Deg2Rad : 1f);
        return new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), 0);
    }

    float angleFromIsosceles(float baseSize, float legSize, bool degrees = false)
    {
        return 2 * Mathf.Asin((baseSize * 0.5f) / legSize) * (degrees ? Mathf.Rad2Deg : 1f);
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

    bool isDirty
    {
        get
        {
            return
                borderUV != _borderUV ||
                fillUV != _fillUV ||
                sections != _sections ||
                distance != _distance ||
                thickness != _thickness ||
                angle != _angle ||
                spread != _spread ||
                borderWidth != _borderWidth ||
                sectionBuffer != _sectionBuffer ||
                slices != _slices ||
                borderColor != _borderColor ||
                fillColor != _fillColor ||
                value != _value;
        }
    }

    void clean()
    {
        _borderUV = borderUV;
        _fillUV = fillUV;
        _sections = sections = Mathf.Clamp(sections, 1, sections);
        _distance = distance = Mathf.Clamp(distance, 0, distance);
        _thickness = thickness = Mathf.Clamp(thickness, 0, thickness);
        _angle = angle;
        _spread = spread;
        _borderWidth = borderWidth = Mathf.Clamp(borderWidth, 0, borderWidth);
        _sectionBuffer = sectionBuffer = Mathf.Clamp(sectionBuffer, 0, sectionBuffer);
        _slices = slices = Mathf.Clamp(slices, 3, slices);
        _borderColor = borderColor;
        _fillColor = fillColor;
        _value = value = Mathf.Clamp(value, 0, (float)sections);
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

        float a1, a2, r1, r2;

        // fill
        for (int i = 0; i < sections; i++)
        {
            a1 = sectionAngle(i);
            a2 = a1 + direction * borderSpread();

            for (int j = 0; j < slices; j++)
            {
                a1 = a2;
                a2 += (direction * fillSliceSpread(i, j));

                // bottom fill
                r1 = distance + borderWidth;
                r2 = r1 + borderWidth;
                addQuad(r1, a1, r2, a2);
                addBottomFillToUVs(j);
                addColor(fillColor);

                // middle fill
                r1 = r2;
                r2 = distance + thickness - borderWidth - borderWidth;
                addQuad(r1, a1, r2, a2);
                addMiddleFillToUVs(j);
                addColor(fillColor);

                // top fill
                r1 = r2;
                r2 = distance + thickness - borderWidth;
                addQuad(r1, a1, r2, a2);
                addTopFillToUVs(j);
                addColor(fillColor);
            }
        }

        // border
        if (borderWidth > 0)
        {
            for (int i = 0; i < sections; i++)
            {
                a1 = sectionAngle(i);
                a2 = a1 + direction * borderSpread();

                // bottom left border
                r1 = distance;
                r2 = distance + borderWidth;
                addQuad(r1, a1, r2, a2);
                addBottomLeftBorderToUVs();
                addColor(borderColor);

                // left border
                r1 = distance + borderWidth;
                r2 = distance + thickness - borderWidth;
                addQuad(r1, a1, r2, a2);
                addLeftBorderToUVs();
                addColor(borderColor);

                // top left border
                r1 = distance + thickness - borderWidth;
                r2 = distance + thickness;
                addQuad(r1, a1, r2, a2);
                addTopLeftBorderToUVs();
                addColor(borderColor);

                // middle border sections
                for (int j = 0; j < slices; j++)
                {
                    a1 = a2;
                    a2 += (direction * sliceSpread());

                    // bottom middle border
                    r1 = distance;
                    r2 = distance + borderWidth;
                    addQuad(r1, a1, r2, a2);
                    addBottomBorderToUVs(j);
                    addColor(borderColor);

                    // top middle border
                    r1 = distance + thickness - borderWidth;
                    r2 = distance + thickness;
                    addQuad(r1, a1, r2, a2);
                    addTopBorderToUVs(j);
                    addColor(borderColor);
                }

                a2 = sectionAngle(i) + direction * sectionSpread();
                a1 = a2 - direction * borderSpread();

                // bottom right border
                r1 = distance;
                r2 = distance + borderWidth;
                addQuad(r1, a1, r2, a2);
                addBottomRightBorderToUVs();
                addColor(borderColor);

                // right border
                r1 = distance + borderWidth;
                r2 = distance + thickness - borderWidth;
                addQuad(r1, a1, r2, a2);
                addRightBorderToUVs();
                addColor(borderColor);

                // top right border
                r1 = distance + thickness - borderWidth;
                r2 = distance + thickness;
                addQuad(r1, a1, r2, a2);
                addTopRightBorderToUVs();
                addColor(borderColor);
            }
        }

        backMesh.Clear();
        backMesh.vertices = verts.ToArray();
        backMesh.uv = uvs.ToArray();
        backMesh.colors = colors.ToArray();
        backMesh.triangles = tris.ToArray();
        backMesh.RecalculateBounds();

        swapMeshBuffers();
    }

    // Add the verts of a quad based on given polar coordinages
    float aMin, aMax, rMin, rMax;
    void addQuad(float r1, float a1, float r2, float a2)
    {
        verts.Add(polarToCartesian(r1, a1, true));
        verts.Add(polarToCartesian(r2, a1, true));
        verts.Add(polarToCartesian(r2, a2, true));
        verts.Add(polarToCartesian(r1, a2, true));

        if (direction < 0)
        {
            // {  0,  1,  2,  0,  2,  3 }
            // { -4, -3, -2, -4, -2, -1 }
            tris.Add(verts.Count - 4);
            tris.Add(verts.Count - 3);
            tris.Add(verts.Count - 2);
            tris.Add(verts.Count - 4);
            tris.Add(verts.Count - 2);
            tris.Add(verts.Count - 1);
        }
        else
        {
            // {  0,  2,  1,  0,  3,  2 }
            // { -4, -2, -3, -4, -1, -2 }
            tris.Add(verts.Count - 4);
            tris.Add(verts.Count - 2);
            tris.Add(verts.Count - 3);
            tris.Add(verts.Count - 4);
            tris.Add(verts.Count - 1);
            tris.Add(verts.Count - 2);
        }
    }

    void addColor(Color c, int size = 4)
    {
        for (int i = 0; i < size; i++)
            colors.Add(c);
    }

    #region UVs

    // border uvs

    float middleBorderXMin(int sliceIndex)
    {
        return borderUV.xMin + ((sliceIndex * borderUV.width) / slices);
    }

    float middleBorderXMax(int sliceIndex)
    {
        return borderUV.xMin + (((sliceIndex + 1) * borderUV.width) / slices);
    }

    void addBottomLeftBorderToUVs()
    {
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, borderUV.yMin));
        uvs.Add(new Vector2(borderUV.xMin, borderUV.yMin));
        uvs.Add(new Vector2(borderUV.xMin, 0));
    }

    void addLeftBorderToUVs()
    {
        uvs.Add(new Vector2(0, borderUV.yMin));
        uvs.Add(new Vector2(0, borderUV.yMax));
        uvs.Add(new Vector2(borderUV.xMin, borderUV.yMax));
        uvs.Add(new Vector2(borderUV.xMin, borderUV.yMin));
    }

    void addTopLeftBorderToUVs()
    {
        uvs.Add(new Vector2(0, borderUV.yMax));
        uvs.Add(new Vector2(0, 1f));
        uvs.Add(new Vector2(borderUV.xMin, 1f));
        uvs.Add(new Vector2(borderUV.xMin, borderUV.yMax));
    }

    void addTopBorderToUVs(int sliceIndex)
    {
        uvs.Add(new Vector2(middleBorderXMin(sliceIndex), borderUV.yMax));
        uvs.Add(new Vector2(middleBorderXMin(sliceIndex), 1f));
        uvs.Add(new Vector2(middleBorderXMax(sliceIndex), 1f));
        uvs.Add(new Vector2(middleBorderXMax(sliceIndex), borderUV.yMax));

    }

    void addTopRightBorderToUVs()
    {
        uvs.Add(new Vector2(borderUV.xMax, borderUV.yMax));
        uvs.Add(new Vector2(borderUV.xMax, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(1f, borderUV.yMax));
    }

    void addRightBorderToUVs()
    {
        uvs.Add(new Vector2(borderUV.xMax, borderUV.yMin));
        uvs.Add(new Vector2(borderUV.xMax, borderUV.yMax));
        uvs.Add(new Vector2(1f, borderUV.yMax));
        uvs.Add(new Vector2(1f, borderUV.yMin));
    }

    void addBottomRightBorderToUVs()
    {
        uvs.Add(new Vector2(borderUV.xMax, 0));
        uvs.Add(new Vector2(borderUV.xMax, borderUV.yMin));
        uvs.Add(new Vector2(1f, borderUV.yMin));
        uvs.Add(new Vector2(1f, 0));
    }

    void addBottomBorderToUVs(int sliceIndex)
    {
        uvs.Add(new Vector2(middleBorderXMin(sliceIndex), 0));
        uvs.Add(new Vector2(middleBorderXMin(sliceIndex), borderUV.yMin));
        uvs.Add(new Vector2(middleBorderXMax(sliceIndex), borderUV.yMin));
        uvs.Add(new Vector2(middleBorderXMax(sliceIndex), 0));
    }

    // fill uvs

    float middleFillXMin(int sliceIndex)
    {
        if (sliceIndex == 0)
            return borderUV.xMin;
        else if (sliceIndex == slices - 1)
            return fillUV.xMax;
        else
            return fillUV.xMin + (((sliceIndex - 1) * fillUV.width) / (slices - 2));
    }

    float middleFillXMax(int sliceIndex)
    {
        if (sliceIndex == 0)
            return fillUV.xMin;
        else if (sliceIndex == slices - 1)
            return borderUV.xMax;
        else
            return fillUV.xMin + ((sliceIndex * fillUV.width) / (slices - 2));
    }

    void addBottomFillToUVs(int sliceIndex)
    {
        uvs.Add(new Vector2(middleFillXMin(sliceIndex), borderUV.yMin));
        uvs.Add(new Vector2(middleFillXMin(sliceIndex), fillUV.yMin));
        uvs.Add(new Vector2(middleFillXMax(sliceIndex), fillUV.yMin));
        uvs.Add(new Vector2(middleFillXMax(sliceIndex), borderUV.yMin));
    }

    void addMiddleFillToUVs(int sliceIndex)
    {
        uvs.Add(new Vector2(middleFillXMin(sliceIndex), fillUV.yMin));
        uvs.Add(new Vector2(middleFillXMin(sliceIndex), fillUV.yMax));
        uvs.Add(new Vector2(middleFillXMax(sliceIndex), fillUV.yMax));
        uvs.Add(new Vector2(middleFillXMax(sliceIndex), fillUV.yMin));
    }

    void addTopFillToUVs(int sliceIndex)
    {
        uvs.Add(new Vector2(middleFillXMin(sliceIndex), fillUV.yMax));
        uvs.Add(new Vector2(middleFillXMin(sliceIndex), borderUV.yMax));
        uvs.Add(new Vector2(middleFillXMax(sliceIndex), borderUV.yMax));
        uvs.Add(new Vector2(middleFillXMax(sliceIndex), fillUV.yMax));

    }

    #endregion



    #endregion



    #region MonoBehaviour

    void Awake()
    {
        //mesh = new Mesh();
        meshA = new Mesh();
        meshA.hideFlags = HideFlags.HideAndDontSave;
        meshB = new Mesh();
        meshB.hideFlags = HideFlags.HideAndDontSave;
        
        verts = new List<Vector3>();
        uvs = new List<Vector2>();
        colors = new List<Color>();
        tris = new List<int>();

        if (meshFilter == null) meshFilter = this.gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = this.gameObject.AddComponent<MeshFilter>();
        meshFilter.hideFlags = HideFlags.HideAndDontSave;
        //meshFilter.hideFlags = HideFlags.HideInInspector;

        if (meshRenderer == null) meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.hideFlags = HideFlags.HideAndDontSave;
        //meshRenderer.hideFlags = HideFlags.HideInInspector;

        meshRenderer.material = material;
        meshRenderer.material.hideFlags = HideFlags.HideAndDontSave;

        meshRenderer.sortingLayerName = _sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = _sortingOrderInLayer = sortingOrderInLayer;

        buildMesh();
    }

    //void OnEnable()
    //{
    //    if (meshFilter == null) meshFilter = this.gameObject.GetComponent<MeshFilter>();
    //    if (meshFilter == null) meshFilter = this.gameObject.AddComponent<MeshFilter>();
    //    meshFilter.hideFlags = HideFlags.HideInInspector;

    //    if (meshRenderer == null) meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
    //    if (meshRenderer == null) meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
    //    meshRenderer.hideFlags = HideFlags.HideInInspector;

    //    meshRenderer.material = material;
    //    buildMesh();
    //}

    void LateUpdate()
    {
        if (isDirty)
        {
            buildMesh();
        }

        if (sortingLayerName != _sortingLayerName ||
            sortingOrderInLayer != _sortingOrderInLayer)
        {
            meshRenderer.sortingLayerName = _sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = _sortingOrderInLayer = sortingOrderInLayer;
        }
    }

    void OnDestroy()
    {
        if (Application.isEditor)
        {
            if (meshFilter != null) meshFilter.sharedMesh = null;
            DestroyImmediate(meshA);
            DestroyImmediate(meshB);
        }
    }

    //void OnDestroy()
    //{
    //    if (meshRenderer != null && meshRenderer.sharedMaterial != null)
    //        DestroyImmediate(meshRenderer.sharedMaterial);

    //    if (Application.isEditor && Application.isPlaying == false)
    //    {
    //        if (meshFilter != null) DestroyImmediate(meshFilter);
    //        if (meshRenderer != null) DestroyImmediate(meshRenderer);
    //    }
    //}

    #endregion

}
