using UnityEngine;

public class MergeAABB : MonoBehaviour
{
    public GameObject m_mainLight = null;
    SimplifiedCSM m_simpleCSM = null;

    void OnWillRenderObject()
    {
        if (m_simpleCSM == null)
            m_simpleCSM = m_mainLight.GetComponent<SimplifiedCSM>();
        if (m_simpleCSM != null && Camera.current.name.Equals("ClipCamera"))
        {
			Bounds bound = GetComponent<Renderer>().bounds;
            m_simpleCSM.MergeObjectAABB(bound.max, bound.min);
        }
    }
}

