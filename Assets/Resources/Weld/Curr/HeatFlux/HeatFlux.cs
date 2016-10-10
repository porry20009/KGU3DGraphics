using UnityEngine;
using UnityEngine.Rendering;
/*
 * 用法：挂到物体(Renderer)上
 * 说明：计算热流密度场
*/
[RequireComponent(typeof(Renderer))]
public class HeatFlux : MonoBehaviour
{
    public int mTexWidth = 512;
    public int mTexHeight = 512;
    public Transform mHeatSource = null;
    public Vector3 mHeatSourcePosition = Vector3.zero;
    //高斯分布系数
    public float mGaussianDistributionSigma = 1.0f;
    //电弧有效热功率
    public float mEffectivePower = 3.0f;
    //热源半径
    public float mHeatRadius = 2.0f;
    //热量衰减系数
    [Range(0.9f, 0.9999f)]
    public float mHeatDamping = 0.995f;
    public Texture mDistributionTex = null;

    public enum Processmap
    {
        PreHeatFlux = 0,
        CurrHeatFlux = 1,
        HeatFluxDistribution = 2

    };
    enum MaterialType
    {
        AddHeat = 0,
        LoseHeat = 1,
        ShowHeatDistribution = 2,
    };

    public RenderTexture[] mProcessmaps = { null, null, null };
    Material[] mMaterial = { null, null, null };
    CommandBuffer mCommandBuffer = null;
    Renderer mRenderer = null;
    void Start()
    {
        Debug.Log("HeatFlux:" + this.GetInstanceID().ToString());
        mRenderer = gameObject.GetComponent<Renderer>();
        CreateMaterials();
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
            Graphics.SetRenderTarget(mProcessmaps[(int)Processmap.CurrHeatFlux]);
            if (mHeatSource != null)
                mMaterial[(int)MaterialType.AddHeat].SetVector("_HeatSourcePos", mHeatSource.position);
            else
                mMaterial[(int)MaterialType.AddHeat].SetVector("_HeatSourcePos", mHeatSourcePosition);
            mMaterial[(int)MaterialType.AddHeat].SetFloat("_GaussianDistributionSigma", mGaussianDistributionSigma);
            mMaterial[(int)MaterialType.AddHeat].SetFloat("_EffectivePower", mEffectivePower * Time.deltaTime);
            mMaterial[(int)MaterialType.AddHeat].SetFloat("_HeatRange", mHeatRadius);

            Graphics.ExecuteCommandBuffer(mCommandBuffer);
            Graphics.SetRenderTarget(mainRTT);
            RenderHeatLost();
            RenderHeatDistribution();
        }
    }

    void CreateMaterials()
    {
        mMaterial[(int)MaterialType.AddHeat] = new Material(Shader.Find("Weld/Heat/AddHeat"));
        mMaterial[(int)MaterialType.LoseHeat] = new Material(Shader.Find("Weld/Heat/LoseHeat"));
        mMaterial[(int)MaterialType.ShowHeatDistribution] = new Material(Shader.Find("Weld/Heat/HeatDistribution"));
    }

    void CreateCommandBuffer()
    {
        if (mCommandBuffer == null)
            mCommandBuffer = new CommandBuffer();
        mCommandBuffer.ClearRenderTarget(true, false, Color.clear);
        mCommandBuffer.DrawRenderer(mRenderer, mMaterial[(int)MaterialType.AddHeat]);
    }

    void CreateProcessmaps()
    {
        mProcessmaps[(int)Processmap.PreHeatFlux] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGBFloat);
        mProcessmaps[(int)Processmap.PreHeatFlux].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.PreHeatFlux].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.PreHeatFlux].name = string.Format("{0} Pre HeatFlux", gameObject.name);

        mProcessmaps[(int)Processmap.CurrHeatFlux] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGBFloat);
        mProcessmaps[(int)Processmap.CurrHeatFlux].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.CurrHeatFlux].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.CurrHeatFlux].name = string.Format("{0} Curr HeatFlux", gameObject.name);

        mProcessmaps[(int)Processmap.HeatFluxDistribution] = new RenderTexture(mTexWidth, mTexHeight, 0, RenderTextureFormat.ARGB32);
        mProcessmaps[(int)Processmap.HeatFluxDistribution].wrapMode = TextureWrapMode.Clamp;
        mProcessmaps[(int)Processmap.HeatFluxDistribution].filterMode = FilterMode.Bilinear;
        mProcessmaps[(int)Processmap.HeatFluxDistribution].name = string.Format("{0} HeatFlux Distribution", gameObject.name);

        for (int i = 0; i < mProcessmaps.Length; i++)
        {
            RenderTexture mainRTT = RenderTexture.active;
            RenderTexture.active = mProcessmaps[i];
            GL.Clear(false, true, new Color(0, 0, 0, 1));
            RenderTexture.active = mainRTT;
        }
    }

    //热量衰减
    void RenderHeatLost()
    {
        mMaterial[(int)MaterialType.LoseHeat].SetFloat("_MaxHeat", 1.0f);
        mMaterial[(int)MaterialType.LoseHeat].SetFloat("_HeatDamping", mHeatDamping);
        mMaterial[(int)MaterialType.LoseHeat].SetTexture("_HeatMap", mProcessmaps[(int)Processmap.CurrHeatFlux]);
        mProcessmaps[(int)Processmap.PreHeatFlux].DiscardContents();
        Graphics.Blit(mProcessmaps[(int)Processmap.CurrHeatFlux], mProcessmaps[(int)Processmap.PreHeatFlux], mMaterial[(int)MaterialType.LoseHeat]);

        RenderTexture temp = mProcessmaps[(int)Processmap.CurrHeatFlux];
        mProcessmaps[(int)Processmap.CurrHeatFlux] = mProcessmaps[(int)Processmap.PreHeatFlux];
        mProcessmaps[(int)Processmap.PreHeatFlux] = temp;
    }

    //渲染热流密度分布图（用颜色表示密度）
    void RenderHeatDistribution()
    {
        mDistributionTex.wrapMode = TextureWrapMode.Clamp;
        mMaterial[(int)MaterialType.ShowHeatDistribution].SetTexture("_HeatMap", mProcessmaps[(int)Processmap.CurrHeatFlux]);
        mMaterial[(int)MaterialType.ShowHeatDistribution].SetTexture("_DistributionMap", mDistributionTex);
        mProcessmaps[(int)Processmap.HeatFluxDistribution].DiscardContents();
        Graphics.Blit(mProcessmaps[(int)Processmap.CurrHeatFlux], mProcessmaps[(int)Processmap.HeatFluxDistribution], mMaterial[(int)MaterialType.ShowHeatDistribution]);
    }
}
