using UnityEngine;
using System.Collections;

public class Weld : MonoBehaviour 
{
    public Camera mMainCamera = null;
    public RenderTexture mWeldHeightmap = null;
    public RenderTexture mWeldNormalmap = null;
    [Range(0,1)]
    public float mNormalScale = 0.5f;
    public float mForce = 1.0f;
    [Range(0,1)]
    public float mRange = 0.03f;
    [Range(0, 1)]
    public float mSmoothness = 0.01f;

    Camera mPreCalcuCamera = null;

    Material mPreCalcuMaterial = null;
    Material mFinalMaterial = null;
    Material mHeightToNormalMaterial = null;
    Renderer mRenderer = null;

	void Awake () 
    {
        if (mMainCamera == null)
            mMainCamera = Camera.main;
        EffectHelp.S.CreateGridPlane(gameObject, null, 128, 128, 10, 10, new Vector3(-10 * 0.5f, 0.0f, -10 * 0.5f));
        mRenderer = gameObject.GetComponent<Renderer>();
        gameObject.AddComponent<MeshCollider>();
        CreateMaterial();
        CreateProcessmap();
        CreateCamera();
	}
	
	void OnDestroy ()
    {
        EffectHelp.S.SaveRelease<RenderTexture>(ref mWeldHeightmap);
        EffectHelp.S.SaveRelease<RenderTexture>(ref mWeldNormalmap);
        EffectHelp.S.SaveRelease<Camera>(ref mPreCalcuCamera);
	}

    void OnWillRenderObject()
    {
        if (mMainCamera == null)
            mMainCamera = Camera.main;
        if (mRenderer == null || mMainCamera == null)
            return;
        if (Camera.current.name.Equals(mMainCamera.name))
        {
            mFinalMaterial.SetTexture("_WeldHeightmap", mWeldHeightmap);
            mFinalMaterial.SetTexture("_WeldNormalmap", mWeldNormalmap);
            mRenderer.sharedMaterial = mFinalMaterial;
        }
        else if (Camera.current.name.Equals("PreCalcuCamera"))
        {
            //-----------------------------Test------------------------------
            if (Input.GetMouseButton(0))
            {
                Ray ray = mMainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.collider.name.Equals(gameObject.name))
                {
                    mPreCalcuMaterial.SetVector("_DrawUV", hit.textureCoord);
                    mPreCalcuMaterial.SetFloat("_Force", mForce * Time.deltaTime);
                    mPreCalcuMaterial.SetFloat("_Range", mRange);
                    mPreCalcuMaterial.SetFloat("_Smoothness", mSmoothness);
                }
            }
            else
            {
                mPreCalcuMaterial.SetFloat("_Force", 0.0f);
            }
            //---------------------------------------------------------------

            mRenderer.sharedMaterial = mPreCalcuMaterial;
        }
    }

    void OnRenderObject()
    {
        if (Camera.current.name.Equals("PreCalcuCamera"))
        {
            mHeightToNormalMaterial.SetTexture("_HeightCurrentTex",mWeldHeightmap);
            mHeightToNormalMaterial.SetVector("_TextureSize",new Vector4(1.0f/1024.0f,1.0f/1024.0f,0.0f,0.0f));
            mHeightToNormalMaterial.SetFloat("_TexelLength_x2", mNormalScale);
            mWeldNormalmap.DiscardContents();
            Graphics.Blit(mWeldHeightmap, mWeldNormalmap, mHeightToNormalMaterial);
        }
    }

    void CreateMaterial()
    {
        mPreCalcuMaterial = new Material(Shader.Find("Weld/PreCalcu"));
        mFinalMaterial = new Material(Shader.Find("Weld/WeldObject"));
        mHeightToNormalMaterial = new Material(Shader.Find("PerlinNoiseWater/HeightToNormal"));
    }

    void CreateProcessmap()
    {
        if (mWeldHeightmap == null)
        {
            mWeldHeightmap = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
            mWeldHeightmap.wrapMode = TextureWrapMode.Clamp;
            mWeldHeightmap.name = "Weld Heightmap";
            mWeldHeightmap.antiAliasing = 1;

            RenderTexture mainRTT = RenderTexture.active;
            RenderTexture.active = mWeldHeightmap;
            GL.Clear(false, true, new Color(0, 0, 0, 1));
            RenderTexture.active = mainRTT;
        }

        if (mWeldNormalmap == null)
        {
            mWeldNormalmap = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
            mWeldNormalmap.wrapMode = TextureWrapMode.Clamp;
            mWeldNormalmap.name = "Weld Normalmap";
            mWeldNormalmap.antiAliasing = 1;

            RenderTexture mainRTT = RenderTexture.active;
            RenderTexture.active = mWeldNormalmap;
            GL.Clear(false, true, new Color(0, 0, 0, 1));
            RenderTexture.active = mainRTT;
        }
    }

    void CreateCamera()
    {
        if (mPreCalcuCamera == null)
        {
            mPreCalcuCamera = EffectHelp.S.CreateRenderCamera(mMainCamera.gameObject.transform, mMainCamera, "PreCalcuCamera", EffectHelp.CameraDepth.Others);
            mPreCalcuCamera.targetTexture = mWeldHeightmap;
            mPreCalcuCamera.clearFlags = CameraClearFlags.Nothing;
            mPreCalcuCamera.cullingMask &= gameObject.layer;
            int i = 0;
        }
    }
}
