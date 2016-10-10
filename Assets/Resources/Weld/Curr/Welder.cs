
using UnityEngine;
using System.Collections;

public class Welder : MonoBehaviour
{
    enum ProcessCSBuffer
    {
        Position = 0,
        Normal = 1,
        VerticesIndex = 2,
        VerticesAdjFaces = 3,
    };

    enum Processmap
    {
        WeldDisplace = 0,
    };

    enum CSComputeKernel
    {
        CSSumDisplacement = 0,
        CSCalculateNormal = 1,
    };

    struct TriangleIndices
    {
        public int ID0;
        public int ID1;
        public int ID2;
    };
    struct TriangleAdjFaces
    {
        public TriangleIndices Face0;
        public TriangleIndices Face1;
        public TriangleIndices Face2;
        public TriangleIndices Face3;
        public TriangleIndices Face4;
        public TriangleIndices Face5;
    };

    public Material mWelderMaterial = null;
    public ComputeShader mComputeShader = null;
    public Camera mMainCamera = null;
    public int mRowCount = 128;
    public int mColCount = 128;
    public float mHeight = 10;
    public float mWidth = 10;
    public float mDisplacementHeight = 0.01f;
    public float mRange = 30.0f;
    public Texture mDistributionTex = null;
    
    Material mDrawDisplacementMaterial = null;
    ComputeBuffer []mProcessCSBuffer = {null,null,null,null};
    RenderTexture[] mProcessmap = { null };
    HeatFlux mHeatFlux = null;
    Fluid mFluid = null;

    void Awake()
    {
        CreateMaterials();
        EffectHelp.S.CreateGridPlane(gameObject, mWelderMaterial, mRowCount, mColCount, mWidth, mHeight, new Vector3(-mWidth * 0.5f, 0.0f, -mHeight * 0.5f));
        CreateProcessmap();
        CreateProcessCSBuffer();
    }

    void Start()
    {
        gameObject.AddComponent<MeshCollider>();
        mHeatFlux = gameObject.AddComponent<HeatFlux>();
        mHeatFlux.mTexWidth = mColCount * 4;
        mHeatFlux.mTexHeight = mRowCount * 4;
        mHeatFlux.mDistributionTex = mDistributionTex;

        mFluid = gameObject.AddComponent<Fluid>();
        mFluid.mTexWidth = mColCount * 4;
        mFluid.mTexHeight = mRowCount * 4;
        mFluid.mWaveSpeed = 0.04f;
        mFluid.mCoeffViscosity = 12.0f;
    }

    void OnDestroy()
    {
        for (int i = 0; i < mProcessmap.Length; i++)
        {
            EffectHelp.S.SaveRelease<RenderTexture>(ref mProcessmap[i]);
        }
        for (int i = 0; i < mProcessCSBuffer.Length; i++ )
        {
            if (mProcessCSBuffer[i] != null)
            {
                mProcessCSBuffer[i].Release();
                mProcessCSBuffer[i] = null;
            }
        }
    }

    void OnWillRenderObject()
    {
        if (mMainCamera == null)
            mMainCamera = Camera.main;
        if (mMainCamera == null)
            return;
        if (Camera.current.name.Equals(mMainCamera.name))
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = mMainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.collider.name.Equals(gameObject.name))
                {
                    if (mHeatFlux != null)
                    {
                        mHeatFlux.mEffectivePower = 10.0f;
                        mHeatFlux.mHeatSourcePosition = hit.point;
                    }
                    if (mFluid != null)
                        mFluid.mHeatSourceHitUV = hit.textureCoord;

                    RenderDisplacement(hit.textureCoord, mRange, mDisplacementHeight);
                    SumDisplacement();
                    CalculateNormal();
                }
            }
            else
            {
                RenderDisplacement(Vector2.zero, 30.0f, 0.0f);
                if (mHeatFlux != null)
                {
                    mHeatFlux.mEffectivePower = 0.0f;
                }
                if (mFluid != null)
                    mFluid.mHeatSourceHitUV = new Vector2(-1,-1);
            }
            SetWelderMaterialParameter();
        }
    }

    void CreateMaterials()
    {
        mDrawDisplacementMaterial = new Material(Shader.Find("Weld/WelderDisplacement"));
        //mWelderMaterial = new Material(Shader.Find("Weld/Welder"));
    }

    void CreateProcessmap()
    {
        mProcessmap[(int)Processmap.WeldDisplace] = new RenderTexture(mRowCount, mColCount, 0, RenderTextureFormat.ARGBFloat);
        mProcessmap[(int)Processmap.WeldDisplace].wrapMode = TextureWrapMode.Clamp;
        mProcessmap[(int)Processmap.WeldDisplace].filterMode = FilterMode.Bilinear;
        mProcessmap[(int)Processmap.WeldDisplace].name = "Curr Displacementmap";

        for (int i = 0; i < mProcessmap.Length; i++)
        {
            RenderTexture mainRTT = RenderTexture.active;
            RenderTexture.active = mProcessmap[i];
            GL.Clear(false, true, new Color(0, 0, 0, 1));
            RenderTexture.active = mainRTT;
        }

    }

    void CreateProcessCSBuffer()
    {
        int nVerticeCount = mRowCount * mColCount;
        mProcessCSBuffer[(int)ProcessCSBuffer.Position] = new ComputeBuffer(nVerticeCount, sizeof(float) * 4, ComputeBufferType.Counter);//RWStructuredBuffer
        mProcessCSBuffer[(int)ProcessCSBuffer.Normal] = new ComputeBuffer(nVerticeCount, sizeof(float) * 3, ComputeBufferType.Counter);//RWStructuredBuffer
        mProcessCSBuffer[(int)ProcessCSBuffer.VerticesIndex] = new ComputeBuffer(nVerticeCount, sizeof(int), ComputeBufferType.Default);//StructuredBuffer
        mProcessCSBuffer[(int)ProcessCSBuffer.VerticesAdjFaces] = new ComputeBuffer(nVerticeCount, sizeof(int) * 18, ComputeBufferType.Default);//StructuredBuffer

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Renderer rd = gameObject.GetComponent<Renderer>();
        if (meshFilter != null && rd != null)
        {
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            int    [] verticesIDForCS = new int[mesh.vertexCount];
            Vector2[] verticesIDForVS = new Vector2[mesh.vertexCount];
            Vector4[] verticesInitData = new Vector4[mesh.vertexCount];
            Vector3[] normalsInitData = new Vector3[mesh.vertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                //为每个顶点做标记
                verticesIDForCS[i] = i;
                verticesIDForVS[i] = EffectHelp.S.EncodeFloatRG((float)i / (float)(mesh.vertexCount - 1));
                //顶点世界坐标
                Vector3 pos = rd.localToWorldMatrix.MultiplyPoint3x4(vertices[i]);
                verticesInitData[i] = new Vector4(pos.x, pos.y, pos.z,0.0f);
                //顶点法线
                normalsInitData[i] = new Vector3(0,1,0);
            }
            mesh.uv2 = verticesIDForVS;
            mProcessCSBuffer[(int)ProcessCSBuffer.VerticesIndex].SetData(verticesIDForCS);
            mProcessCSBuffer[(int)ProcessCSBuffer.Position].SetData(verticesInitData);
            mProcessCSBuffer[(int)ProcessCSBuffer.Normal].SetData(normalsInitData);

            int[] indices = mesh.triangles;
            TriangleAdjFaces[] triangleAdjFaces = new TriangleAdjFaces[mesh.vertexCount];
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                TriangleAdjFaces adjFaces = new TriangleAdjFaces();
                adjFaces.Face0.ID0 = -1;
                adjFaces.Face1.ID0 = -1;
                adjFaces.Face2.ID0 = -1;
                adjFaces.Face3.ID0 = -1;
                adjFaces.Face4.ID0 = -1;
                adjFaces.Face5.ID0 = -1;
                int count = 0;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    if (count > 6)
                    {
                        break;
                    }
                    if (j == indices[i] || j == indices[i + 1] || j == indices[i + 2])
                    {
                        if (count == 0)
                        {
                            adjFaces.Face0.ID0 = indices[i];
                            adjFaces.Face0.ID1 = indices[i + 1];
                            adjFaces.Face0.ID2 = indices[i + 2];
                        }
                        else if (count == 1)
                        {
                            adjFaces.Face1.ID0 = indices[i];
                            adjFaces.Face1.ID1 = indices[i + 1];
                            adjFaces.Face1.ID2 = indices[i + 2];
                        }
                        else if (count == 2)
                        {
                            adjFaces.Face2.ID0 = indices[i];
                            adjFaces.Face2.ID1 = indices[i + 1];
                            adjFaces.Face2.ID2 = indices[i + 2];
                        }
                        else if (count == 3)
                        {
                            adjFaces.Face3.ID0 = indices[i];
                            adjFaces.Face3.ID1 = indices[i + 1];
                            adjFaces.Face3.ID2 = indices[i + 2];
                        }
                        else if (count == 4)
                        {
                            adjFaces.Face4.ID0 = indices[i];
                            adjFaces.Face4.ID1 = indices[i + 1];
                            adjFaces.Face4.ID2 = indices[i + 2];
                        }
                        else if (count == 5)
                        {
                            adjFaces.Face5.ID0 = indices[i];
                            adjFaces.Face5.ID1 = indices[i + 1];
                            adjFaces.Face5.ID2 = indices[i + 2];
                        }
                        count++;
                    }
                }
                triangleAdjFaces[j] = adjFaces;
            }
            mProcessCSBuffer[(int)ProcessCSBuffer.VerticesAdjFaces].SetData(triangleAdjFaces);

            //TriangleAdjFaces[] data = new TriangleAdjFaces[mesh.vertexCount];
            //mProcessCSBuffer[(int)ProcessCSBuffer.VerticesAdjFaces].GetData(data);
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Debug.Log("顶点" + i.ToString());
            //    Debug.Log("face0:" + data[i].Face0.ID0.ToString() + "," + data[i].Face0.ID1.ToString() + "," + data[i].Face0.ID2.ToString());
            //    Debug.Log("face1:" + data[i].Face1.ID0.ToString() + "," + data[i].Face1.ID1.ToString() + "," + data[i].Face1.ID2.ToString());
            //    Debug.Log("face2:" + data[i].Face2.ID0.ToString() + "," + data[i].Face2.ID1.ToString() + "," + data[i].Face2.ID2.ToString());
            //    Debug.Log("face3:" + data[i].Face3.ID0.ToString() + "," + data[i].Face3.ID1.ToString() + "," + data[i].Face3.ID2.ToString());
            //    Debug.Log("face4:" + data[i].Face4.ID0.ToString() + "," + data[i].Face4.ID1.ToString() + "," + data[i].Face4.ID2.ToString());
            //    Debug.Log("face5:" + data[i].Face5.ID0.ToString() + "," + data[i].Face5.ID1.ToString() + "," + data[i].Face5.ID2.ToString());
            //}
        }

    }

    void RenderDisplacement(Vector2 drawCenter,float range,float height)
    {
        mDrawDisplacementMaterial.SetVector("_DrawCenter", drawCenter);
        mDrawDisplacementMaterial.SetFloat("_Height", height);
        mDrawDisplacementMaterial.SetFloat("_Range", range);
        mDrawDisplacementMaterial.SetFloat("_WHRatio", (float)mColCount/(float)mRowCount);

        mProcessmap[(int)Processmap.WeldDisplace].DiscardContents();
        Graphics.Blit(null, mProcessmap[(int)Processmap.WeldDisplace], mDrawDisplacementMaterial);
    }

    void SumDisplacement()
    {
        mComputeShader.SetBuffer((int)CSComputeKernel.CSSumDisplacement, "gVerticesIndex", mProcessCSBuffer[(int)ProcessCSBuffer.VerticesIndex]);
        mComputeShader.SetBuffer((int)CSComputeKernel.CSSumDisplacement, "gPosition", mProcessCSBuffer[(int)ProcessCSBuffer.Position]);
        mComputeShader.SetBuffer((int)CSComputeKernel.CSSumDisplacement, "gNormals", mProcessCSBuffer[(int)ProcessCSBuffer.Normal]);
        mComputeShader.SetTexture((int)CSComputeKernel.CSSumDisplacement, "txDeltaPositionTex", mProcessmap[(int)Processmap.WeldDisplace]);

        int[] dispatch = { mColCount, mRowCount, 1 };
        mComputeShader.SetInts("gDispatch", dispatch);
        mComputeShader.Dispatch((int)CSComputeKernel.CSSumDisplacement, dispatch[0], dispatch[1], dispatch[2]);

        //Vector4[] data = new Vector4[mRowCount * mColCount];
        //mProcessCSBuffer[(int)ProcessCSBuffer.Position].GetData(data);
        //for (int i = 0; i < data.Length; i++)
        //{
        //    Debug.Log(data[i].x.ToString() + "," + data[i].y.ToString() + "," + data[i].z.ToString());
        //}
    }

    void CalculateNormal()
    {
        mComputeShader.SetBuffer((int)CSComputeKernel.CSCalculateNormal, "gVerticesIndex", mProcessCSBuffer[(int)ProcessCSBuffer.VerticesIndex]);
        mComputeShader.SetBuffer((int)CSComputeKernel.CSCalculateNormal, "gPosition", mProcessCSBuffer[(int)ProcessCSBuffer.Position]);
        mComputeShader.SetBuffer((int)CSComputeKernel.CSCalculateNormal, "gNormals", mProcessCSBuffer[(int)ProcessCSBuffer.Normal]);
        mComputeShader.SetBuffer((int)CSComputeKernel.CSCalculateNormal, "gVerticesAdjFaces", mProcessCSBuffer[(int)ProcessCSBuffer.VerticesAdjFaces]);

        int[] dispatch = { mColCount,mRowCount, 1 };
        mComputeShader.SetInts("gDispatch", dispatch);
        mComputeShader.Dispatch((int)CSComputeKernel.CSCalculateNormal, dispatch[0], dispatch[1], dispatch[2]);

        //Vector3[] data = new Vector3[mRowCount * mColCount];
        //mProcessCSBuffer[(int)ProcessCSBuffer.Normal].GetData(data);
        //for (int i = 0; i < data.Length; i++)
        //{
        //    Debug.Log(data[i].x.ToString() + "," + data[i].y.ToString() + "," + data[i].z.ToString());
        //}
    }

    void SetWelderMaterialParameter()
    {
        mWelderMaterial.SetBuffer("gPoints", mProcessCSBuffer[(int)ProcessCSBuffer.Position]);
        mWelderMaterial.SetBuffer("gNormals", mProcessCSBuffer[(int)ProcessCSBuffer.Normal]);
        mWelderMaterial.SetInt("_VerticesCount", mRowCount * mColCount);
        if (mHeatFlux !=null)
            mWelderMaterial.SetTexture("_HeatFluxMap", mHeatFlux.mProcessmaps[(int)HeatFlux.Processmap.CurrHeatFlux]);
        if (mFluid !=null)
            mWelderMaterial.SetTexture("_FluidNormalMap", mFluid.mProcessmaps[(int)Fluid.Processmap.Normal]);
        
    }
}
