using UnityEngine;
using System.Collections;

//挂到空的GameObject上
public class PerlinNoiseWater : MonoBehaviour
{
    public Camera m_mainCamera = null;
    public int m_rowCount = 128;
    public int m_colCount = 128;
    public float m_width = 10.0f;
    public float m_height = 10.0f;

    public Texture m_staticReflectTex = null;
    public int m_noiseWidthDim = 128;
    public int m_noiseHeightDim = 128;
    public int m_octaves = 4;
    public float m_persistence = 0.5f;
    public float m_lacunarity = 2.0f;
    public int m_frequency = 20;
    public float m_amplitude = 0.6f;
    public float m_speed = 1.0f;
    public Vector2 m_noiseTile = new Vector2(1.0f, 1.0f);

    public LayerMask m_canRefractLayer;
    public LayerMask m_waveParticleLayer;
    public float m_distortValue = 1.0f;
    public float m_heightScale = 1.0f;
    public float m_waterPlaneOffset = -0.2f;
    public Transform m_sun = null;
    public Vector3 m_sunDir = Vector3.one;
    [Range(0, 300)]
    public float m_sunspot = 100.0f;
    public Color m_sunColor = new Color32(255, 255, 255, 255);
    public Color m_waterColor = new Color32(24, 91, 98, 255);

    Camera m_refractCamera = null;
    Material m_noiseMaterial = null;
    Material m_heightToNormalMaterial = null;
    Material m_renderMaterial = null;
    public RenderTexture m_noise = null;
    public RenderTexture m_normalmap = null;
    public RenderTexture m_refractmap = null;
    void Awake ()
    {
        CreateMaterial();
        CreateProcessmap();
        CreateRefractCamera();
        EffectHelp.S.CreateGridPlane(gameObject, m_renderMaterial, m_rowCount, m_colCount, m_width, m_height,new Vector3(-m_width * 0.5f,0.0f,-m_height * 0.5f));
    }
    void OnWillRenderObject()
    {
        if (Camera.current.name.Equals(m_mainCamera.name))
        {
            UpdatePerlinNoise();
            UpdateNormalmap();
            UpdateRenderWater();
        }
    }

    void OnDestroy()
    {
        EffectHelp.S.SaveRelease<RenderTexture>(ref m_noise);
        EffectHelp.S.SaveRelease<RenderTexture>(ref m_normalmap);
        EffectHelp.S.SaveRelease<RenderTexture>(ref m_refractmap);
        EffectHelp.S.SaveRelease<Camera>(ref m_refractCamera);
    }

    void CreateMaterial()
    {
        m_noiseMaterial = new Material(Shader.Find("PerlinNoise/Gradient3D"));
        m_heightToNormalMaterial = new Material(Shader.Find("PerlinNoiseWater/HeightToNormal"));
        m_renderMaterial = new Material(Shader.Find("PerlinNoiseWater/PerlinNoiseWater"));
    }

    void CreateProcessmap()
    {
        if (m_noise == null)
        {
            m_noise = new RenderTexture(m_noiseWidthDim, m_noiseHeightDim, 0, RenderTextureFormat.ARGB32);
            m_noise.wrapMode = TextureWrapMode.Repeat;
            m_noise.name = "water surface noise";
        }

        if (m_normalmap == null)
        {
            m_normalmap = new RenderTexture(m_noiseWidthDim, m_noiseHeightDim, 0, RenderTextureFormat.ARGB32);
            m_normalmap.wrapMode = TextureWrapMode.Repeat;
            m_normalmap.name = "water surface normal";
        }

        if (m_refractmap == null)
        {
            m_refractmap = new RenderTexture(Screen.width, Screen.width, 16, RenderTextureFormat.ARGB32);
            m_refractmap.wrapMode = TextureWrapMode.Clamp;
            m_refractmap.name = "refract texture";
        }
    }
    void CreateRefractCamera()
    {
        if (m_refractCamera == null)
        {
            m_refractCamera = EffectHelp.S.CreateRenderCamera(m_mainCamera.gameObject.transform, m_mainCamera, "RefractCamera", EffectHelp.CameraDepth.WaterRefract);
            m_refractCamera.enabled = false;
            m_refractCamera.targetTexture = m_refractmap;
            m_refractCamera.cullingMask = m_canRefractLayer;
        }
    }

    void UpdatePerlinNoise()
    {
        m_noiseMaterial.SetFloat("_Octaves", m_octaves);
        m_noiseMaterial.SetFloat("_Persistence", m_persistence);
        m_noiseMaterial.SetFloat("_Lacunarity", m_lacunarity);
        m_noiseMaterial.SetFloat("_Frequency", m_frequency);
        m_noiseMaterial.SetFloat("_Amplitude", m_amplitude);
        m_noiseMaterial.SetVector("_Seed", Vector2.zero);
        m_noiseMaterial.SetFloat("_Speed", m_speed);
        m_noise.DiscardContents();
        Graphics.Blit(null, m_noise, m_noiseMaterial);
    }

    void UpdateNormalmap()
    {
        m_heightToNormalMaterial.SetTexture("_HeightCurrentTex", m_noise);
        m_heightToNormalMaterial.SetFloat("_TexelLength_x2", 4.0f/(m_heightScale*(m_width + m_height)));
        m_heightToNormalMaterial.SetVector("_TextureSize", new Vector4(1.0f / (float)m_noiseWidthDim, 1.0f / (float)m_noiseHeightDim, 0, 0));
        m_normalmap.DiscardContents();
        Graphics.Blit(m_noise, m_normalmap, m_heightToNormalMaterial);
    }

    void UpdateRenderWater()
    {
        m_renderMaterial.SetTexture("_RefractTex", m_refractmap);
        m_renderMaterial.SetTexture("_ReflectTex", m_staticReflectTex);
        m_renderMaterial.SetTexture("_Heightmap", m_noise);
        m_renderMaterial.SetTexture("_WaterNormal", m_normalmap);

        m_renderMaterial.SetColor("_WaterColor", m_waterColor);
        m_renderMaterial.SetColor("_SunColor", m_sunColor);
        if (m_sun != null)
            m_sunDir = -m_sun.forward;
        m_renderMaterial.SetVector("_SunDir", new Vector4(m_sunDir.x, m_sunDir.y, m_sunDir.z, m_sunspot));
        m_renderMaterial.SetFloat("_DistortLevel", m_distortValue);
        m_renderMaterial.SetFloat("_HeightScale", m_heightScale);
        m_renderMaterial.SetVector("_NoiseTile", m_noiseTile);

        Vector3 pos = gameObject.transform.position;
        Vector3 normal = gameObject.transform.up;
        m_refractCamera.worldToCameraMatrix = m_mainCamera.worldToCameraMatrix;
        Vector4 waterClipPlane = CameraSpacePlane(m_refractCamera, pos, normal, m_waterPlaneOffset, -1.0f);
        Matrix4x4 matrixRefract = m_mainCamera.CalculateObliqueMatrix(waterClipPlane);
        m_refractCamera.projectionMatrix = matrixRefract;
        m_refractCamera.Render();
    }
    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float planeOffset, float sideSign)
    {
        Vector3 offsetPos = pos + normal * planeOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }
}
