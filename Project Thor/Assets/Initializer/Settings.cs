using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    [SerializeField]
    private bool ShouldRunInBackground = true;
    [SerializeField]
    private int FrameRate = 144;

    // Start is called before the first frame update
    void Start()
    {
        Application.runInBackground = ShouldRunInBackground;
        Application.targetFrameRate = FrameRate;
    }
}
