using UnityEngine;

//物体对水面单向交互(二维波动方程)
/*bug:
 *Shader.SetGlobalFloat("_WaterPlaneY", gameObject.transform.position.y);
 *所以视野范围内出现多个高度不同的水面时，就会出现问题 
 */

public class SmallScaleWater : MonoBehaviour
{
    public delegate void UpdateWaterParameterFunc(params System.Object[] args);

    public enum RefractTexScale
    {
        FullSize,
        DownScale_2,
        DownScale_4,
        DownScale_8
    };

    public Camera m_mainCamera = null;
    public LayerMask mCanRefractLayer;
    public LayerMask mCanDisturbWaterLayer;
    public Texture mReflectTexture = null;
    public Texture mWaterShapeTexture = null;//水面的形状
    public RefractTexScale mRefractTexScale = RefractTexScale.FullSize;

    public int mTexWidth = 128;
    public int mTexHeight = 128;
    public float m_waterPlaneOffset = 0.1f;
    [Range(0, 1)]
    public float mDampingRatio = 0.97f;  //区间[0,1]
    [Range(0, 10)]
    public float mDistortValue = 0.5f;  //扭曲值
   // [Range(0, 1)]
    float mForce = 0.5f;          //压力大小
    [Range(0, 1)]
    public float mFrenelBias = 0.5f;
    [Range(0, 1)]
    public float mNormalScale = 0.5f;

    public Transform mSun = null;
    public Vector3 mSunDir = Vector3.one;
    [Range(0, 300)]
    public float mSunspot = 100.0f;
    public Color mWaterBodyColor = new Color32(24, 91, 98, 255);

    bool mIsRefraction = false;
    RenderTexture mWaterNormal = null;
    Material mWaterMaterial = null;

    Renderer mRenderer = null;
    Camera mForceCamera = null;
    AddForceCameraScript mAddForceCameraScript = null;
    UpdateWaterParameterFunc mUpdateWaterParameterCallBack = null;

    int mCurrWaterNumber = 0;
    static int mSCurrWaterCount = 0;
    static RenderTexture mRefractTexture = null;
    static Camera mRefractCamera = null;

    bool mIsPreRefraction = false;
    RefractTexScale mPreRefractTexScale = RefractTexScale.FullSize;
    int mRefractTexWidth = 256;
    int mRefractTexHeight = 256;
    void Awake()
    {
        if (m_mainCamera == null)
            m_mainCamera = Camera.main;
        mRenderer = gameObject.GetComponent<Renderer>();
        mIsRefraction = (mCanRefractLayer == 0 ? false : true);
        mPreRefractTexScale = mRefractTexScale;
        CreateRefractmap();
        CreateMaterial();
        CreateRefractCamera();
        CreateForceCamera();
        SetGPUParameter();
        mSCurrWaterCount++;
        mCurrWaterNumber = mSCurrWaterCount;
        Debug.Log("SmallScaleWater " + mCurrWaterNumber.ToString() + "Has Initialized!");
    }

    void InitPostAddForceCameraScript(RenderTexture[] tex)
    {
        mWaterNormal = tex[0];
        mWaterMaterial.SetTexture("_WaterNormal", tex[0]);
    }

    void OnWillRenderObject()
    {
        if (!enabled || !mRenderer || !mRenderer.sharedMaterial || !mRenderer.enabled)
            return;

        Camera cam = Camera.current;
        if (!cam)
            return;

        if (cam.name.Equals(m_mainCamera.name))
        {
#if UNITY_EDITOR
            SetGPUParameter();
            if (mPreRefractTexScale != mRefractTexScale)
            {
                Destroy(mRefractTexture);
                mRefractTexture = null;
                CreateRefractmap();
                mRefractCamera.targetTexture = mRefractTexture;
            }
            mPreRefractTexScale = mRefractTexScale;
#endif
            RealTimeRefraction();
        }
    }

    //当水面不在视野时关闭ForceCamera
    void OnBecameInvisible()
    {
        mAddForceCameraScript.GetCamera().enabled = false;
        Debug.Log("SmallScaleWater " + mCurrWaterNumber.ToString() + "became invisible!");
    }
    void OnBecameVisible()
    {
        mAddForceCameraScript.GetCamera().enabled = true;
        Debug.Log("SmallScaleWater " + mCurrWaterNumber.ToString() + "became visible!");
    }

    void OnDestroy()
    {
        mSCurrWaterCount--;
        if (mSCurrWaterCount <= 0 && mRefractCamera != null)
        {
            Destroy(mRefractCamera.gameObject);
            mRefractCamera = null;
        }
        if (mForceCamera != null)
        {
            Destroy(mForceCamera.gameObject);
            mForceCamera = null;
        }
        if (mSCurrWaterCount <= 0 && mRefractTexture != null)
        {
            Destroy(mRefractTexture);
            mRefractTexture = null;
        }
        Debug.Log("SmallScaleWater " + mCurrWaterNumber.ToString() + "Has Destroy!");
    }

    void CreateMaterial()
    {
        if (mIsRefraction)
            mWaterMaterial = new Material(Shader.Find("Water/SmallScale/RenderWater_Refraction"));
        else
            mWaterMaterial = new Material(Shader.Find("Water/SmallScale/RenderWater"));
        mRenderer.sharedMaterial = mWaterMaterial;
    }

    void CreateRefractmap()
    {

        int scaleRatio = 1;
        if (mRefractTexScale == RefractTexScale.DownScale_2)
            scaleRatio = 2;
        else if (mRefractTexScale == RefractTexScale.DownScale_4)
            scaleRatio = 4;
        else if (mRefractTexScale == RefractTexScale.DownScale_8)
            scaleRatio = 8;
        mRefractTexWidth = Screen.width/scaleRatio;
        mRefractTexHeight = Screen.height/scaleRatio;
#if UNITY_EDITOR
        if (mRefractTexture == null)
        {
            mRefractTexture = new RenderTexture(mRefractTexWidth, mRefractTexHeight, 16, RenderTextureFormat.ARGB32);
            mRefractTexture.wrapMode = TextureWrapMode.Clamp;
            mRefractTexture.name = "Refract Texture";
            mRefractTexture.isPowerOfTwo = true;
            mRefractTexture.hideFlags = HideFlags.DontSave;
        }
#else
        if (mIsRefraction && mRefractTexture == null)
        {
            mRefractTexture = new RenderTexture(mRefractTexWidth, mRefractTexHeight, 16, RenderTextureFormat.ARGB32);
            mRefractTexture.wrapMode = TextureWrapMode.Clamp;
            mRefractTexture.name = "Refract Texture";
            mRefractTexture.isPowerOfTwo = true;
            mRefractTexture.hideFlags = HideFlags.DontSave;
        }
#endif
    }
    void CreateRefractCamera()
    {
#if UNITY_EDITOR
        if (mRefractCamera == null)
        {
            mRefractCamera = CreateRenderCamera(m_mainCamera.gameObject.transform, m_mainCamera, "RefractCamera", -4);
            mRefractCamera.enabled = false;
            mRefractCamera.targetTexture = mRefractTexture;
            mRefractCamera.cullingMask = mCanRefractLayer;
        }
#else
        if (mIsRefraction && mRefractCamera == null)
        {
            mRefractCamera = CreateRenderCamera(m_mainCamera.gameObject.transform, m_mainCamera, "RefractCamera", -4);
            mRefractCamera.enabled = false;
            mRefractCamera.targetTexture = mRefractTexture;
            mRefractCamera.cullingMask = mCanRefractLayer;
        }
#endif

    }
    void CreateForceCamera()
    {
        if (mForceCamera == null)
        {
            mForceCamera = CreateRenderCamera(gameObject.transform, null, "ForceCamera", -3);
            mForceCamera.enabled = true;
            mForceCamera.targetTexture = null;
            mForceCamera.cullingMask = mCanDisturbWaterLayer;
            mForceCamera.clearFlags = CameraClearFlags.Nothing;
            mForceCamera.orthographic = true;
            mForceCamera.transform.Rotate(new Vector3(90, 180, 0), Space.Self);
            mForceCamera.nearClipPlane = -1000.0f;
            mForceCamera.farClipPlane = 1000.0f;
            mAddForceCameraScript = mForceCamera.gameObject.AddComponent<AddForceCameraScript>();
            mAddForceCameraScript.InitializeCallBack = InitPostAddForceCameraScript;
            mUpdateWaterParameterCallBack = mAddForceCameraScript.UpdateWaterParameter;

            mAddForceCameraScript.mTexWidth = mTexWidth;
            mAddForceCameraScript.mTexHeight = mTexHeight;
            mAddForceCameraScript.mDampingRatio = mDampingRatio;
            mAddForceCameraScript.mForce = mForce;
            mAddForceCameraScript.mWaterShapeTex = mWaterShapeTexture;
        }
    }

    //void CreateDrops()
    //{
    //    GameObject go = new GameObject("Drops");
    //    int compare = 1 ; 
    //    for (int i = 0; i < 32; i++ )
    //    {
    //        int f = mCanDisturbWaterLayer.value & compare;
    //        if (f > 0)
    //        {
    //            go.layer = i;
    //            break;
    //        }
    //        compare *= 2;
    //    }
    //    go.transform.parent = transform;
    //    go.transform.localPosition = new Vector3(0,1,0);
    //    go.transform.localRotation = Quaternion.identity;
    //    go.transform.localScale = Vector3.one;
    //    go.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);

    //    mDropObjects = go.GetComponent<ParticleSystem>();
    //    if (mDropObjects == null)
    //        mDropObjects = go.AddComponent<ParticleSystem>();
    //    ParticleSystem.CollisionModule collisionModule = mDropObjects.collision;
    //    collisionModule.SetPlane(0, transform);
    //    collisionModule.minKillSpeed = 1.0f;
    //    ParticleSystem.MinMaxCurve minmaxCurve = new ParticleSystem.MinMaxCurve();
    //    minmaxCurve.constantMax = 0.0f;
    //    minmaxCurve.constantMin = 0.0f;
    //    collisionModule.bounce = minmaxCurve;
    //    collisionModule.enabled = true;

    //    ParticleSystem.ShapeModule shapeModule = mDropObjects.shape;
    //    shapeModule.shapeType = ParticleSystemShapeType.Box;
    //    shapeModule.box = new Vector3(mWaterShapeSize, mWaterShapeSize);

    //    mDropObjects.startSize = mDropSize;
    //    mDropObjects.startSpeed = mDropFreq;
    //}
    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float planeOffset, float sideSign)
    {
        Vector3 offsetPos = pos + normal * planeOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    //创建正交投影矩阵
    void BuildOrthogonalProjectMatrix(ref Matrix4x4 projectMatrix, Vector3 v3MaxInViewSpace, Vector3 v3MinInViewSpace)
    {
        float scaleX, scaleY, scaleZ;
        float offsetX, offsetY, offsetZ;
        scaleX = 2.0f / (v3MaxInViewSpace.x - v3MinInViewSpace.x);
        scaleY = 2.0f / (v3MaxInViewSpace.y - v3MinInViewSpace.y);
        offsetX = -0.5f * (v3MaxInViewSpace.x + v3MinInViewSpace.x) * scaleX;
        offsetY = -0.5f * (v3MaxInViewSpace.y + v3MinInViewSpace.y) * scaleY;
        scaleZ = 1.0f / (v3MaxInViewSpace.z - v3MinInViewSpace.z);
        offsetZ = -v3MinInViewSpace.z * scaleZ;

        //列矩阵
        projectMatrix.m00 = scaleX; projectMatrix.m01 = 0.0f; projectMatrix.m02 = 0.0f; projectMatrix.m03 = offsetX;
        projectMatrix.m10 = 0.0f; projectMatrix.m11 = scaleY; projectMatrix.m12 = 0.0f; projectMatrix.m13 = offsetY;
        projectMatrix.m20 = 0.0f; projectMatrix.m21 = 0.0f; projectMatrix.m22 = scaleZ; projectMatrix.m23 = offsetZ;
        projectMatrix.m30 = 0.0f; projectMatrix.m31 = 0.0f; projectMatrix.m32 = 0.0f; projectMatrix.m33 = 1.0f;
    }

    Camera CreateRenderCamera(Transform parent, Camera copyCamara, string name, int depth)
    {
        GameObject cameraObj = new GameObject(name);
        cameraObj.transform.parent = parent;
        cameraObj.transform.localPosition = new Vector3(0, 0, 0);
        cameraObj.transform.localRotation = new Quaternion(0, 0, 0, 1);

        Camera camera = cameraObj.AddComponent<Camera>();
        if (copyCamara != null)
            camera.CopyFrom(copyCamara);
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.clearFlags = CameraClearFlags.Color;
        //amera.hideFlags = HideFlags.HideAndDontSave;
        camera.useOcclusionCulling = false;
        camera.renderingPath = RenderingPath.Forward;
        camera.depth = depth;
        return camera;
    }

    void SetGPUParameter()
    {
#if UNITY_EDITOR
        mIsRefraction = (mCanRefractLayer == 0 ? false : true);
        if (mIsPreRefraction != mIsRefraction)
        {
            CreateMaterial();
        }
        mIsPreRefraction = mIsRefraction;
#endif
        if (mRefractCamera != null)
            mRefractCamera.cullingMask = mCanRefractLayer;
        mForceCamera.cullingMask = mCanDisturbWaterLayer;

        if (mIsRefraction)
        {
            mWaterMaterial.SetTexture("_RefractTex", mRefractTexture);
            mWaterMaterial.SetFloat("_DistortLevel", mDistortValue);
        }
        mWaterMaterial.SetTexture("_ReflectTex", mReflectTexture);
        mWaterMaterial.SetTexture("_WaterNormal", mWaterNormal);
        mWaterMaterial.SetTexture("_WaterShapeTex", mWaterShapeTexture);
        mWaterMaterial.SetColor("_WaterColor", mWaterBodyColor);
        mWaterMaterial.SetFloat("_FrenelBias", mFrenelBias);
        Shader.SetGlobalFloat("_WaterPlaneY", gameObject.transform.position.y);

        if (mSun != null)
            mSunDir = -mSun.forward;
        mWaterMaterial.SetVector("_SunDir", new Vector4(mSunDir.x, mSunDir.y, mSunDir.z, mSunspot));

        Matrix4x4 matAABBPoj = Matrix4x4.identity;
        Vector3 v3MaxInViewSpace = Vector3.zero;
        Vector3 v3MinInViewSpace = Vector3.zero;
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Renderer rd = gameObject.GetComponent<Renderer>();
        Vector3 OBBMin =  rd.localToWorldMatrix.MultiplyPoint3x4(mesh.bounds.min);
        Vector3 OBBMax =  rd.localToWorldMatrix.MultiplyPoint3x4(mesh.bounds.max);

        CalcuWaterPlaneAABBInViewSpace(mForceCamera, OBBMin, OBBMax, ref v3MaxInViewSpace, ref v3MinInViewSpace);
        BuildOrthogonalProjectMatrix(ref matAABBPoj, v3MaxInViewSpace, v3MinInViewSpace);
        mForceCamera.projectionMatrix = matAABBPoj;

        Matrix4x4 matProj = GL.GetGPUProjectionMatrix(mForceCamera.projectionMatrix, true);
        Matrix4x4 matSVP = matProj * mForceCamera.worldToCameraMatrix;
        mWaterMaterial.SetMatrix("g_matForceViewProj", matSVP);

        if (mUpdateWaterParameterCallBack != null)
        {
            if (mWaterShapeTexture != null)
            {
                mUpdateWaterParameterCallBack(new System.Object[] { mTexWidth, mTexHeight, mDampingRatio, mForce,mNormalScale, mWaterShapeTexture });
            }
            else
            {
                mUpdateWaterParameterCallBack(new System.Object[] { mTexWidth, mTexHeight, mDampingRatio, mForce,mNormalScale });
            }
        }
    }

    void RealTimeRefraction()
    {
        if (mIsRefraction)
        {
            Vector3 pos = gameObject.transform.position;
            Vector3 normal = new Vector3(0, 1, 0);
            Vector4 waterClipPlane = CameraSpacePlane(mRefractCamera, pos, normal, m_waterPlaneOffset, -1.0f);
            Matrix4x4 matrixRefract = m_mainCamera.CalculateObliqueMatrix(waterClipPlane);
            mRefractCamera.projectionMatrix = matrixRefract;
            mRefractCamera.Render();
        }
    }

    void CalcuWaterPlaneAABBInViewSpace(Camera camera, Vector3 OBBMin,Vector3 OBBMax, ref Vector3 v3MaxInViewSpace, ref Vector3 v3MinInViewSpace)
    {
        Vector3[] vertices = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
        Vector3 min = OBBMin + new Vector3(0, -1, 0);
        Vector3 max = OBBMax + new Vector3(0, 1, 0);

        vertices[0].x = min.x; vertices[0].y = min.y; vertices[0].z = min.z;
        vertices[1].x = max.x; vertices[1].y = min.y; vertices[1].z = min.z;
        vertices[2].x = min.x; vertices[2].y = min.y; vertices[2].z = max.z;
        vertices[3].x = max.x; vertices[3].y = min.y; vertices[3].z = max.z;

        vertices[4].x = min.x; vertices[4].y = max.y; vertices[4].z = min.z;
        vertices[5].x = max.x; vertices[5].y = max.y; vertices[5].z = min.z;
        vertices[6].x = min.x; vertices[6].y = max.y; vertices[6].z = max.z;
        vertices[7].x = max.x; vertices[7].y = max.y; vertices[7].z = max.z;

        //计算在MainCamera视图空间Object的AABB
        Vector3 v3MaxPosition = Vector3.one * -50000;
        Vector3 v3MinPosition = Vector3.one * 500000;

        for (int vertId = 0; vertId < 8; ++vertId)
        {
            Vector3 v3Position = camera.worldToCameraMatrix.MultiplyPoint3x4(vertices[vertId]);
            if (v3Position.x > v3MaxPosition.x)
                v3MaxPosition.x = v3Position.x;
            if (v3Position.y > v3MaxPosition.y)
                v3MaxPosition.y = v3Position.y;
            if (v3Position.z > v3MaxPosition.z)
                v3MaxPosition.z = v3Position.z;

            if (v3Position.x < v3MinPosition.x)
                v3MinPosition.x = v3Position.x;
            if (v3Position.y < v3MinPosition.y)
                v3MinPosition.y = v3Position.y;
            if (v3Position.z < v3MinPosition.z)
                v3MinPosition.z = v3Position.z;
        }
        v3MinInViewSpace = v3MinPosition;
        v3MaxInViewSpace = v3MaxPosition;
    }
}