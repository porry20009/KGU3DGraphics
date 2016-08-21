using UnityEngine;

//由于卷云没有光照模型，所以天空只是适合中午
class SimpleSky : MonoBehaviour
{
    public enum SkyQuality
    {
        Low,
        Medium
    }
    public bool m_isDebug = false;
    public SkyQuality m_skyQuality = SkyQuality.Low;
    //云
    [Range(0,3.14159268979f)]
    public float m_windDegrees = 0.0f;
    public float m_windSpeed = 0.05f;
    [Range(0.0f, 100.0f)]
    public float m_cloudDensity = 9.6f;
    [Range(0.0f, 1.0f)]
    public float m_cloudSharpness = 3.0f;
    [Range(0.0f, 1.0f)]
    public float m_cloudCover = 0.1f;
    public Vector2 m_cloudTile1 = new Vector2(3, 3);
    public Vector2 m_cloudTile2 = new Vector2(5, 5);
    Material m_cloudMaterial = null;
    Vector4 m_cloudUV = Vector4.zero;
    public RenderTexture m_cloudTexture1 = null;
    public RenderTexture m_cloudTexture2 = null;

    //天空、太阳
    Material m_skyMaterial = null;
    Material m_sunMaterial = null;
    Transform m_sun = null;
    public Vector3 m_offsetPosition = new Vector3(0,0,0);
    public Color m_duskSkyColor = new Color(0.3137f, 0.4118f, 0.5098f, 1.0f);      //黄昏时，天空Color
    public Color m_noonSkyColor = new Color(0.4902f, 0.6471f, 0.8431f, 1.0f);      //中午时，天空Color
    public Color m_duskHorizonColor = new Color(0.8f, 0.4f, 0.1f,1.0f);            //黄昏时，水平线Color
    public Color m_noonHorizonColor = new Color(1.0f, 1.0f, 1.0f,1.0f);            //中午时，水平线Color
    public Color m_sunColor = new Color(1, 1, 1, 1);

    public bool m_isCreateSun = true;
    [Range(0.0f, 2.0f)]
    public float m_sunSize = 1.0f;
    [Range(0.0f, 1.0f)]
    public float m_exposeScale = 0.2f;
    [Range(0.0f, 1.0f)]
    public float m_exposeOffset = 0.15f;
    [Range(0.0f, 2.0f)]
    public float m_sunHorizontalPos = 0.25f;
    [Range(-0.5f,0.5f)]
    public float m_sunVerticalPos = 0.25f;

    [Range(0.0f, 100.0f)]
    public float m_noiseFrequency = 20.0f;
    [Range(0.0f, 2.0f)]
    public float m_noiseAmplitude = 0.7f; 
    void Start()
    {
        CreateCloud();
        CreateSky();
        if (m_isCreateSun)
            CreateSun();
        UpdateGPUParameter();
    }

    void Update()
    {
        UpdateCloud();
        if (m_isDebug)
        {
            UpdateGPUParameter();
            Noise.CreateCloudTexture(ref m_cloudTexture1, 256, 256, m_noiseFrequency, m_noiseAmplitude, Vector2.zero, m_cloudSharpness,m_cloudCover);
            Noise.CreateCloudTexture(ref m_cloudTexture2, 256, 256, m_noiseFrequency, m_noiseAmplitude, Vector2.one * 100, m_cloudSharpness, m_cloudCover);
        }
        transform.position = Camera.main.gameObject.transform.position + m_offsetPosition;
    }

    void OnDestroy()
    {
        EffectHelp.S.SaveRelease<RenderTexture>(ref m_cloudTexture1);
        EffectHelp.S.SaveRelease<RenderTexture>(ref m_cloudTexture2);
    }

    void UpdateGPUParameter()
    {
        Vector3 sunPos = Vector3.zero;
        float fAlpha = Mathf.PI * m_sunHorizontalPos;
        float fBeta = Mathf.PI * m_sunVerticalPos;
        sunPos.x = Mathf.Sin(fAlpha) * Mathf.Sin(fBeta);
        sunPos.y = Mathf.Cos(fBeta);
        sunPos.z = Mathf.Cos(fAlpha) * Mathf.Sin(fBeta);

        if (m_isCreateSun)
        {
            Quaternion dir = Quaternion.FromToRotation(new Vector3(0, 0, 1), new Vector3(0, 0, 1));
            m_sun.rotation = Camera.main.gameObject.transform.rotation * dir;
            m_sun.localPosition = sunPos * 1.5f;
            m_sun.localScale = Vector3.one * m_sunSize;
            m_sunMaterial.SetColor("_SunColor", m_sunColor);
        }

        m_skyMaterial.SetVector("_SunPositon", sunPos);
        m_skyMaterial.SetColor("_SunColor", m_sunColor);
        m_skyMaterial.SetFloat("_ExposeScale", m_exposeScale);
        m_skyMaterial.SetFloat("_ExposeOffset", m_exposeOffset);
        m_skyMaterial.SetColor("_DuskHorizonColor", m_duskHorizonColor);
        m_skyMaterial.SetColor("_DuskSkyColor", m_duskSkyColor);
        m_skyMaterial.SetColor("_NoonHorizonColor", m_noonHorizonColor);
        m_skyMaterial.SetColor("_NoonSkyColor", m_noonSkyColor);

        m_cloudMaterial.SetTexture("_NoiseTexture1", m_cloudTexture1);
        m_cloudMaterial.SetTexture("_NoiseTexture2", m_cloudTexture2);
        m_cloudMaterial.SetVector("_CloudScale1", m_cloudTile1);
        m_cloudMaterial.SetVector("_CloudScale2", m_cloudTile2);
        m_cloudMaterial.SetFloat("_CloudDensity", m_cloudDensity);

    }

    void CreateSun()
    {
        m_sunMaterial = new Material(Shader.Find("Sky/SimpleSun"));
        GameObject sunQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sunQuad.name = "Sun";
        sunQuad.transform.parent = transform;
        sunQuad.transform.localPosition = Vector3.zero;
        sunQuad.transform.localRotation = Quaternion.identity;
        sunQuad.transform.localScale = Vector3.one * m_sunSize;
        sunQuad.GetComponent<Renderer>().material = m_sunMaterial;
        sunQuad.layer = gameObject.layer;
        m_sun = sunQuad.transform;
    }

    void CreateSky()
    {
        m_skyMaterial = new Material(Shader.Find("Sky/SimpleSky"));
        GameObject skyObj = new GameObject("Sky");
        skyObj.transform.parent = transform;
        skyObj.transform.localPosition = new Vector3(0, 0, 0);
        skyObj.transform.localRotation = new Quaternion(0, 0, 0, 1);
        skyObj.transform.localScale = new Vector3(2.0f,2.0f, 2.0f);
        skyObj.layer = gameObject.layer;
        CreateSkySphereMesh(skyObj, false,m_skyMaterial, 1, 20, 20);
    }
    void CreateCloud()
    {
        Noise.CreateCloudTexture(ref m_cloudTexture1, 256, 256, m_noiseFrequency, m_noiseAmplitude, Vector2.zero, m_cloudSharpness, m_cloudCover);
        Noise.CreateCloudTexture(ref m_cloudTexture2, 256, 256, m_noiseFrequency, m_noiseAmplitude, Vector2.one * 100, m_cloudSharpness, m_cloudCover);

        m_cloudMaterial = new Material(Shader.Find("Sky/SimpleCloud"));
        GameObject cloudObj = new GameObject("Cloud");
        cloudObj.transform.parent = transform;
        cloudObj.transform.localPosition = Vector3.zero;
        cloudObj.transform.localRotation = Quaternion.identity;
        cloudObj.transform.localScale = Vector3.one;
        cloudObj.layer = gameObject.layer;
        CreateSkySphereMesh(cloudObj, true, m_cloudMaterial, 1, 5, 5);
    }
    void UpdateCloud()
    {
        // Wind direction and speed calculation
        Vector2 v1 = Vector2.zero;
        Vector2 v2 = Vector2.zero;
        Vector4 wind = Vector4.zero;

        v1.x = Mathf.Cos(m_windDegrees + 0.2617994f);
        v1.y = Mathf.Sin(m_windDegrees + 0.2617994f);
        v2.x = Mathf.Cos(m_windDegrees - 0.2617994f);
        v2.y = Mathf.Sin(m_windDegrees - 0.2617994f);

        wind.x = m_windSpeed * v1.x;
        wind.y = m_windSpeed * v1.y;
        wind.z = m_windSpeed * v2.x;
        wind.w = m_windSpeed * v2.y;
        // Update cloud UV coordinates
        m_cloudUV += Time.deltaTime * wind;
        m_cloudUV = new Vector4(m_cloudUV.x % m_cloudTile1.x,
                              m_cloudUV.y % m_cloudTile1.y,
                              m_cloudUV.z % m_cloudTile2.x,
                              m_cloudUV.w % m_cloudTile2.y);
        m_cloudMaterial.SetVector("_CloudUV", m_cloudUV);
    }

    void CreateSkySphereMesh(GameObject sphereGO, bool isHalfSphere,Material material, float fSkyRadius, int nLatitudesDeta, int nLongitudesDeta)
    {
        int nNumLatitudes = 360 / nLatitudesDeta;//纬度上的顶点
        int nNumLongitudes = (isHalfSphere ? 90 : 180) / nLongitudesDeta; //经度上的顶点
        int nVertsPerLongi = nNumLatitudes + 1;
       // int nVertsPerLati = nNumLongitudes + 1;
        int nVerticesCount = (nNumLatitudes + 1) * (nNumLongitudes + 1);

        Vector3[] vertices = new Vector3[nVerticesCount];
        Vector2[] texcoord = new Vector2[nVerticesCount];

        //Vertex buffer
        int nIndex = 0;
        //转换为弧度
        float fAlpha = Mathf.PI * (float)nLatitudesDeta / 180.0f;
        float fBeta = Mathf.PI * (float)nLongitudesDeta / 180.0f;
        for (int i = 0; i < nNumLongitudes + 1; i++)//纬度线的数量
        {
            for (int j = 0; j < nNumLatitudes + 1; j++)//经度线的数量
            {
                //计算顶点坐标
                vertices[nIndex].x = fSkyRadius * Mathf.Sin(fAlpha * j) * Mathf.Sin(fBeta * i);
                vertices[nIndex].y = fSkyRadius * Mathf.Cos(fBeta * i);
                vertices[nIndex].z = fSkyRadius * Mathf.Cos(fAlpha * j) * Mathf.Sin(fBeta * i);
                //计算纹理坐标
                texcoord[nIndex].x = i * fAlpha / (2.0f * Mathf.PI);
                texcoord[nIndex].y = j * fBeta / (2.0f * Mathf.PI);
                nIndex++;
            }
        }
        //Index buffer
        int[] indices = new int[nVerticesCount * 6];
        int n = 0;
        for (int row = 0; row < nNumLongitudes; row++)
        {
            for (int col = 0; col < nNumLatitudes; col++)
            {
                indices[n + 0] = row * nVertsPerLongi + col;
                indices[n + 1] = (row + 1) * nVertsPerLongi + col;
                indices[n + 2] = (row + 1) * nVertsPerLongi + col + 1;

                indices[n + 3] = (row + 1) * nVertsPerLongi + col + 1;
                indices[n + 4] = row * nVertsPerLongi + col + 1;
                indices[n + 5] = row * nVertsPerLongi + col;
                n += 6;
            }
        }
        MeshRenderer meshRender;
        MeshFilter meshFilter;
        meshRender = sphereGO.AddComponent<MeshRenderer>();
        meshFilter = sphereGO.AddComponent<MeshFilter>();
        meshRender.material = material;
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.uv = texcoord;
        meshFilter.mesh.triangles = indices;
    }
}
