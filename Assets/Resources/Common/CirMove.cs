using UnityEngine;

public class CirMove : MonoBehaviour
{
    public Vector3 m_XYZ = Vector3.zero;
    public Vector3 m_speed = Vector3.one;

    Vector3 m_oldPosition = Vector3.zero;
    void Start()
    {
        m_oldPosition = transform.position;
    }

    void Update()
    {
        transform.position = m_oldPosition + new Vector3(m_XYZ.x * Mathf.Sin(Time.time * m_speed.x), m_XYZ.y * Mathf.Cos(Time.time * m_speed.y), m_XYZ.z * Mathf.Sin(Time.time * m_speed.z));
    }
}
