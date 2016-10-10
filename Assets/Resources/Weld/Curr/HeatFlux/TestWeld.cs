using UnityEngine;
public class TestWeld : MonoBehaviour
{
    HeatFlux mHeatFlux = null;
    Fluid mFluid = null;

    Material mMaterial = null;
    void Start()
    {
        mHeatFlux = gameObject.GetComponent<HeatFlux>();
        mFluid = gameObject.GetComponent<Fluid>();
        mMaterial = gameObject.GetComponent<Renderer>().material;
    }
    void OnWillRenderObject()
    {
        if (Camera.current.name.Equals(Camera.main.name))
        {
            if (mHeatFlux !=null)
                mMaterial.SetTexture("_HeatMap", mHeatFlux.mProcessmaps[(int)HeatFlux.Processmap.HeatFluxDistribution]);
            if (mFluid !=null)
                mMaterial.SetTexture("_Normalmap", mFluid.mProcessmaps[(int)Fluid.Processmap.Normal]);
            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.collider.name.Equals(gameObject.name))
                {
                    if (mHeatFlux != null)
                    {
                        mHeatFlux.mEffectivePower = 7.0f;
                        mHeatFlux.mHeatSourcePosition = hit.point;
                    }
                    if (mFluid != null)
                        mFluid.mHeatSourceHitUV = hit.textureCoord;
                }
            }
            else
            {
                if (mHeatFlux != null)
                    mHeatFlux.mEffectivePower = 0.0f;
                if (mFluid != null)
                    mFluid.mHeatSourceHitUV = new Vector2(-1, -1);
            }
        }
    }
}
