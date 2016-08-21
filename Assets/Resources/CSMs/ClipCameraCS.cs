using UnityEngine;
using System.Collections.Generic;
//using Vector3 = UnityEngine.Vector3;

public class ClipCameraCS : MonoBehaviour
{
    private Vector3 MAX_OBJ_POSITION = Vector3.one * 50000.0f;
    private Vector3 MIN_OBJ_POSITION = Vector3.one * -50000.0f;
    private struct FrustumAABB
    {
        public Vector3 v3Max;
        public Vector3 v3Min;
        public Matrix4x4 cropMatrix;
    }

    private struct ObjectAABB
    {
        public Vector3 v3Max;
        public Vector3 v3Min;
    }

    // Use this for initialization
    private List<ObjectAABB> m_listObjectAABB = new List<ObjectAABB>();
    private  FrustumAABB m_frustumAABB;
    private Material m_gaussianBlurMaterial = null;

    RenderTexture m_blurShadowmap = null;
    Shader    m_shaderPassOne = null;
    Matrix4x4 m_matLightViewMatrix;
    bool        m_isBlurShadow = false;
    int          m_shadowMapSize = 256;
    Projector m_projectorShadowCast = null;
    SimplifiedCSM.CSMMode m_csmMode = SimplifiedCSM.CSMMode.dependentScene;
    RenderTexture m_rTTShadowmap = null;

    public SimplifiedCSM m_simpleCSM = null;

    void Start ()
    {
        m_shaderPassOne = m_simpleCSM.m_shaderPassOne;
        m_matLightViewMatrix = m_simpleCSM.m_matLightViewMatrix;
        m_isBlurShadow = m_simpleCSM.m_isBlurShadow;
        m_shadowMapSize = m_simpleCSM.m_shadowMapSize;
        m_projectorShadowCast = m_simpleCSM.m_projectorShadowCast;
        m_csmMode = m_simpleCSM.m_csmMode;
        m_rTTShadowmap = m_simpleCSM.m_rTTShadowmap;

		m_gaussianBlurMaterial = new Material(Shader.Find("PostEffect/GaussianBlur"));
        m_gaussianBlurMaterial.SetVector("_BlurTexSizeInverse", new Vector4(1.0f / (float)m_shadowMapSize, 1.0f / (float)m_shadowMapSize, 0, 0));
        m_frustumAABB = new FrustumAABB();
		GetComponent<Camera>().SetReplacementShader(m_shaderPassOne, "");
        if (m_isBlurShadow)
        {
            CreateBlurShadowmap(m_shadowMapSize);
            m_projectorShadowCast.material.SetTexture("_ShadowTex", m_blurShadowmap);
        }
    }
    public void ResetBlurShadowmapRTT()
    {
        m_shaderPassOne = m_simpleCSM.m_shaderPassOne;
        m_matLightViewMatrix = m_simpleCSM.m_matLightViewMatrix;
        m_isBlurShadow = m_simpleCSM.m_isBlurShadow;
        m_shadowMapSize = m_simpleCSM.m_shadowMapSize;
        m_projectorShadowCast = m_simpleCSM.m_projectorShadowCast;
        m_csmMode = m_simpleCSM.m_csmMode;
        m_rTTShadowmap = m_simpleCSM.m_rTTShadowmap;
        if (m_gaussianBlurMaterial != null)
            m_gaussianBlurMaterial.SetVector("_BlurTexSizeInverse", new Vector4(1.0f / (float)m_shadowMapSize, 1.0f / (float)m_shadowMapSize, 0, 0));

        if (m_blurShadowmap !=null)
        {
            Destroy(m_blurShadowmap);
            m_blurShadowmap = null;
        }
        if (m_isBlurShadow)
        {
            CreateBlurShadowmap(m_shadowMapSize);
            m_projectorShadowCast.material.SetTexture("_ShadowTex", m_blurShadowmap);
        }
    }

    void CreateBlurShadowmap(int nSize)
    {
        if (m_blurShadowmap == null)
        {
            m_blurShadowmap = new RenderTexture(nSize, nSize, 0, RenderTextureFormat.ARGB32);
            m_blurShadowmap.wrapMode = TextureWrapMode.Clamp;
            m_blurShadowmap.name = "BlurShadowmap";
            m_blurShadowmap.isPowerOfTwo = true;
        }
    }

    void OnDestroy()
    {
        if (m_blurShadowmap != null)
        {
            Destroy(m_blurShadowmap);
            m_blurShadowmap = null;
        }
    }
    //void RenderFullScreenQuad(RenderTexture src,RenderTexture dest,Material mat,int pass)
    //{
    //    GL.PushMatrix();
    //    mat.SetPass(pass);
    //    mat.SetTexture("_MainTex", src);
    //    Graphics.SetRenderTarget(dest);
    //    GL.LoadOrtho();
    //    GL.Begin(GL.TRIANGLES);
    //    GL.Vertex3(0, 0.5f, 0);
    //    GL.TexCoord2(0, 0);
    //    GL.Vertex3(0.5f, 0.5f, 0);
    //    GL.TexCoord2(0, 0);
    //    GL.Vertex3(0, 0, 0);
    //    GL.TexCoord2(0, 1);

    //    GL.Vertex3(0.5f, 0.5f, 0);
    //    GL.TexCoord2(1, 0);
    //    GL.Vertex3(0.5f, 0, 0);
    //    GL.TexCoord2(1, 1);
    //    GL.Vertex3(0, 0, 0);
    //    GL.TexCoord2(0, 1);

    //    GL.End();
    //    GL.PopMatrix();
    //}

    void Update()
    {
        if (m_simpleCSM == null)
            return;
    }

    void OnPreRender()
    {
        if (m_csmMode == SimplifiedCSM.CSMMode.IndependentScene)
        {
            UpdateFrustumAABB(m_simpleCSM.m_nearClipPlane, m_simpleCSM.m_farClipPlane);
        }
        else
        {
            UpdateObjectUnitAABB();
        }
        UpdateCropMatrix ();
        Matrix4x4 matCrop = GL.GetGPUProjectionMatrix(m_frustumAABB.cropMatrix,false);
        Matrix4x4 matLVP = matCrop * m_matLightViewMatrix;
        Shader.SetGlobalMatrix("g_matLightViewProj",matLVP);
    }
    void OnPostRender()
    {
        if (m_isBlurShadow)
        {
            RenderTexture temp = RenderTexture.GetTemporary(m_shadowMapSize, m_shadowMapSize, 0, RenderTextureFormat.ARGB32);
            temp.DiscardContents();
            Graphics.Blit(m_rTTShadowmap, temp, m_gaussianBlurMaterial, 0);
            m_blurShadowmap.DiscardContents();
            Graphics.Blit(temp, m_blurShadowmap, m_gaussianBlurMaterial, 1);
            RenderTexture.ReleaseTemporary(temp);
        }
    }
    Vector3[] vertices = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
    //
    //独立于场景计算的AABB
    //
    void UpdateFrustumAABB(float fNearClip,float fFarClip)
    {
		GetComponent<Camera>().farClipPlane = fFarClip;
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 viewDir = Camera.main.transform.rotation * new Vector3(0, 0, 1);
        Vector3 upDir = Camera.main.transform.rotation * new Vector3(0, 1, 0);
        Vector3 rightDir = Camera.main.transform.rotation * new Vector3(1, 0, 0);

        float fRadian = Mathf.PI * Camera.main.fieldOfView / 180.0f;
        float nearHeight = 2.0f * Mathf.Tan(fRadian * 0.5f) * fNearClip;
        float nearWidth = nearHeight * Camera.main.aspect;
        float farHeight = 2.0f * Mathf.Tan(fRadian * 0.5f) * fFarClip;
        float farWidth = farHeight * Camera.main.aspect;
        Vector3 nc = cameraPos + viewDir * fNearClip;
        Vector3 fc = cameraPos + viewDir * fFarClip;

        // Vertices in a world space.
        vertices[0] = nc - upDir * nearHeight * 0.5f - rightDir * nearWidth * 0.5f; // nbl (near, bottom, left)
        vertices[1] = nc - upDir * nearHeight * 0.5f + rightDir * nearWidth * 0.5f; // nbr
        vertices[2] = nc + upDir * nearHeight * 0.5f + rightDir * nearWidth * 0.5f; // ntr
        vertices[3] = nc + upDir * nearHeight * 0.5f - rightDir * nearWidth * 0.5f;// ntl
        vertices[4] = fc - upDir * farHeight * 0.5f - rightDir * farWidth * 0.5f; // fbl (far, bottom, left)
        vertices[5] = fc - upDir * farHeight * 0.5f + rightDir * farWidth * 0.5f; // fbr
        vertices[6] = fc + upDir * farHeight * 0.5f + rightDir * farWidth * 0.5f; // ftr
        vertices[7] = fc + upDir * farHeight * 0.5f - rightDir * farWidth * 0.5f;// ftl

        Vector3 v3MaxPosition = MIN_OBJ_POSITION;
        Vector3 v3MinPosition = MAX_OBJ_POSITION;

        for (int vertId = 0; vertId < 8; ++vertId)
        {
            // Light view space
            Vector3 v3Position = m_matLightViewMatrix.MultiplyPoint3x4(vertices[vertId]);
            if (v3Position.x > v3MaxPosition.x)
            {
                v3MaxPosition.x = v3Position.x;
            }
            if (v3Position.y > v3MaxPosition.y)
            {
                v3MaxPosition.y = v3Position.y;
            }
            if (v3Position.z > v3MaxPosition.z)
            {
                v3MaxPosition.z = v3Position.z;
            }
            if (v3Position.x < v3MinPosition.x)
            {
                v3MinPosition.x = v3Position.x;
            }
            if (v3Position.y < v3MinPosition.y)
            {
                v3MinPosition.y = v3Position.y;
            }
            if (v3Position.z < v3MinPosition.z)
            {
                v3MinPosition.z = v3Position.z;
            }
            m_frustumAABB.v3Max = v3MaxPosition;
            m_frustumAABB.v3Min = v3MinPosition;
        }
    }

    //
    //依赖于场景计算的AABB
    //
    void UpdateObjectUnitAABB()
    {
        Vector3 v3MaxObjPosition = MIN_OBJ_POSITION;
        Vector3 v3MinObjPosition = MAX_OBJ_POSITION;
        for (int i = 0; i < m_listObjectAABB.Count; i++)
        {
            ObjectAABB aabb = m_listObjectAABB[i];
            if (aabb.v3Max.x > v3MaxObjPosition.x)
            {
                v3MaxObjPosition.x = aabb.v3Max.x;
            }
            if (aabb.v3Max.y > v3MaxObjPosition.y)
            {
                v3MaxObjPosition.y = aabb.v3Max.y;
            }
            if (aabb.v3Max.z > v3MaxObjPosition.z)
            {
                v3MaxObjPosition.z = aabb.v3Max.z;
            }

            if (aabb.v3Min.x < v3MinObjPosition.x)
            {
                v3MinObjPosition.x = aabb.v3Min.x;
            }
            if (aabb.v3Min.y < v3MinObjPosition.y)
            {
                v3MinObjPosition.y = aabb.v3Min.y;
            }
            if (aabb.v3Min.z < v3MinObjPosition.z)
            {
                v3MinObjPosition.z = aabb.v3Min.z;
            }
        }
        m_listObjectAABB.Clear();
        m_frustumAABB.v3Max = v3MaxObjPosition;
        m_frustumAABB.v3Min = v3MinObjPosition;
    }

    void UpdateCropMatrix()
    {
		EffectHelp.S.BuildOrthogonalProjectMatrix(ref m_frustumAABB.cropMatrix, m_frustumAABB.v3Max, m_frustumAABB.v3Min);
    }
    public void MergeObjectAABB(Vector3 max, Vector3 min)
    {
        vertices[0].x = min.x;  vertices[0].y = min.y; vertices[0].z = min.z;
        vertices[1].x = max.x; vertices[1].y = min.y; vertices[1].z = min.z;
        vertices[2].x = min.x;  vertices[2].y = min.y; vertices[2].z = max.z;
        vertices[3].x = max.x; vertices[3].y = min.y; vertices[3].z = max.z;

        vertices[4].x = min.x;  vertices[4].y = max.y; vertices[4].z = min.z;
        vertices[5].x = max.x; vertices[5].y = max.y; vertices[5].z = min.z;
        vertices[6].x = min.x;  vertices[6].y = max.y; vertices[6].z = max.z;
        vertices[7].x = max.x; vertices[7].y = max.y; vertices[7].z = max.z;

        //计算在MainCamera视图空间Object的AABB
        Vector3 v3MaxPosition = MIN_OBJ_POSITION;
        Vector3 v3MinPosition = MAX_OBJ_POSITION;

        for (int vertId = 0; vertId < 8; ++vertId)
        {
            // Light view space
            Vector3 v3Position = m_matLightViewMatrix.MultiplyPoint3x4(vertices[vertId]);
            if (v3Position.x > v3MaxPosition.x)
            {
                v3MaxPosition.x = v3Position.x;
            }
            if (v3Position.y > v3MaxPosition.y)
            {
                v3MaxPosition.y = v3Position.y;
            }
            if (v3Position.z > v3MaxPosition.z)
            {
                v3MaxPosition.z = v3Position.z;
            }
            if (v3Position.x < v3MinPosition.x)
            {
                v3MinPosition.x = v3Position.x;
            }
            if (v3Position.y < v3MinPosition.y)
            {
                v3MinPosition.y = v3Position.y;
            }
            if (v3Position.z < v3MinPosition.z)
            {
                v3MinPosition.z = v3Position.z;
            }
        }
        ObjectAABB aabb;
        aabb.v3Min = v3MinPosition;
        aabb.v3Max = v3MaxPosition;
        m_listObjectAABB.Add (aabb);
    }
}

