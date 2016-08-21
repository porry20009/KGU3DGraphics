using UnityEngine;
using System.Collections.Generic;

//挂到空的GameObject上
public class KGLine : MonoBehaviour
{
    public bool isUpdate = true;
    public List<Vector3> m_lineVerticesPosition = new List<Vector3>();
    public List<Color> m_lineVerticesColor = new List<Color>();

    Mesh m_mesh = null;
    Material m_material = null;
    void Start()
    {
        if (m_lineVerticesPosition.Count > 0)
        {
            m_material = new Material(Shader.Find("KGLine"));
            m_mesh = new Mesh();
            m_mesh.SetVertices(m_lineVerticesPosition);
            m_mesh.SetColors(m_lineVerticesColor);
            int[] indices = new int[m_lineVerticesPosition.Count];
            for (int i = 0; i < m_lineVerticesPosition.Count - 1; i++)
            {
                indices[i] = i;
                indices[i + 1] = i + 1;
            }
            m_mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = m_mesh;
            MeshRenderer rd = gameObject.AddComponent<MeshRenderer>();
            rd.material = m_material;
        }
    }
    void Update()
    {
        transform.position = Vector3.zero;
        if (isUpdate && m_mesh != null)
        {
            m_mesh.SetVertices(m_lineVerticesPosition);
            m_mesh.SetColors(m_lineVerticesColor);
            m_mesh.RecalculateBounds();
        }
    }
}
