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

        static KGFrameBox[] sKGFrameBox = { null, null, null, null, null, null, null, null, null, null, null }; 

        public void Show()
        {
            Vector3 min = mBoxMin;
            Vector3 max = mBoxMax;

            Vector3[] vertices = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            vertices[0].x = min.x; vertices[0].y = min.y; vertices[0].z = min.z;
            vertices[1].x = max.x; vertices[1].y = min.y; vertices[1].z = min.z;
            vertices[2].x = max.x; vertices[2].y = min.y; vertices[2].z = max.z;
            vertices[3].x = min.x; vertices[3].y = min.y; vertices[3].z = max.z;


            vertices[4].x = min.x; vertices[4].y = max.y; vertices[4].z = min.z;
            vertices[5].x = max.x; vertices[5].y = max.y; vertices[5].z = min.z;
            vertices[6].x = max.x; vertices[6].y = max.y; vertices[6].z = max.z;
            vertices[7].x = min.x; vertices[7].y = max.y; vertices[7].z = max.z;



            for (int i = 0; i < sKGFrameBox.Length; i++)
            {
                if (sKGFrameBox[i] == null)
                {
                    GameObject box = new GameObject("box" + i.ToString());
                    box.transform.localPosition = Vector3.zero;
                    box.transform.localRotation = Quaternion.identity;
                    box.transform.localScale = Vector3.one;
                    box.transform.parent = null;
                    sKGFrameBox[i] = box.AddComponent<KGFrameBox>();
                    sKGFrameBox[i].BeginDraw();
                }
            }

            for (int i = 0; i < sKGFrameBox.Length; i++)
            {
                if (sKGFrameBox[i].Draw(vertices, Color.green))
                {
                    break;
                }
                else
                {
                    if (i + 1 < sKGFrameBox.Length)
                        sKGFrameBox[i+1].Draw(vertices, Color.green);
                }
            }
        }
    };

    public class Node
    {
        public bool mIsLeaf = false;
        public bool mIsReal = false;
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

        GameObject[] mSceneObjects;
        public Octree()
        {
            mRoot = new Node();
        }

        /*
         * 创建一颗深度为depth的八叉树
         */
        public void Create(GameObject[] sceneObjects, Vector3 center, float boxSize, int depth)
        {
            mSceneObjects = sceneObjects;
            mMaxDepth = depth;
            mRoot.mPatch.mBoxMax = center + Vector3.one * boxSize * 0.5f;
            mRoot.mPatch.mBoxMin = center - Vector3.one * boxSize * 0.5f;
            mRoot.mPatch.mPosition = center;

            for (int i = 0; i < 8; i++)
            {
                CreateChildNode(mRoot, i, 0);
            }
        }

        public void ExciseScene()
        {
            ExciseScene(mRoot);
        }

        void ExciseScene(Node node)
        {
            if (node.mIsLeaf)
            {
                if (IsIntersectPatchAndScene(mSceneObjects, node))
                {
                    node.mIsReal = true;
                    node.mPatch.Show();
                }
            }
            for (int i = 0; i < 8; i++)
            {
                if (node.mChildren[i] != null)
                    ExciseScene(node.mChildren[i]);
            }
        }
        void CreateChildNode(Node parent, int n, int currDepth)
        {
            ++currDepth;
            if (currDepth > mMaxDepth)
            {
                parent.mIsLeaf = true;
                return;
            }
            bool isIntersection = false;
            for (int i = 0; i < mSceneObjects.Length; i++)
            {
                MeshFilter[] meshfilters = mSceneObjects[i].GetComponentsInChildren<MeshFilter>();
                for (int j = 0; j < meshfilters.Length; j++)
                {
                    Renderer rd = meshfilters[j].gameObject.GetComponent<Renderer>();
                    if (IsIntersectAABBandAABB(parent.mPatch.mBoxMin, parent.mPatch.mBoxMax, rd.bounds.min, rd.bounds.max))
                    {
                        isIntersection = true;
                        break;
                    }
                }
            }
            if (!isIntersection)
                return;
            Node node = new Node();
            node.mParent = parent;

            float boxSizeDivide2 = (parent.mPatch.mBoxMax.x - parent.mPatch.mBoxMin.x) * 0.25f;
            node.mPatch.mPosition = parent.mPatch.mPosition + (sOffset[n] * boxSizeDivide2);
            node.mPatch.mBoxMax = node.mPatch.mPosition + new Vector3(1, 1, 1) * boxSizeDivide2;
            node.mPatch.mBoxMin = node.mPatch.mPosition + new Vector3(-1, -1, -1) * boxSizeDivide2;

            parent.mChildren[n] = node;
            for (int k = 0; k < 8; k++)
            {
                CreateChildNode(node, k, currDepth);
            }
        }

        /*
         * 判断Patch与场景模型是否有交集
         * true 有交集
         * false 无交集
         */
        bool IsIntersectPatchAndScene(GameObject[] sceneObjects, Node node)
        {
            bool isIntersection = false;
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                MeshFilter[] meshfilters = sceneObjects[i].GetComponentsInChildren<MeshFilter>();
                for (int j = 0; j < meshfilters.Length; j++)
                {
                    Renderer rd = meshfilters[j].gameObject.GetComponent<Renderer>();
                    if (IsIntersectAABBandAABB(node.mPatch.mBoxMin, node.mPatch.mBoxMax, rd.bounds.min, rd.bounds.max))
                    {
                        Vector3[] verticesInModelSpace = meshfilters[j].sharedMesh.vertices;
                        int[] indices = meshfilters[j].mesh.GetIndices(0);
                        for (int k = 0; k < indices.Length - 2; k++)
                        {
                            Vector3 p1 = rd.localToWorldMatrix.MultiplyPoint3x4(verticesInModelSpace[indices[k]]);
                            Vector3 p2 = rd.localToWorldMatrix.MultiplyPoint3x4(verticesInModelSpace[indices[k + 1]]);
                            Vector3 p3 = rd.localToWorldMatrix.MultiplyPoint3x4(verticesInModelSpace[indices[k + 2]]);
                            if (IsIntersectTriAndBox(node.mPatch.mBoxMin, node.mPatch.mBoxMax, p1, p2, p3))
                            {
                                isIntersection = true;
                                goto exit;
                            }
                        }
                    }
                }
            }
        exit:
            return isIntersection;
        }

        /*
         * 判断AABB盒与三角形p1p2p3是否有交集
         * true 有交集
         * false 无交集
         */
        bool IsIntersectTriAndBox(Vector3 min, Vector3 max, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int crossCount = 0;
            Vector3[] vertices =
            {
                new Vector3(min.x,min.y,min.z),new Vector3(min.x,max.y,min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, min.y, min.z),
                new Vector3(min.x,min.y,max.z),new Vector3(min.x,max.y,max.z), new Vector3(max.x, max.y, max.z), new Vector3(max.x, min.y, max.z),
            };
            Vector3[] projPos = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, 
                                  Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            for (int i = 0; i < vertices.Length; i++)
            {
                float dist = 0.0f;
                CalculatePointToTriangleDist(vertices[i], p1, p2, p3, out dist, out projPos[i]);
                if (dist > 0)
                    crossCount++;
                else
                    crossCount--;
            }

            if (crossCount == 8 || crossCount == -8)
                return false;

            for (int i = 0; i < projPos.Length; i++)
            {
                if (IsPointInPolygon(projPos[i], new Vector3[] { p1, p2, p3 }))
                {
                    return true;
                }
            }
            return false;
        }
        /*
         *计算点p到三角形p1p2p3的距离 (负数：在三角面的负侧，正数：在三角面的正侧)
         */
        public static void CalculatePointToTriangleDist(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3, out float dist, out Vector3 point)
        {
            float lengthP1toCenter = Vector3.Distance(p, p1);

            Vector3 vec0 = (p - p1).normalized;
            Vector3 vec1 = Vector3.Cross(p2 - p1, p3 - p2).normalized;
            float dot = Vector3.Dot(vec0, vec1);
            dist = lengthP1toCenter * dot;
            point = p - vec1 * dist;
        }

        /*
         * 判断点point是否在多边形内
         * true 在
         * false 不在
         */
        bool IsPointInPolygon(Vector3 point,  Vector3[] polygons)
        {
            Vector3 vec0 = polygons[1] - polygons[0];
            Vector3 vec1 = polygons[2] - polygons[1];
            Vector3 faceNormal = Vector3.Cross(vec0, vec1);
            faceNormal.Normalize();

            bool isIn = true;
            for (int i = 0; i < polygons.Length; i++)
            {
                int nextv = (i + 1) % polygons.Length;
                int currv = i;
                Vector3 vect0 = polygons[nextv] - polygons[currv];
                Vector3 vect1 = point - polygons[nextv];
                Vector3 cross = Vector3.Cross(vect0, vect1);
                float dot = Vector3.Dot(cross, faceNormal);
                if (dot < 0)
                {
                    isIn = false;
                    continue;
                }
            }
            return isIn;
        }

        /*
         * 判断线段与多边形是否有交集
         * true 有交集
         * false 无交集
         * 线段方程：(p2-p1)*t + p1 = p (0<= t <= 1)
         * 平面方程：A(x-x0) + B(y-y0) + C(z-z0) = 0
         */
        bool IsIntersectLineAndPolygon(Vector3 linep0, Vector3 linep1, Vector3[] polygons, ref Vector3 intersection)
        {
            if (polygons.Length < 3)
                return false;
            Vector3 lineVec = linep1 - linep0;
            Vector3 vec0 = polygons[1] - polygons[0];
            Vector3 vec1 = polygons[2] - polygons[1];
            Vector3 faceNormal = Vector3.Cross(vec0, vec1);
            faceNormal.Normalize();
            float parallel = Vector3.Dot(faceNormal, lineVec);
            if (Mathf.Abs(parallel) < 0.00001f)
                return false;
            float t = (Vector3.Dot(faceNormal, polygons[0]) - Vector3.Dot(faceNormal, linep0)) / parallel;
            if (t < 0 || t > 1)
                return false;
            intersection = linep0 + lineVec * t;
            bool isIn = true;
            for (int i = 0; i < polygons.Length; i++)
            {
                int nextv = (i + 1) % polygons.Length;
                int currv = i;
                Vector3 vect0 = polygons[nextv] - polygons[currv];
                Vector3 vect1 = intersection - polygons[nextv];
                Vector3 cross = Vector3.Cross(vect0, vect1);
                float dot = Vector3.Dot(cross, faceNormal);
                if (dot < 0)
                {
                    isIn = false;
                    continue;
                }
            }
            return isIn;
        }

        /*
         * 判断两个AABB盒是否有交集
         * true 有交集
         * false 无交集
         */
        bool IsIntersectAABBandAABB(Vector3 min0, Vector3 max0, Vector3 min1, Vector3 max1)
        {
            Vector3[] vertices0 =
            {
                new Vector3(min0.x,min0.y,min0.z),new Vector3(min0.x,max0.y,min0.z), new Vector3(max0.x, max0.y, min0.z), new Vector3(max0.x, min0.y, min0.z),
                new Vector3(min0.x,min0.y,max0.z),new Vector3(min0.x,max0.y,max0.z), new Vector3(max0.x, max0.y, max0.z), new Vector3(max0.x, min0.y, max0.z),
            };
            Vector3[] vertices1 =
            {
                new Vector3(min1.x,min1.y,min1.z),new Vector3(min1.x,max1.y,min1.z), new Vector3(max1.x, max1.y, min1.z), new Vector3(max1.x, min1.y, min1.z),
                new Vector3(min1.x,min1.y,max1.z),new Vector3(min1.x,max1.y,max1.z), new Vector3(max1.x, max1.y, max1.z), new Vector3(max1.x, min1.y, max1.z),
            };

            for (int i = 0; i < 8; i++)
            {
                if (IsPointInAABB(min1, max1, vertices0[i]))
                {
                    return true;
                }
            }
            for (int i = 0; i < 8; i++)
            {
                if (IsPointInAABB(min0, max0, vertices1[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /*
         * 判断空间一点是否在AABB盒内
         * true 在AABB盒内
         * false 不在AABB盒内
         */
        bool IsPointInAABB(Vector3 min, Vector3 max, Vector3 point)
        {
            if (point.x >= min.x && point.x <= max.x &&
               point.y >= min.y && point.y <= max.y &&
               point.z >= min.z && point.z <= max.z)
                return true;
            return false;
        }
    }
}



