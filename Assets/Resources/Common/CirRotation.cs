using UnityEngine;

public class CirRotation : MonoBehaviour
{
    public Vector3 m_speed = Vector3.zero;
    void Update()
    {
        transform.Rotate(m_speed * Time.deltaTime);
    }
}

