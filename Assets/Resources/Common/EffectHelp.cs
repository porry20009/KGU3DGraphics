using UnityEngine;
using System.IO;
public class EffectHelp
{
    public enum CameraDepth
    {
        CSM = 1,
        SpringForce = 2,
        WaterRefract = 3,
        WaterReflect = 4,
        WaterDepth = 5,
        Mirage = 6, //分身
        Others = 7
    };
    public enum PlaneSpace
    {
        XZ = 1,
        XY = 2,
        YZ = 3,
    };

    public enum UnityVersion
    {
        UnityAbove5_0, //unity >= 5.0
        UnityBelow5_0   //unity < 5.0
    }

    static EffectHelp m_sEffectHelp = null;

    public float MAX_VALUE = 1e+12f;
    public float MIN_VALUE = -1e+12f;

    public static EffectHelp S
    {
        get
        {
            if (m_sEffectHelp == null)
                m_sEffectHelp = new EffectHelp();
            return m_sEffectHelp;
        }
    }

    public Camera CreateRenderCamera(Transform parent,Camera copyCamara,string name,CameraDepth depth)
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
        //camera.hideFlags = HideFlags.HideAndDontSave;
        camera.useOcclusionCulling = false;
        camera.renderingPath = RenderingPath.Forward;
        camera.depth = -1 - (int)depth;
        return camera;
    }

    public Material CreateNewMaterial(Shader shader)
    {
        Material newMat = new Material(shader);
        newMat.name = string.Concat("[New]", newMat.name);
        return newMat;
    }

    //创建正交投影矩阵
    public void BuildOrthogonalProjectMatrix(ref Matrix4x4 projectMatrix,Vector3 v3MaxInViewSpace, Vector3 v3MinInViewSpace)
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

    public void CreatePlane(GameObject empty,float w,float h)
    {
        empty.transform.localScale = new Vector3(w,h,1);
        Mesh mesh = new Mesh();
        // 0    1
        // 3    2
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-1.0f, 0.0f,1.0f),
            new Vector3(1.0f, 0.0f,1.0f),
            new Vector3(1.0f, 0.0f,-1.0f),
            new Vector3(-1.0f, 0.0f,-1.0f),
        };

        Vector3[] normal = new Vector3[4]
        {
            new Vector3(0.0f, 1.0f,0.0f),
            new Vector3(0.0f, 1.0f,0.0f),
            new Vector3(0.0f, 1.0f,0.0f),
            new Vector3(0.0f, 1.0f,0.0f),
        };

        // UV的原点设为左下角
        Vector2[] uv= new Vector2[4]
        {
            new Vector2(0, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0),
            new Vector2(0, 0),
        };

          mesh.vertices = vertices;
          mesh.normals = normal;
          mesh.uv = uv;
          int[] indices = { 0, 1, 2, 0, 2, 3 };
          mesh.triangles = indices;
          MeshFilter filterFloor = empty.AddComponent<MeshFilter>();
          filterFloor.mesh = mesh;
          empty.AddComponent<MeshRenderer>();
    }
    public void SetNPCRendererLayer(GameObject npc, int layer)
    {
        Renderer[] renderers = npc.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rd in renderers)
        {
            string shadername = rd.material.shader.name;
            if (shadername.Contains("VertexLit") || shadername.Contains("Character"))
            {
                rd.gameObject.layer = layer;
            }
        }
    }
    public void CheckedNPCMaterial(GameObject npc)
    {
        Renderer[] renderers = npc.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rd in renderers)
        {
            string shadername = rd.material.shader.name;
            if (shadername.Equals("YueChuan/Character/CharacterHigh"))
            {
                Material newMaterial = new Material(Shader.Find("YueChuan/Character/CharacterLow"));
                newMaterial.CopyPropertiesFromMaterial(rd.material);
                rd.material = newMaterial;
            }
            else if (shadername.Equals("YueChuan/Character/CharacterHighAlphaTest"))
            {
                Material newMaterial = new Material(Shader.Find("YueChuan/Character/CharacterLowAlphaTest"));
                newMaterial.CopyPropertiesFromMaterial(rd.material);
                rd.material = newMaterial;
            }
        }
    }

    //RenderTexture to png
    public bool SaveRenderTextureToPNG(Texture inputTex,Shader outputShader, string contents, string pngName)
    {
        RenderTexture temp = RenderTexture.GetTemporary(inputTex.width, inputTex.height, 0, RenderTextureFormat.ARGB32);
        Material mat = new Material(outputShader);
        Graphics.Blit(inputTex, temp, mat);
        bool ret = SaveRenderTextureToPNG(temp, contents,pngName);
        RenderTexture.ReleaseTemporary(temp);

        return ret;
    }

    public bool SaveRenderTextureToPNG(RenderTexture rt,string contents, string pngName)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        byte[] bytes = png.EncodeToPNG();

        if (!Directory.Exists(contents))
        {
            Directory.CreateDirectory(contents);
        }
        FileStream file = File.Open(contents + "/" + pngName + ".png", FileMode.Create);
        Debug.Log("生成png:" + contents + "/" + pngName + ".png");

        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();

        Texture2D.DestroyImmediate(png);
        png = null;

        RenderTexture.active = prev;

        return true;
    }

    /* -------------------------->x
       | 0--1--2--3--4--5--6---
       | 1--
       | 2--
       | 3--
       | 4--
       y 
    */
    public void CreateGridPlane(GameObject empty,Material mat, int rowCount, int colCount, float w, float h,Vector3 offsetVector)
    {
        Mesh mesh = new Mesh();

        Vector2 cellSize = new Vector2(w / (colCount - 1),h / (rowCount - 1));
        Vector3[] vertices = new Vector3[rowCount * colCount];
        Vector2[] uvs = new Vector2[rowCount * colCount];
        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                vertices[r * colCount + c].x = cellSize.x * c + offsetVector.x;
                vertices[r * colCount + c].y = 0 + offsetVector.y;
                vertices[r * colCount + c].z = cellSize.y * r + offsetVector.z;

                uvs[r * colCount + c].x = (float)c / (float)(colCount - 1);
                uvs[r * colCount + c].y = (float)r / (float)(rowCount - 1);

            }
        }

        int[] indices = new int[(rowCount - 1) * (colCount - 1) * 6];
        int n = 0;
        for (int r = 0; r < rowCount - 1 ; r++)
        {
            for (int c = 0; c < colCount - 1; c++)
            {
                indices[n] = r * colCount + c;
                indices[n + 1] = (r + 1) * colCount + c;
                indices[n + 2] = r * colCount + c + 1;

                indices[n + 3] = r * colCount + c + 1;
                indices[n + 4] = (r + 1) * colCount + c;
                indices[n + 5] = (r + 1) * colCount + c + 1;

                n += 6;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = indices;
        MeshFilter filter = empty.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshRenderer rd = empty.AddComponent<MeshRenderer>();
        rd.material = mat;
    }

    public Vector2 PosInWorldSpaceToScreenUV(Camera camera,Vector3 posInWorldSpace)
    {
        Matrix4x4 matVP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
        Vector3 v3PosInProjSpace = matVP.MultiplyPoint(posInWorldSpace);
        Vector2 v2UV = new Vector2(0.5f * v3PosInProjSpace.x + 0.5f, 0.5f * v3PosInProjSpace.y + 0.5f);

        return v2UV;
    }


    //线性插值
    public float LinearInterpolate(float a, float b, float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        return a * (1 - t) + b * t;
    }
    public Vector2 LinearInterpolate(Vector2 a, Vector2 b, float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        return a * (1 - t) + b * t;
    }

    //一个周期正弦插值(a -> b -> a)
    public float CycSinInterpolate(float a,float b,float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        float ft = t * Mathf.PI;
        float f = Mathf.Sin(ft);
        return a * (1 - f) + b * f;
    }

    //余弦插值
    public float CosInterpolate(float a, float b, float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        float ft = t * Mathf.PI;
        float f = (1 - Mathf.Cos(ft)) * 0.5f;
        return a * (1 - f) + b * f;
    }
    //三次样条曲线插值
    public float ThirdSplineInterpolate(float a, float b, float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        float f = t * t * (-2 * t + 3);
        return a * (1 - f) + b * f;
    }
    //五次样条线插值
    public float FifthSplineInterpolate(float a, float b, float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        float f = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
        return a * (1 - f) + b * f;
    }
    public Vector2 FifthSplineInterpolate(Vector2 a, Vector2 b, float t)
    {
        if (t < 0)
            return a;
        else if (t > 1.0f)
            return b;
        float f = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
        return a * (1 - f) + b * f;
    }

    public void BlitUseSrcDepthBuffer(RenderTexture src, RenderBuffer destColor,RenderBuffer depthBuffer, Material mat, int pass)
    {
        mat.SetTexture("_MainTex", src);
        Graphics.SetRenderTarget(destColor, depthBuffer);
        GL.PushMatrix();
        GL.LoadOrtho();

        mat.SetPass(pass);
        GL.Begin(GL.TRIANGLES);

        GL.Clear(false, true, Color.black);

        GL.Vertex3(0.0f, 0.0f, 0);
        GL.Vertex3(0.0f, 1.0f, 0);
        GL.Vertex3(1.0f, 0.0f, 0);
        //----------------------------------
        GL.Vertex3(1.0f, 0.0f, 0);
        GL.Vertex3(0.0f, 1.0f, 0);
        GL.Vertex3(1.0f, 1.0f, 0);

        GL.End();
        GL.PopMatrix();
        Graphics.SetRenderTarget(null);
    }

    //高斯分布函数
    public float GaussianDistribution(float u,float sigma,float x)
    {
        return 1.0f / (Mathf.Sqrt(2.0f * Mathf.PI) * sigma) * Mathf.Exp((-(x - u) * (x - u)) / (2.0f * sigma * sigma));
    }
    float frac(float t)
    {
        int i = (int)t;
        return t - i;
    }
    Vector4 frac(Vector4 t)
    {
        Vector4 ret = Vector4.zero;
        ret.x = frac(t.x);
        ret.y = frac(t.y);
        ret.z = frac(t.z);
        ret.w = frac(t.w);
        return ret;
    }

    Vector2 frac(Vector2 t)
    {
        Vector2 ret = Vector2.zero;
        ret.x = frac(t.x);
        ret.y = frac(t.y);
        return ret;
    }

    public float DecodeFRGBA(Vector4 enc)
    {
        Vector4 kDecodeDot = new Vector4(1.0f, 1.0f / 255.0f, 1.0f / 65025.0f, 1.0f / 16581375.0f);
        return Vector4.Dot(enc, kDecodeDot);
    }

    //将一个浮点数(0,1]编码到RGBA32bit中
    public Vector4 EncodeFToRGBA(float v)
    {
        Vector4 kEncodeMul = new Vector4(1.0f, 255.0f, 65025.0f, 16581375.0f);
        float kEncodeBit = 1.0f / 255.0f;
        Vector4 enc = kEncodeMul * v;
        enc = frac(enc);
        enc.x -= enc.y * kEncodeBit;
        enc.y -= enc.z * kEncodeBit;
        enc.z -= enc.w * kEncodeBit;
        enc.w -= enc.w * kEncodeBit;
        return enc;
    }

    public Vector2 EncodeFloatRG(float v)
    {
        Vector2 kEncodeMul = new Vector2(1.0f, 255.0f);
        float kEncodeBit = 1.0f / 255.0f;
        Vector2 enc = kEncodeMul * v;
        enc = frac(enc);
        enc.x -= enc.y * kEncodeBit;
        return enc;
    }
    public float DecodeFloatRG(Vector2 enc)
    {
        Vector2 kDecodeDot = new Vector2(1.0f, 1 / 255.0f);
        return Vector2.Dot(enc, kDecodeDot);
    }

    public void SaveRelease<T>(ref T tex) where T : Object
    {
        if (tex != null)
        {
            Object.Destroy(tex);
            tex = null;
        }
    }

    public float SquareDistance(Vector2 a,Vector2 b)
    {
        Vector2 vec = a - b;
        return Vector2.Dot(vec, vec);
    }

    public float SquareDistance(Vector3 a, Vector3 b)
    {
        Vector3 vec = a - b;
        return Vector3.Dot(vec, vec);
    }

    public float Length(Vector3 a)
    {
        return  Mathf.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
    }

    // Given position/normal of the plane, calculates plane in camera space.
    public Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float planeOffset, float sideSign)
    {
        Vector3 offsetPos = pos + normal * planeOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    // Calculates reflection matrix around the given plane
    public void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}
