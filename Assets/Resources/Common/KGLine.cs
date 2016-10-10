using UnityEngine;
using System.Collections.Generic;

//挂到空的GameObject上
public class KGLine : MonoBehaviour
{
    List<Vector3> mPositions = new List<Vector3>();
    List<Color> mColors = new List<Color>();
    List<int> mIndices = new List<int>();

    Mesh mMesh = null;
    Material mMaterial = null;

    int mCurrPointNumber = 0;
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
        if (mCurrPointNumber < 65536)
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

    public bool Draw(Vector3 from,Vector3 to,Color color)
    {
        if (mCurrPointNumber < 65536)
        {
            if (mPositions.Count > mCurrPointNumber)
            {
                mPositions[mCurrPointNumber] = from;
                mIndices[mCurrPointNumber] = mCurrPointNumber;
                mColors[mCurrPointNumber] = color;
            }
            else
            {
                mPositions.Add(from);
                mIndices.Add(mCurrPointNumber);
                mColors.Add(color);
            }
            mCurrPointNumber++;

            if (mPositions.Count > mCurrPointNumber)
            {
                mPositions[mCurrPointNumber] = to;
                mIndices[mCurrPointNumber] = mCurrPointNumber;
                mColors[mCurrPointNumber] = color;
            }
            else
            {
                mPositions.Add(to);
                mIndices.Add(mCurrPointNumber);
                mColors.Add(color);
            }
            mCurrPointNumber++;
            return true;
        }
        else
        {
            Debug.LogWarning("顶点数量超过了65536个!");
            return false;
        }
    }

    public void BeginDraw()
    {
        mCurrPointNumber = 0;
    }
    public void EndDraw()
    {

    }
}
