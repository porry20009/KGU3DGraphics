using UnityEngine;
public class SimplifiedHDR : MonoBehaviour
{
    enum HDRPass
    {
        Luminance = 0,
        Average4x4Samples = 1,
        Average2x2Samples = 2,
        AdaptiveLuminance = 3,
        AutoExposureWithoutBloom = 4,
        AutoExposureWithBloom = 5,
        Brightness = 6
    }

    enum GaussianBlurPass
    {
        Horizontal3x3 = 0,
        Vertical3x3 = 1,
        Horizontal5x5 = 2,
        Vertical5x5 = 3
    }

    enum HDRProcessmap
    {
        PreLuminance = 0,
        CurrLuminance = 1,
        NewLuminance = 2,
        DownScale128x128 = 3,
        DownScale64x64 = 4,
        DownScale16x16 = 5,
        DownScale4x4 = 6
    }

    public float mAdaptiveSpeed = 1.0f;
    [Range(0, 1)]
    public float mMiddleGray = 0.5f;//指定图像base亮度
    public float mExposure = 2.0f;
    public float mCoeff0 = 2.0f;

    public bool mIsBloomDebug = false;
    public bool mIsBloom = true;
    public float mBrightOffset = -0.7f;
    public float mBrightScale = 0.6f;

    Material mHDRMaterial = null;
    Material mGaussianMaterial = null;
    RenderTexture[] mHDRProcessmap = { null, null, null, null, null, null, null };

    void Awake()
    {
        CreateMaterial();
        CreateProcessMap();
    }

    void OnDestroy()
    {
        for (int i = 0; i < mHDRProcessmap.Length; i++)
        {
            if (mHDRProcessmap[i] != null)
            {
                Destroy(mHDRProcessmap[i]);
                mHDRProcessmap[i] = null;
            }
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RGBToLuminance(src);
        AverageLuminance();
        AdaptiveLuminance();
        if (mIsBloom)
        {
            AutoExposureWithBloom(src, dest);
        }
        else
        {
            AutoExposureWithoutBloom(src, dest);
        }
    }

    void CreateMaterial()
    {
        mHDRMaterial = new Material(Shader.Find("PostEffect/SimplifiedHDR"));
        if (mHDRMaterial == null)
        {
            Debug.LogWarning("SimplifiedHDR.shader丢失!");
            enabled = false;
        }

        mGaussianMaterial = new Material(Shader.Find("PostEffect/GaussianBlur"));
        if (mGaussianMaterial == null)
        {
            Debug.LogWarning("GaussianBlur.shader丢失!");
            enabled = false;
        }
    }

    void CreateProcessMap()
    {
        RenderTextureFormat format = RenderTextureFormat.ARGB32;
        mHDRProcessmap[(int)HDRProcessmap.DownScale128x128] = new RenderTexture(128, 128, 0, format);
        mHDRProcessmap[(int)HDRProcessmap.DownScale64x64] = new RenderTexture(64, 64, 0, format);
        mHDRProcessmap[(int)HDRProcessmap.DownScale16x16] = new RenderTexture(16, 16, 0, format);
        mHDRProcessmap[(int)HDRProcessmap.DownScale4x4] = new RenderTexture(4, 4, 0, format);
        mHDRProcessmap[(int)HDRProcessmap.CurrLuminance] = new RenderTexture(1, 1, 0, format);
        mHDRProcessmap[(int)HDRProcessmap.PreLuminance] = new RenderTexture(1, 1, 0, format);
        mHDRProcessmap[(int)HDRProcessmap.NewLuminance] = new RenderTexture(1, 1, 0, format);

        for (int i = 0; i < mHDRProcessmap.Length; i++)
        {
            mHDRProcessmap[i].generateMips = false;
            mHDRProcessmap[i].filterMode = FilterMode.Point;
            mHDRProcessmap[i].wrapMode = TextureWrapMode.Clamp;

            RenderTexture mainRTT = RenderTexture.active;
            RenderTexture.active = mHDRProcessmap[i];
            GL.Clear(false, true, Color.black);
            RenderTexture.active = mainRTT;
        }
    }


    void RGBToLuminance(RenderTexture src)
    {
        mHDRMaterial.SetTexture("_MainTex", src);
        mHDRProcessmap[(int)HDRProcessmap.DownScale128x128].DiscardContents();
        Graphics.Blit(src, mHDRProcessmap[(int)HDRProcessmap.DownScale128x128], mHDRMaterial, (int)HDRPass.Luminance);
    }

    void AverageLuminance()
    {
        LumDownScale2x2(mHDRProcessmap[(int)HDRProcessmap.DownScale128x128], mHDRProcessmap[(int)HDRProcessmap.DownScale64x64], new Vector2(1.0f / 128.0f, 1.0f / 128.0f));
        LumDownScale4x4(mHDRProcessmap[(int)HDRProcessmap.DownScale64x64], mHDRProcessmap[(int)HDRProcessmap.DownScale16x16], new Vector2(1.0f / 64.0f, 1.0f / 64.0f));
        LumDownScale4x4(mHDRProcessmap[(int)HDRProcessmap.DownScale16x16], mHDRProcessmap[(int)HDRProcessmap.DownScale4x4], new Vector2(1.0f / 16.0f, 1.0f / 16.0f));
        LumDownScale4x4(mHDRProcessmap[(int)HDRProcessmap.DownScale4x4], mHDRProcessmap[(int)HDRProcessmap.CurrLuminance], new Vector2(1.0f / 4.0f, 1.0f / 4.0f));
    }

    bool mFirst = true;
    void AdaptiveLuminance()
    {
        if (mFirst)
        {
            Graphics.Blit(mHDRProcessmap[(int)HDRProcessmap.CurrLuminance], mHDRProcessmap[(int)HDRProcessmap.NewLuminance]);
            mFirst = false;
        }
        else
        {
            mHDRMaterial.SetTexture("_CurrAverLumTex", mHDRProcessmap[(int)HDRProcessmap.CurrLuminance]);
            mHDRMaterial.SetTexture("_PreAverLumTex", mHDRProcessmap[(int)HDRProcessmap.PreLuminance]);
            mHDRMaterial.SetFloat("_AdaptiveSpeed", mAdaptiveSpeed);
            mHDRMaterial.SetFloat("_ElapsedTime", Time.deltaTime);
            mHDRProcessmap[(int)HDRProcessmap.NewLuminance].DiscardContents();
            Graphics.Blit(null, mHDRProcessmap[(int)HDRProcessmap.NewLuminance], mHDRMaterial, (int)HDRPass.AdaptiveLuminance);
        }
    }

    void AutoExposureWithoutBloom(RenderTexture src, RenderTexture dest)
    {
        mHDRMaterial.SetTexture("_MainTex", src);
        mHDRMaterial.SetTexture("_AdaptedLumTex", mHDRProcessmap[(int)HDRProcessmap.NewLuminance]);
        mHDRMaterial.SetFloat("_MiddleGray", mMiddleGray);
        mHDRMaterial.SetFloat("_Exposure", mExposure);
        mHDRMaterial.SetFloat("_Coeff0", mCoeff0);
        Graphics.Blit(src, dest, mHDRMaterial, (int)HDRPass.AutoExposureWithoutBloom);
        SwapRenderTexture((int)HDRProcessmap.NewLuminance, (int)HDRProcessmap.PreLuminance);
    }

    void SwapRenderTexture(int a, int b)
    {
        RenderTexture t = mHDRProcessmap[a];
        mHDRProcessmap[a] = mHDRProcessmap[b];
        mHDRProcessmap[b] = t;
    }

    //缩小4倍（采样16个点）
    void LumDownScale4x4(RenderTexture src, RenderTexture dest, Vector2 texsizeInverse)
    {
        mHDRMaterial.SetTexture("_MainTex", src);
        mHDRMaterial.SetVector("_AverImageSizeInverse", texsizeInverse);

        dest.DiscardContents();
        Graphics.Blit(src, dest, mHDRMaterial, (int)HDRPass.Average4x4Samples);
    }

    //缩小2倍（采样4个点）
    void LumDownScale2x2(RenderTexture src, RenderTexture dest, Vector2 texsizeInverse)
    {
        mHDRMaterial.SetTexture("_MainTex", src);
        mHDRMaterial.SetVector("_AverImageSizeInverse", texsizeInverse);

        dest.DiscardContents();
        Graphics.Blit(src, dest, mHDRMaterial, (int)HDRPass.Average2x2Samples);
    }

    void AutoExposureWithBloom(RenderTexture src, RenderTexture dest)
    {
        int scaleW = Screen.width / 4;
        int scaleH = Screen.height / 4;
        RenderTexture brightnessScaleTex = RenderTexture.GetTemporary(scaleW, scaleH, 0, RenderTextureFormat.ARGB32);
        RenderTexture temp = RenderTexture.GetTemporary(scaleW, scaleH, 0, RenderTextureFormat.ARGB32);

        //Brightness
        mHDRMaterial.SetTexture("_CurrAverLumTex", mHDRProcessmap[(int)HDRProcessmap.NewLuminance]);
        mHDRMaterial.SetFloat("_MiddleGray", mMiddleGray);
        mHDRMaterial.SetFloat("_BrightOffset", mBrightOffset);
        mHDRMaterial.SetFloat("_BrightScale", mBrightScale);
        mHDRMaterial.SetFloat("_Exposure", mExposure);
        mHDRMaterial.SetFloat("_Coeff0", mCoeff0);
        Graphics.Blit(src, brightnessScaleTex, mHDRMaterial, (int)HDRPass.Brightness);

        //Gaussian Blur
        mGaussianMaterial.SetVector("_BlurTexSizeInverse", new Vector4(1.0f / (float)scaleW, 1.0f / (float)scaleH, 0, 0));
        temp.DiscardContents();
        Graphics.Blit(brightnessScaleTex, temp, mGaussianMaterial, (int)GaussianBlurPass.Horizontal5x5);

        if (mIsBloomDebug)
        {
            brightnessScaleTex.DiscardContents();
            Graphics.Blit(temp, brightnessScaleTex, mGaussianMaterial, (int)GaussianBlurPass.Vertical5x5);
            Graphics.Blit(brightnessScaleTex, dest);
        }
        else
        {
            brightnessScaleTex.DiscardContents();
            Graphics.Blit(temp, brightnessScaleTex, mGaussianMaterial, (int)GaussianBlurPass.Vertical5x5);
            //Blend Screen And Brightness
            mHDRMaterial.SetTexture("_MainTex", src);
            mHDRMaterial.SetTexture("_BloomTex", brightnessScaleTex);
            mHDRMaterial.SetTexture("_AdaptedLumTex", mHDRProcessmap[(int)HDRProcessmap.NewLuminance]);
            mHDRMaterial.SetFloat("_MiddleGray", mMiddleGray);
            mHDRMaterial.SetFloat("_Exposure", mExposure);
            mHDRMaterial.SetFloat("_Coeff0", mCoeff0);
            Graphics.Blit(src, dest, mHDRMaterial, (int)HDRPass.AutoExposureWithBloom);
            SwapRenderTexture((int)HDRProcessmap.NewLuminance, (int)HDRProcessmap.PreLuminance);
        }
        RenderTexture.ReleaseTemporary(brightnessScaleTex);
        RenderTexture.ReleaseTemporary(temp);
    }
}

