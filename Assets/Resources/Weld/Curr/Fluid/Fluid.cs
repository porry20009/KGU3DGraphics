using UnityEngine;
using UnityEngine.Rendering;
/*
 * 用法：挂到物体(Renderer)上
 * 说明：计算表面扰动法线/高度图
*/
[RequireComponent(typeof(Renderer))]
public class Fluid : MonoBehaviour
{
    public int mTexWidth = 512;
    public int mTexHeight = 512;
    public Vector2 mHeatSourceHitUV = Vector2.zero;
    [Range(0.8f, 0.99f)]
    public float mWaveDamping = 0.98f;
    [Range(0,1.0f)]
    public float mHitRadius = 0.05f;
    [Range(0,0.1f)]
    public float mSmoothness = 0.01f;
    public float mHitForce = 1.0f;
    public float mCoeffViscosity = 7.0f;
    public float mWaveSpeed = 0.04f;
    
    public enum Processmap
    {
        PreHeight = 0,
        CurrHeight = 1,
        NextHeight = 2,
        Normal = 3,
    };
    enum MaterialType
    {
        AddFource = 0,
        WavePropagation = 1,
        HeightToNormal = 2,
    };

    public RenderTexture[] mProcessmaps = { null, null, null, null};
    Material[] mMaterial = { null, null, null};
    CommandBuffer mCommandBuffer = null;
    Renderer mRenderer = null;
    void Start()
    {
        Debug.Log("Fluid:" + this.GetInstanceID().ToString());
        mRenderer = gameObject.GetComponent<Renderer>();
        CreateMaterials();
        CheckWaveEquation(mCoeffViscosity, mWaveSpeed, Mathf.Min(1.0f / (float)mTexWidth, 1.0f / (float)mTexHeight), 0.03f);
        CreateCommandBuffer();
        CreateProcessmaps();
    }

    void OnDestroy()
    {
        for (int i = 0; i < mProcessmaps.Length; i++)
        {
            EffectHelp.S.SaveRelease<RenderTexture>(ref mProcessmaps[i]);
        }
        if (mCommandBuffer != null)
            mCommandBuffer.Clear();
    }

    void OnWillRenderObject()
    {
        if (Camera.current.name.Equals(Camera.main.name))
        {
            RenderTexture mainRTT = RenderTexture.active;
           
            Graphics.SetRenderTarget(mProcessmaps[(int)Processmap.CurrHeight]);
            mMaterial[(int)MaterialType.AddFource].SetVector("_CenterUV", mHeatSourceHitUV);
            mMaterial[(int)MaterialType.AddFource].SetFloat("_Radius", mHitRadius);
            mMaterial[(int)MaterialType.AddFource].SetFloat("_Smoothness", mSmoothness);
            mMaterial[(int)MaterialType.AddFource].SetFloat("_Force", mHitForce);
            mMaterial[(int)MaterialType.AddFource].SetFloat("_AspectRatio", (float)mTexHeight / (float)mTexWidth);
            Graphics.ExecuteCommandBuffer(mCommandBuffer);
            Graphics.SetRenderTarget(mainRTT);

            ExecuteWaveEquation();
            RenderHeightmapToNormalmap();
        }
    }

    void CreateMaterials()
    {
        mMaterial[(int)MaterialType.AddFource] = new Material(Shader.Find("Weld/Fluid/AddForce"));
        mMaterial[(int)MaterialType.WavePropagation] = new Material(Shader.Find("Weld/Fluid/WavePropagation"));
        mMaterial[(int)MaterialType.HeightToNormal] = new Material(Shader.Find("Weld/Fluid/HeightToNormal"));
    }

    void CreateCommandBuffer()
    {
        if (mCommandBuffer == null)
            mCommandBuffer = new CommandBuffer();
        mCommandBuffer.ClearRenderTarget(true, false, Color.clear);
        mCommandBuffer.DrawRenderer(mRenderer, mMaterial[(int)MaterialType.AddFource]);
    }

    void CreateProcessmaps()
    {
        mProcessmaps[(int)Processmap.PreHeight] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGBFloat);
        mProcessmaps[(int)Processmap.PreHeight].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.PreHeight].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.PreHeight].name = string.Format("{0} Pre Height", gameObject.name);
       
        mProcessmaps[(int)Processmap.CurrHeight] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGBFloat);
        mProcessmaps[(int)Processmap.CurrHeight].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.CurrHeight].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.CurrHeight].name = string.Format("{0} Curr Height", gameObject.name);

        mProcessmaps[(int)Processmap.NextHeight] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGBFloat);
        mProcessmaps[(int)Processmap.NextHeight].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.NextHeight].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.NextHeight].name = string.Format("{0} Next Height", gameObject.name);

        mProcessmaps[(int)Processmap.Normal] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGBFloat);
        mProcessmaps[(int)Processmap.Normal].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.Normal].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.Normal].name = string.Format("{0} Normal", gameObject.name);

        for (int i = 0; i < mProcessmaps.Length; i++)
        {
            RenderTexture mainRTT = RenderTexture.active;
            RenderTexture.active = mProcessmaps[i];
            GL.Clear(false, true, new Color(0, 0, 0, 1));
            RenderTexture.active = mainRTT;
        }
    }
    void CheckWaveEquation(float u, float c, float d, float t)
    {
        float maxC = 0.5f * d / t * Mathf.Sqrt(u * t + 2);
        if (c >= maxC || c <= 0)
        {
            Debug.LogError("波动方程求解崩溃！注意波速WaveSpeed(Max):" + maxC.ToString());
            return;
        }
        float maxDeltaTime = (u + Mathf.Sqrt(u * u + 32.0f * c * c / (d * d))) / (8.0f * c * c / (d * d));
        if (t >= maxDeltaTime || t <= 0)
        {
            Debug.LogError("波动方程求解崩溃！注意时间步长DeltaTime(Max):" + maxDeltaTime.ToString());
            return;
        }
        float k = c * c * t * t / (d * d);
        float k1 = (4.0f - 8.0f * k) / (u * t + 2);
        float k2 = (u * t - 2) / (u * t + 2);
        float k3 = (2.0f * k) / (u * t + 2);
        mMaterial[(int)MaterialType.WavePropagation].SetVector("_K", new Vector3(k1, k2, k3));
    }

    void ExecuteWaveEquation()
    {
#if UNITY_EDITOR
        CheckWaveEquation(mCoeffViscosity, mWaveSpeed, Mathf.Min(1.0f / (float)mTexWidth, 1.0f / (float)mTexHeight), 0.03f);
#endif
        mMaterial[(int)MaterialType.WavePropagation].SetTexture("_HeightPrevTex", mProcessmaps[(int)Processmap.PreHeight]);
        mMaterial[(int)MaterialType.WavePropagation].SetTexture("_HeightCurrentTex", mProcessmaps[(int)Processmap.CurrHeight]);
       // mMaterial[(int)MaterialType.WavePropagation].SetTexture("_ShapeTex", mProcessmaps[(int)Processmap.CurrHeatFlux]);
        mMaterial[(int)MaterialType.WavePropagation].SetFloat("_Damping", mWaveDamping);
        mMaterial[(int)MaterialType.WavePropagation].SetVector("_TextureSize", new Vector4(1.0f / (float)mTexWidth, 1.0f / (float)mTexHeight, 0, 0));
        mProcessmaps[(int)Processmap.NextHeight].DiscardContents();
        Graphics.Blit(mProcessmaps[(int)Processmap.PreHeight], mProcessmaps[(int)Processmap.NextHeight], mMaterial[(int)MaterialType.WavePropagation]);
    }

    void RenderHeightmapToNormalmap()
    {
        mMaterial[(int)MaterialType.HeightToNormal].SetTexture("_HeightCurrentTex",mProcessmaps[(int)Processmap.CurrHeight]);
        mMaterial[(int)MaterialType.HeightToNormal].SetVector("_TextureSize",new Vector4(1.0f / (float)mTexWidth, 1.0f / (float)mTexHeight, 0, 0));
        mMaterial[(int)MaterialType.HeightToNormal].SetFloat("_NormalScale",0.3f);
        mProcessmaps[(int)Processmap.Normal].DiscardContents();
        Graphics.Blit(mProcessmaps[(int)Processmap.CurrHeight], mProcessmaps[(int)Processmap.Normal], mMaterial[(int)MaterialType.HeightToNormal]);

        RenderTexture temp = mProcessmaps[(int)Processmap.PreHeight];
        mProcessmaps[(int)Processmap.PreHeight] = mProcessmaps[(int)Processmap.CurrHeight];
        mProcessmaps[(int)Processmap.CurrHeight] = mProcessmaps[(int)Processmap.NextHeight];
        mProcessmaps[(int)Processmap.NextHeight] = temp;
    }
}
