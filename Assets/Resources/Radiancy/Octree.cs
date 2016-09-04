using UnityEngine;
using System.Collections;


namespace Radiancy
{
    public class Patch
    {
        public Vector3 mBoxMax = Vector3.zero;
        public Vector3 mBoxMin = Vector3.zero;

        public Vector3 mPosition = Vector3.zero;
        public Vector3 mNormal = Vector3.zero;
        public Vector4 mEmission = Vector4.zero;
        public float mAlbedo = 0.0f;

        public void Show()
        {
            Vector3 min = mBoxMin;
            Vector3 max = mBoxMax;

            Vector3[] vertices = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            vertices[0].x = min.x;  vertices[0].y = min.y; vertices[0].z = min.z;
            vertices[1].x = max.x; vertices[1].y = min.y; vertices[1].z = min.z;
            vertices[2].x = max.x; vertices[2].y = max.y; vertices[2].z = min.z;
            vertices[3].x = min.x;  vertices[3].y = max.y; vertices[3].z = min.z;
        
            vertices[4].x = min.x;  vertices[4].y = min.y; vertices[4].z = max.z;
            vertices[5].x = max.x; vertices[5].y = min.y; vertices[5].z = max.z;
            vertices[6].x = max.x; vertices[6].y = max.y; vertices[6].z = max.z;
            vertices[7].x = min.x;  vertices[7].y = max.y; vertices[7].z = max.z;

            GameObject root = new GameObject("root");
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject line0 = new GameObject("line0");
            line0.transform.localPosition = Vector3.zero;
            line0.transform.localRotation = Quaternion.identity;
            line0.transform.localScale = Vector3.one;
            line0.transform.parent = root.transform;
            KGLine kgline = line0.AddComponent<KGLine>();
            kgline.isUpdate = false;
            kgline.m_lineVerticesPosition.Add(vertices[0]);
            kgline.m_lineVerticesPosition.Add(vertices[1]);
            kgline.m_lineVerticesPosition.Add(vertices[2]);
            kgline.m_lineVerticesPosition.Add(vertices[3]);
            kgline.m_lineVerticesPosition.Add(vertices[0]);

            GameObject line1= new GameObject("line1");
            line1.transform.localPosition = Vector3.zero;
            line1.transform.localRotation = Quaternion.identity;
            line1.transform.localScale = Vector3.one;
            line1.transform.parent = root.transform;
            kgline = line1.AddComponent<KGLine>();
            kgline.isUpdate = false;
            kgline.m_lineVerticesPosition.Add(vertices[1]);
            kgline.m_lineVerticesPosition.Add(vertices[2]);
            kgline.m_lineVerticesPosition.Add(vertices[6]);
            kgline.m_lineVerticesPosition.Add(vertices[5]);
            kgline.m_lineVerticesPosition.Add(vertices[1]);

            GameObject line2 = new GameObject("line2");
            line2.transform.localPosition = Vector3.zero;
            line2.transform.localRotation = Quaternion.identity;
            line2.transform.localScale = Vector3.one;
            line2.transform.parent = root.transform;
            kgline = line2.AddComponent<KGLine>();
            kgline.isUpdate = false;
            kgline.m_lineVerticesPosition.Add(vertices[2]);
            kgline.m_lineVerticesPosition.Add(vertices[3]);
            kgline.m_lineVerticesPosition.Add(vertices[7]);
            kgline.m_lineVerticesPosition.Add(vertices[6]);
            kgline.m_lineVerticesPosition.Add(vertices[2]);

            GameObject line3 = new GameObject("line3");
            line3.transform.localPosition = Vector3.zero;
            line3.transform.localRotation = Quaternion.identity;
            line3.transform.localScale = Vector3.one;
            line3.transform.parent = root.transform;
            kgline = line3.AddComponent<KGLine>();
            kgline.isUpdate = false;
            kgline.m_lineVerticesPosition.Add(vertices[0]);
            kgline.m_lineVerticesPosition.Add(vertices[3]);
            kgline.m_lineVerticesPosition.Add(vertices[7]);
            kgline.m_lineVerticesPosition.Add(vertices[4]);
            kgline.m_lineVerticesPosition.Add(vertices[0]);

            GameObject line4 = new GameObject("line4");
            line4.transform.localPosition = Vector3.zero;
            line4.transform.localRotation = Quaternion.identity;
            line4.transform.localScale = Vector3.one;
            line4.transform.parent = root.transform;
            kgline = line4.AddComponent<KGLine>();
            kgline.isUpdate = false;
            kgline.m_lineVerticesPosition.Add(vertices[4]);
            kgline.m_lineVerticesPosition.Add(vertices[5]);
            kgline.m_lineVerticesPosition.Add(vertices[1]);
            kgline.m_lineVerticesPosition.Add(vertices[0]);
            kgline.m_lineVerticesPosition.Add(vertices[4]);

            GameObject line5 = new GameObject("line5");
            line5.transform.localPosition = Vector3.zero;
            line5.transform.localRotation = Quaternion.identity;
            line5.transform.localScale = Vector3.one;
            line5.transform.parent = root.transform;
            kgline = line5.AddComponent<KGLine>();
            kgline.isUpdate = false;
            kgline.m_lineVerticesPosition.Add(vertices[4]);
            kgline.m_lineVerticesPosition.Add(vertices[5]);
            kgline.m_lineVerticesPosition.Add(vertices[6]);
            kgline.m_lineVerticesPosition.Add(vertices[7]);
            kgline.m_lineVerticesPosition.Add(vertices[4]);
        }
    };

    public class Node
    {

        public bool mIsLeaf = false;
        public Node mParent = null;
        public Node[] mChildren = { null, null, null, null, null, null, null, null };
        public Patch mPatch = new Patch();
    }

    public class Octree
    {
        static Vector3[] sOffset = 
        {
            new Vector3(-1,-1,-1),
            new Vector3(-1,-1,1),
            new Vector3(-1,1,-1),
            new Vector3(-1,1,1),

            new Vector3(1,-1,-1),
            new Vector3(1,-1,1),
            new Vector3(1,1,-1),
            new Vector3(1,1,1)
        };

        Node mRoot = null;
        int mMaxDepth = 0;
        bool mIsShowDebugLine = false;

        GameObject[] mSceneObjects;
        public Octree()
        {
            mRoot = new Node();
        }

        /*
         * 创建一颗深度为depth的八叉树
         */
        public void Create(GameObject[] sceneObjects,Vector3 center,float boxSize,int depth,bool isShowDebugLine)
        {
            mMaxDepth = depth;
            mIsShowDebugLine = isShowDebugLine;
            mSceneObjects = sceneObjects;
            mRoot.mPatch.mBoxMax = center + Vector3.one * boxSize * 0.5f;
            mRoot.mPatch.mBoxMin = center - Vector3.one * boxSize * 0.5f;
            mRoot.mPatch.mPosition = center;
            mRoot.mIsLeaf = true;
            if (mIsShowDebugLine)
                mRoot.mPatch.Show();
            for (int i = 0; i < 8; i++)
            {
                if (CreateChildNode(mRoot, i, 0))
                {
                    mRoot.mIsLeaf = false;
                }
            }
        }

        /*
         *true 创建子节点成功
         *false 创建子节点失败
         */
        bool CreateChildNode(Node parent,int n,int currDepth)
        {
            ++currDepth;
            if (currDepth > mMaxDepth)
                return false;
            Node node = new Node();
            node.mParent = parent;
            node.mIsLeaf = true;

            float boxSizeDivide2 = (parent.mPatch.mBoxMax.x - parent.mPatch.mBoxMin.x) * 0.25f;
            node.mPatch.mPosition = parent.mPatch.mPosition + (sOffset[n] * boxSizeDivide2);
            node.mPatch.mBoxMax = node.mPatch.mPosition + new Vector3(1, 1, 1) * boxSizeDivide2;
            node.mPatch.mBoxMin = node.mPatch.mPosition + new Vector3(-1, -1, -1) * boxSizeDivide2;
            if (mIsShowDebugLine)
                node.mPatch.Show();

            parent.mChildren[n] = node;
            for (int i = 0; i < 8; i++)
            {
                if (CreateChildNode(node, i, currDepth))
                {
                    node.mIsLeaf = false;
                }
            }
            return true;
        }

        /*
         * 判断patch与三角形p1p2p3是否有交集
         * true 有交集
         * false 无交集
         */
        bool IsIntersect(Patch patch,Vector3 p1,Vector3 p2,Vector3 p3)
        {
            return true;
        }
    }
}



