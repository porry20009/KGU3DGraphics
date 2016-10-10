using UnityEngine;
using System.Collections;

public class TestOctree : MonoBehaviour 
{
    public Vector3 mp = new Vector3(10, 2, 0);
    public Vector3 mp1 = Vector3.zero;
    public Vector3 mp2 = new Vector3(5, 2, 0);
    public Vector3 mp3 = new Vector3(-2, 10, 0);

    Radiancy.Octree mOctree = new Radiancy.Octree();
    KGLine mKGLine = null;
    KGFrameBox mKGFrameBox = null;
	void Start () 
    {
        mOctree.Create(GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[],Vector3.zero, 10, 6);
        mOctree.ExciseScene();
        //GameObject line0 = new GameObject("line0");
        //line0.transform.localPosition = Vector3.zero;
        //line0.transform.localRotation = Quaternion.identity;
        //line0.transform.localScale = Vector3.one;
        //line0.transform.parent = transform;
        //mKGLine = line0.AddComponent<KGLine>();

        //GameObject box = new GameObject("box");
        //box.transform.localPosition = Vector3.zero;
        //box.transform.localRotation = Quaternion.identity;
        //box.transform.localScale = Vector3.one;
        //box.transform.parent = null;

        //mKGFrameBox = box.AddComponent<KGFrameBox>();


        //mKGFrameBox.BeginDraw();
        //mKGFrameBox.Draw(Vector3.zero, Vector3.one, Color.red);

        //mKGFrameBox.Draw(new Vector3(10, 1, 2), Vector3.one * 2, Color.green);

        //mKGFrameBox.EndDraw();
	}

    //void Update()
    //{
    //    float dist = 0.0f;
    //    Vector3 point = Vector3.zero;
    //    Radiancy.Octree.CalculatePointToTriangleDist(mp, mp1, mp2, mp3, out dist, out point);

    //    mKGLine.BeginDraw();
    //    mKGLine.Draw(mp1, mp2, Color.green);
    //    mKGLine.Draw(mp2, mp3, Color.green);
    //    mKGLine.Draw(mp3, mp1, Color.green);
    //    mKGLine.Draw(mp, point, Color.red);
    //    mKGLine.Draw(point, point + new Vector3(0, 1, 0), Color.blue);

    //    mKGLine.EndDraw();
    //}
}
