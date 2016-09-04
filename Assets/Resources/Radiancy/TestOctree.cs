using UnityEngine;
using System.Collections;

public class TestOctree : MonoBehaviour 
{

    Radiancy.Octree mOctree = new Radiancy.Octree();
	void Start () 
    {
        mOctree.Create(GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[],Vector3.zero, 10, 3, true);
	}
	
	void Update () 
    {
	
	}
}
