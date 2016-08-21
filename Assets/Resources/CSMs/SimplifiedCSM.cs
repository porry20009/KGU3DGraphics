using UnityEngine;

/*
 *此脚本直接挂在方向光上 (每个场景只能拥有单个SimplifiedCSM脚本)
 */
public class SimplifiedCSM : MonoBehaviour
{
    public enum CSMMode
    {
        IndependentScene, //独立于场景
        dependentScene    //依赖于场景
    };
    private GameObject m_gameObjShadowCast = null;
    [HideInInspector]
    public Projector m_projectorShadowCast = null;
    [HideInInspector]
    public RenderTexture m_rTTShadowmap = null;
    private Camera m_clipCamera = null;      //裁剪摄像机

    [HideInInspector]
    public Shader m_shaderPassOne = null;
    private Shader       m_shaderPassTwo = null;
    [HideInInspector]
    public Matrix4x4 m_matLightViewMatrix;
    private ClipCameraCS m_clipCameraCS = null;

    public int m_shadowMapSize = 256;
    public int m_sampleCount = 4;
    public float m_shadowDensity = 0.5f;
    public bool m_isBlurShadow = false;
    public float m_nearClipPlane = 0.1f;
    public float m_farClipPlane = 5.0f;
    public CSMMode m_csmMode;
    public LayerMask m_receiveShadowLayer;
    public LayerMask m_castShadowLayer;
    [HideInInspector]
    public int m_castProjectorLayer;

    Material m_castShadowMaterial;

    public float ShadowProjectSize
    {
        set
        {
            if (m_projectorShadowCast != null)
                m_projectorShadowCast.orthographicSize = value;
        }
    }

    void Awake()
    {
        if (m_shaderPassOne == null)
            m_shaderPassOne = Shader.Find("SimpleShadow/ShadowPassOne");
        if (m_shaderPassTwo == null)
            m_shaderPassTwo = Shader.Find("SimpleShadow/ShadowPassTwo");

		m_castShadowMaterial = new Material(m_shaderPassTwo);
    }
    void Start()
    {
        CreatePassOneRTT (m_shadowMapSize,m_sampleCount);
        CreateCastProjecter ();
        CreateClipCamera ();
        CreateLightViewMatrix ();
        AddClipCameraScript ();
    }

    void AddClipCameraScript()
    {
        if (m_clipCameraCS == null)
        {
            m_clipCameraCS = m_clipCamera.gameObject.AddComponent<ClipCameraCS>();
            m_clipCameraCS.m_simpleCSM = this;
        }
    }

    void CreateLightViewMatrix()
    {
        Vector3 _look = gameObject.transform.rotation * new Vector3(0,0,-1);
        Vector3 _up = gameObject.transform.rotation * new Vector3(0,1,0);
        Vector3 _right = gameObject.transform.rotation * new Vector3(1,0,0);
        Vector3 _pos = gameObject.transform.position;

        _look.Normalize ();
        _up.Normalize ();
        _right.Normalize ();
        // Build the view matrix:
        float x = -Vector3.Dot (_right,_pos);
        float y = -Vector3.Dot (_up,_pos);
        float z = -Vector3.Dot (_look,_pos);

        m_matLightViewMatrix.m00 = _right.x; m_matLightViewMatrix.m10 = _up.x; m_matLightViewMatrix.m20 = _look.x; m_matLightViewMatrix.m30 = 0.0f;
        m_matLightViewMatrix.m01 = _right.y; m_matLightViewMatrix.m11 = _up.y; m_matLightViewMatrix.m21 = _look.y; m_matLightViewMatrix.m31 = 0.0f;
        m_matLightViewMatrix.m02 = _right.z; m_matLightViewMatrix.m12 = _up.z; m_matLightViewMatrix.m22 = _look.z; m_matLightViewMatrix.m32 = 0.0f;
        m_matLightViewMatrix.m03 = x;        m_matLightViewMatrix.m13 = y;     m_matLightViewMatrix.m23 = z;       m_matLightViewMatrix.m33 = 1.0f;
        Shader.SetGlobalVector("g_v4LightDir",new Vector4(_look.x,_look.y,_look.z,1.0f));
        Shader.SetGlobalFloat("_ShadowDensity", m_shadowDensity);
    }

    public void ResetRTT(int nSize,int antiAliasing )
    {
        if (m_rTTShadowmap != null)
        {
            Destroy(m_rTTShadowmap);
            m_rTTShadowmap = null;
        }
        CreatePassOneRTT(nSize, antiAliasing);
        if (m_projectorShadowCast != null)
            m_projectorShadowCast.material.SetTexture("_ShadowTex", m_rTTShadowmap);
        if (m_clipCamera != null)
            m_clipCamera.targetTexture = m_rTTShadowmap;
        if (m_clipCameraCS != null)
        {
            m_clipCameraCS.ResetBlurShadowmapRTT();
        }
    }

    void CreatePassOneRTT(int nSize,int antiAliasing)
    {
        if (m_rTTShadowmap == null)
        {
            m_rTTShadowmap = new RenderTexture(nSize, nSize, 0, RenderTextureFormat.ARGB32);
            m_rTTShadowmap.wrapMode = TextureWrapMode.Clamp;
            m_rTTShadowmap.name = "ShadowPassOneRTT";
            m_rTTShadowmap.antiAliasing = antiAliasing;
            m_rTTShadowmap.isPowerOfTwo = true;
        }
    }

    void CreateCastProjecter()
    {
        if (m_gameObjShadowCast == null)
        {
            m_gameObjShadowCast = new GameObject("ShadowCastProjector");
            m_gameObjShadowCast.transform.parent = Camera.main.gameObject.transform;
            m_gameObjShadowCast.layer = m_castProjectorLayer;
            m_gameObjShadowCast.transform.localPosition = new Vector3(0,0,0);
            m_gameObjShadowCast.transform.localRotation = new Quaternion(0,0,0,1);

            m_projectorShadowCast = m_gameObjShadowCast.AddComponent<Projector>();
            m_projectorShadowCast.orthographic = true;
            m_projectorShadowCast.orthographicSize = 5.0f;
            m_projectorShadowCast.nearClipPlane = 0.3f;
            m_projectorShadowCast.farClipPlane = 100.0f;
            m_projectorShadowCast.material = m_castShadowMaterial;
            m_projectorShadowCast.material.SetTexture("_ShadowTex", m_rTTShadowmap);
            m_projectorShadowCast.ignoreLayers = ~m_receiveShadowLayer;
            m_projectorShadowCast.enabled = true;
        }
        else
        {
            m_gameObjShadowCast.SetActive(false);
        }
    }

    public void SetShadowCastLayer(int castProjectorLayer)
    {
        if (m_gameObjShadowCast != null)
            m_gameObjShadowCast.layer = castProjectorLayer;
        m_castProjectorLayer = castProjectorLayer;
    }

    void CreateClipCamera()
    {
        if (m_clipCamera == null)
        {
            m_clipCamera = EffectHelp.S.CreateRenderCamera(Camera.main.gameObject.transform, Camera.main, "ClipCamera", EffectHelp.CameraDepth.CSM);
            m_clipCamera.targetTexture = m_rTTShadowmap;
            m_clipCamera.cullingMask = m_castShadowLayer;
            m_clipCamera.aspect = Camera.main.aspect;
        }
        else
        {
            m_clipCamera.gameObject.SetActive(true);
        }
    }

    // Cleanup all the objects we possibly have created
    void OnDestroy()
    {
        if (m_rTTShadowmap)
        {
            Destroy(m_rTTShadowmap);
            m_rTTShadowmap = null;
        }
        if (m_clipCamera != null)
        {
            Destroy(m_clipCamera.gameObject);
            m_clipCamera = null;
        }
        if (m_gameObjShadowCast != null)
        {
            Destroy(m_gameObjShadowCast);
            m_gameObjShadowCast = null;
        }
    }
    //void OnDisable()
    //{
    //    if (m_clipCamera != null)
    //    {
    //        m_clipCamera.gameObject.SetActive(false);
    //    }
    //    if (m_gameObjShadowCast != null)
    //    {
    //        m_gameObjShadowCast.SetActive(false);
    //    }
    //}
    //void OnEnable()
    //{
    //    if (m_projectorShadowCast != null)
    //    {
    //        m_gameObjShadowCast.SetActive(true);
    //    }
    //    if (m_clipCamera != null)
    //    {
    //        m_clipCamera.gameObject.SetActive(true);
    //    }
    //}

    public void MergeObjectAABB(Vector3 max,Vector3 min)
    {
        if (m_csmMode == CSMMode.dependentScene)
            m_clipCameraCS.MergeObjectAABB(max, min);
    }

    //关闭CSM(如果是销毁资源，那么切换阴影质量的时候就不好实时更新了)
    public static void Close(GameObject mainLight)
    {
        if (mainLight == null)
            return;
        SimplifiedCSM csm = mainLight.GetComponent<SimplifiedCSM>();
        if (csm != null)
        {
            Destroy(csm);
           // csm.enabled = false;
        }
    }

    public static SimplifiedCSM Open(Camera mainCamera, GameObject mainLight, int shadowmapSize, int sampleCount, LayerMask castShadowLayer, LayerMask receiveShadowLayer, bool isBlurShadow, float shadowDensity)
    {
        if (mainLight == null)
            return null;
        SimplifiedCSM csm = mainLight.GetComponent<SimplifiedCSM>();
        if (csm == null)
        {
            csm = mainLight.AddComponent<SimplifiedCSM>();
            csm.m_castShadowLayer = castShadowLayer;
            csm.m_receiveShadowLayer = receiveShadowLayer;
            csm.m_castProjectorLayer = LayerMask.NameToLayer("Terrain");
            csm.m_shadowDensity = shadowDensity;
            csm.m_shadowMapSize = shadowmapSize;
            csm.m_sampleCount = sampleCount;
            csm.m_isBlurShadow = isBlurShadow;
        }
        else
        {
            csm.SetShadowCastLayer( LayerMask.NameToLayer("Terrain"));
            csm.m_shadowMapSize = shadowmapSize;
            csm.m_sampleCount = sampleCount;
            csm.m_isBlurShadow = isBlurShadow;
            csm.ResetRTT(shadowmapSize, sampleCount);
            csm.enabled = true;
        }

        return csm;
    }
}