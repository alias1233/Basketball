using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedHollowControl : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float hue = 0;

    public HueControl huecontrol;

    // Start is called before the first frame update
    void Awake()
    {
        huecontrol.hue = hue;
    }

    public void updateHue(float huediff)
    {
        hue += huediff;

        if(hue > 1)
        {
            hue = 0;
        }

        huecontrol.UpdateHue(hue);
    }
}
