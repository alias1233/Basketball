using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public struct Inputs
{
    public int TimeStamp;
    public bool W;
    public bool S;
    public bool A;
    public bool D;
    public bool SpaceBar;

    public Inputs(int timestamp, bool w, bool a, bool s, bool d, bool spacebar)
    {
        TimeStamp = timestamp;
        W = w;
        A = a;
        S = s;
        D = d;
        SpaceBar = spacebar;
    }
}
