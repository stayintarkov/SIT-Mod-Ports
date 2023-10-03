using GPUInstancer;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace CWX_GrassCutter
{
    public class GrassCutterScript
    {
        public void Start()
        {
            List<GPUInstancerDetailManager> allGrass = GameObject.FindObjectsOfType<GPUInstancerDetailManager>().ToList();
            GameObject streetsGrass = GameObject.Find("GrassPrefabManager");

            foreach (var grass in allGrass)
            {
               grass.enabled = false;
            }

            streetsGrass?.gameObject.SetActive(false);
        }
    }
}
