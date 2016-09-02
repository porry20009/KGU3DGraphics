using UnityEngine;

public class AddForceCameraScript : MonoBehaviour
{
    public delegate void InitGPUParameter(RenderTexture[] tex);
    enum WaterProcessmap
    {
        PreHeight = 0,
        CurrHeight = 1,
        NextHeight = 2,
        PreOutline = 3,
        CurrOutline = 4,
        Normal = 5
    };

    public RenderTexture[] mProcessmaps = { null, null, null, null, null, null };
    public Shader mAddForceShader = null;
    public Material mWavePropagationMaterial = null;
    public Material mHeightToNormalMaterial = null;
    public Material mOutlineToHeightMaterial = null;

    public InitGPUParameter InitializeCallBack = null;
    Camera mCamera = null;

    [HideInInspector]
    public float mNormalScale = 0.4f;
    [HideInInspector]
    public float mForce = 0.5f;
    [HideInInspector]
    public int mTexSize = 256;
    [HideInInspector]
    public float mDampingRatio = 0.95f;
    [HideInInspector]
    public Texture mWaterShapeTex = null;

    int mHForce = 0;
    int mHHeightPrevTex = 0;
    int mHHeightCurrentTex = 0;
    int mHWaterShapeTex = 0;
    int mHDamping = 0;
    int mHTextureSize = 0;
    int mHNormalScale = 0;
    int mHOutlineCurrTex = 0;
    int mHOutlinePreTex = 0;
    void Start()
    {
        mCamera = GetComponent<Camera>();
        CreateProcessmap();
        CreateMaterial();
        mCamera.SetReplacementShader(mAddForceShader, "");
        if (InitializeCallBack != null)
            InitializeCallBack(new RenderTexture[] { mProcessmaps[(int)WaterProcessmap.Normal] });
    }

    public void UpdateWaterParameter(params System.Object[] args)
    {
        mTexSize = (int)args[0];
        mDampingRatio = (float)args[1];
        mForce = (float)args[2];
        mNormalScale = (float)args[3];
        if (args.Length > 4)
            mWaterShapeTex = (Texture)args[4];
    }

    void OnPostRender()
    {
        AddForce();
        OutlinemapToHeightmap();
        ExecuteWaveEquation();
        HeightmapToNormalmap();
        SwapHeightmap();
        SwapOutlinemap();

    }

    void OnDestroy()
    {
        for (int i = 0; i < mProcessmaps.Length; i++)
        {
            if (mProcessmaps[i] != null)
            {
                Destroy(mProcessmaps[i]);
                mProcessmaps[i] = null;
            }
        }
    }

    void CreateMaterial()
    {
        mAddForceShader = Shader.Find("Water/SmallScale/AddFource");
        mWavePropagationMaterial = new Material(Shader.Find("Water/SmallScale/WaterSimulation"));
        mHeightToNormalMaterial = new Material(Shader.Find("Water/SmallScale/HeightToNormal"));
        mOutlineToHeightMaterial = new Material(Shader.Find("Water/SmallScale/OutlineToHeight"));   

        mHForce = Shader.PropertyToID("_Force");
        mHHeightPrevTex = Shader.PropertyToID("_HeightPrevTex");
        mHHeightCurrentTex = Shader.PropertyToID("_HeightCurrentTex");
        mHWaterShapeTex = Shader.PropertyToID("_WaterShapeTex");
        mHDamping = Shader.PropertyToID("_Damping");
        mHTextureSize = Shader.PropertyToID("_TextureSize");
        mHNormalScale = Shader.PropertyToID("_NormalScale");
        mHOutlineCurrTex = Shader.PropertyToID("_OutlineCurrTex");
        mHOutlinePreTex = Shader.PropertyToID("_OutlinePreTex");
    }

    void CreateProcessmap()
    {
        int width = mTexSize;
        int height = mTexSize;
        mProcessmaps[(int)WaterProcessmap.PreHeight] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)WaterProcessmap.PreHeight].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)WaterProcessmap.PreHeight].name = "Pre Frame Height";
        mProcessmaps[(int)WaterProcessmap.PreHeight].isPowerOfTwo = true;

        mProcessmaps[(int)WaterProcessmap.CurrHeight] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)WaterProcessmap.CurrHeight].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)WaterProcessmap.CurrHeight].name = "Curr Frame Height";
        mProcessmaps[(int)WaterProcessmap.CurrHeight].isPowerOfTwo = true;

        mProcessmaps[(int)WaterProcessmap.NextHeight] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)WaterProcessmap.NextHeight].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)WaterProcessmap.NextHeight].name = "Next Frame Height";
        mProcessmaps[(int)WaterProcessmap.NextHeight].isPowerOfTwo = true;

        mProcessmaps[(int)WaterProcessmap.PreOutline] = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)WaterProcessmap.PreOutline].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)WaterProcessmap.PreOutline].name = "Pre Outline";
        mProcessmaps[(int)WaterProcessmap.PreOutline].isPowerOfTwo = true;

        mProcessmaps[(int)WaterProcessmap.CurrOutline] = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)WaterProcessmap.CurrOutline].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)WaterProcessmap.CurrOutline].name = "Curr Outline";
        mProcessmaps[(int)WaterProcessmap.CurrOutline].isPowerOfTwo = true;

        mProcessmaps[(int)WaterProcessmap.Normal] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)WaterProcessmap.Normal].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)WaterProcessmap.Normal].name = "Normal";
        mProcessmaps[(int)WaterProcessmap.Normal].isPowerOfTwo = true;

        RenderTexture mainRTT = RenderTexture.active;
        for (int i = 0; i < mProcessmaps.Length; i++)
        {
            RenderTexture.active = mProcessmaps[i];
            GL.Clear(false, true, new Color(0, 0, 0, 0));
        }
        RenderTexture.active = mainRTT;

        mCamera.targetTexture = mProcessmaps[(int)WaterProcessmap.CurrOutline];
    }

    void AddForce()
    {
        Shader.SetGlobalFloat(mHForce, mForce);
    }

    void OutlinemapToHeightmap()
    {
        if (mOutlineToHeightMaterial != null)
        {
            mOutlineToHeightMaterial.SetTexture(mHOutlinePreTex, mProcessmaps[(int)WaterProcessmap.PreOutline]);
            mOutlineToHeightMaterial.SetTexture(mHOutlineCurrTex, mProcessmaps[(int)WaterProcessmap.CurrOutline]);
            mProcessmaps[(int)WaterProcessmap.CurrHeight].DiscardContents();
            Graphics.Blit(mProcessmaps[(int)WaterProcessmap.CurrOutline], mProcessmaps[(int)WaterProcessmap.CurrHeight], mOutlineToHeightMaterial);
        }
    }

    void ExecuteWaveEquation()
    {
        if (mWavePropagationMaterial != null)
        {
            mWavePropagationMaterial.SetTexture(mHHeightPrevTex, mProcessmaps[(int)WaterProcessmap.PreHeight]);
            mWavePropagationMaterial.SetTexture(mHHeightCurrentTex, mProcessmaps[(int)WaterProcessmap.CurrHeight]);
            mWavePropagationMaterial.SetTexture(mHWaterShapeTex, mWaterShapeTex);
            mWavePropagationMaterial.SetFloat(mHDamping, mDampingRatio);
            mWavePropagationMaterial.SetVector(mHTextureSize, new Vector4(1.0f / (float)mTexSize, 1.0f / (float)mTexSize, 0, 0));
            mProcessmaps[(int)WaterProcessmap.NextHeight].DiscardContents();
            Graphics.Blit(mProcessmaps[(int)WaterProcessmap.PreHeight], mProcessmaps[(int)WaterProcessmap.NextHeight], mWavePropagationMaterial);
        }
    }

    void HeightmapToNormalmap()
    {
        if (mHeightToNormalMaterial != null)
        {
            mHeightToNormalMaterial.SetTexture(mHHeightCurrentTex, mProcessmaps[(int)WaterProcessmap.CurrHeight]);
            mHeightToNormalMaterial.SetVector(mHTextureSize, new Vector4(1.0f / (float)mTexSize, 1.0f / (float)mTexSize, 0, 0));
            mHeightToNormalMaterial.SetFloat(mHNormalScale, mNormalScale);
            mProcessmaps[(int)WaterProcessmap.Normal].DiscardContents();
            Graphics.Blit(mProcessmaps[(int)WaterProcessmap.CurrHeight], mProcessmaps[(int)WaterProcessmap.Normal], mHeightToNormalMaterial);
        }
    }

    void SwapHeightmap()
    {
      //  mCamera.targetTexture = mProcessmaps[(int)WaterProcessmap.CurrHeight];
        RenderTexture temp = mProcessmaps[(int)WaterProcessmap.PreHeight];
        mProcessmaps[(int)WaterProcessmap.PreHeight] = mProcessmaps[(int)WaterProcessmap.CurrHeight];
        mProcessmaps[(int)WaterProcessmap.CurrHeight] = mProcessmaps[(int)WaterProcessmap.NextHeight];
        mProcessmaps[(int)WaterProcessmap.NextHeight] = temp;
    }

    void SwapOutlinemap()
    {
        mCamera.targetTexture = mProcessmaps[(int)WaterProcessmap.CurrOutline];
        RenderTexture temp = mProcessmaps[(int)WaterProcessmap.CurrOutline];
        mProcessmaps[(int)WaterProcessmap.CurrOutline] = mProcessmaps[(int)WaterProcessmap.PreOutline];
        mProcessmaps[(int)WaterProcessmap.PreOutline] = temp;
    }

    public Camera GetCamera()
    {
        return mCamera;
    }
}
