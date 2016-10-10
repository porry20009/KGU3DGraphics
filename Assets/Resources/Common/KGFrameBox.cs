using UnityEngine;
using System.Collections.Generic;

//挂到空的GameObject上
public class KGFrameBox : MonoBehaviour
{
    public List<Vector3> mPositions = new List<Vector3>();
    public List<Color> mColors = new List<Color>();
    public List<int> mIndices = new List<int>();

    Mesh mMesh = null;
    Material mMaterial = null;

    Vector3 []mVertices = {Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero,
                           Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero};
    int mVertexNumber = 0;
    int mBoxNumber = 0;

    void Start()
    {
        mMaterial = new Material(Shader.Find("KGLine"));
        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        if (filter == null)
            filter = gameObject.AddComponent<MeshFilter>();
        else
        {
            Debug.LogError("你确定是挂在Empty GameObject下？");
            return;
        }
        filter.mesh = new Mesh();
        MeshRenderer rd = gameObject.GetComponent<MeshRenderer>();
        if (rd == null)
            rd = gameObject.AddComponent<MeshRenderer>();
        rd.material = mMaterial;
        mMesh = filter.mesh;
    }

    void Update()
    {
        if (mPositions.Count < 65536)
        {
            if (mMesh != null)
            {
                mMesh.SetVertices(mPositions);
                mMesh.SetIndices(mIndices.ToArray(), MeshTopology.Lines, 0);
                mMesh.SetColors(mColors);
                mMesh.RecalculateBounds();
            }
        }
    }

    /* 
     * Vector3 []vertices 
     * [0][1][2][3] 上表面
     * [4][5][6][7] 下底面
     */

    public bool Draw(Vector3 center, Vector3 size, Color color)
    {
        if (mPositions.Count < 65500)
        {
            Vector3 sizeDivide2 = size * 0.5f;
            mVertices[0] = center + new Vector3(sizeDivide2.x, sizeDivide2.y, -sizeDivide2.z);
            mVertices[1] = center + new Vector3(sizeDivide2.x, sizeDivide2.y, sizeDivide2.z);
            mVertices[2] = center + new Vector3(-sizeDivide2.x, sizeDivide2.y, sizeDivide2.z);
            mVertices[3] = center + new Vector3(-sizeDivide2.x, sizeDivide2.y, -sizeDivide2.z);

            mVertices[4] = center + new Vector3(sizeDivide2.x, -sizeDivide2.y, -sizeDivide2.z);
            mVertices[5] = center + new Vector3(sizeDivide2.x, -sizeDivide2.y, sizeDivide2.z);
            mVertices[6] = center + new Vector3(-sizeDivide2.x, -sizeDivide2.y, sizeDivide2.z);
            mVertices[7] = center + new Vector3(-sizeDivide2.x, -sizeDivide2.y, -sizeDivide2.z);
            for (int i = 0; i < 8; i++)
            {
                AddVertex(mVertices[i], color);
            }
            AddIndices();
            mBoxNumber++;
            return true;
        }
        else
        {
           // Debug.LogWarning("顶点数量超过了65500个!");
            return false;
        }
    }

    public bool Draw(Vector3 []vertices, Color color)
    {
        if (mPositions.Count < 65500)
        {
            if (vertices.Length != 8)
                return false;
            for (int i = 0; i < 8; i++)
            {
                AddVertex(vertices[i], color);
            }
            AddIndices();
            mBoxNumber++;
            return true;
        }
        else
        {
           // Debug.LogWarning("顶点数量超过了65500个!");
            return false;
        }
    }

    public void BeginDraw()
    {
        mBoxNumber = 0;
        mVertexNumber = 0;
    }
    public void EndDraw()
    {

    }

    void AddVertex(Vector3 vertex,Color color)
    {
        if (mPositions.Count > mVertexNumber)
        {
            mPositions[mVertexNumber] = vertex;
            mColors[mVertexNumber] = color;
        }
        else
        {
            mPositions.Add(vertex);
            mColors.Add(color);
        }
        mVertexNumber++;
    }

    void AddIndice(int offset,int value)
    {
        int n = mBoxNumber * 24;
        int k = mBoxNumber * 8;
        if (mIndices.Count > n + offset)
        {
            mIndices[n + offset] = value + k;
        }
        else
        {
            mIndices.Add(value + k);
        }

    }

    void AddIndices()
    {
        //上表面
        AddIndice(0, 0);
        AddIndice(1, 1);
        AddIndice(2, 1);
        AddIndice(3, 2);
        AddIndice(4, 2);
        AddIndice(5, 3);
        AddIndice(6, 3);
        AddIndice(7, 0);

        //下底面
        AddIndice(8, 4);
        AddIndice(9, 5);
        AddIndice(10, 5);
        AddIndice(11, 6);
        AddIndice(12, 6);
        AddIndice(13, 7);
        AddIndice(14, 7);
        AddIndice(15, 4);

        //侧面
        AddIndice(16, 0);
        AddIndice(17, 4);
        AddIndice(18, 1);
        AddIndice(19, 5);
        AddIndice(20, 2);
        AddIndice(21, 6);
        AddIndice(22, 3);
        AddIndice(23, 7);
    }
}
