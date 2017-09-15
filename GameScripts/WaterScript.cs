using UnityEngine;

namespace GameScripts
{
    [ExecuteInEditMode]
    public class WaterScript : MonoBehaviour
    {

        public Camera cam;
 
        void OnEnable()
        {
            if (Camera.main != null)
            {
                cam = Camera.main;
                if (cam.depthTextureMode == DepthTextureMode.None)
                    cam.depthTextureMode = DepthTextureMode.Depth;
            }
        }
    }
}
