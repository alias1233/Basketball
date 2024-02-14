using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunInBackgroundScript : MonoBehaviour
{
    [SerializeField]
    private bool ShouldRunInBackground = true;

    // Start is called before the first frame update
    void Start()
    {
        Application.runInBackground = ShouldRunInBackground;
    }
}
