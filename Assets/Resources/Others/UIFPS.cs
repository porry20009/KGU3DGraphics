using UnityEngine;
using System.Collections;

public class UIFPS : MonoBehaviour 
{
    public float mIntervalTime = 1.0f;
    int mCount = 0;
    float mSumTime = 0.0f;
    UILabel mFPSLabel = null;
	void Start () 
    {
        mFPSLabel = gameObject.GetComponent<UILabel>();
        if (mFPSLabel == null)
            Debug.LogWarning(gameObject.name + "忘记挂上UILabel?");
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (mFPSLabel == null)
            return;
        mCount++;
        mSumTime += Time.deltaTime;
        float t = mSumTime / mIntervalTime;
        if (t >= 1.0f)
        {
            int fps = (int)((float)mCount/mIntervalTime);
            mFPSLabel.text = string.Concat(new string[] { "FPS:", fps.ToString() });
            mSumTime = 0.0f;
            mCount = 0;
        }
	}
}
