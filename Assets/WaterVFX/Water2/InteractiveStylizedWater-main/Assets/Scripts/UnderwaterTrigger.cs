using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderwaterTrigger : MonoBehaviour
{
    private int activeCameraCount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "MainCamera")
        {
            activeCameraCount++;
            RenderSettings.fog = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "MainCamera")
        {
            activeCameraCount = Mathf.Max(0, activeCameraCount - 1);
            if (activeCameraCount == 0)
            {
                RenderSettings.fog = false;
            }
        }
    }

    private void OnDisable()
    {
        activeCameraCount = 0;
        RenderSettings.fog = false;
    }
}
