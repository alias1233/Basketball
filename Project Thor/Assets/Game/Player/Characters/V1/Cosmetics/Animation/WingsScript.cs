using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingsScript : MonoBehaviour
{
    public AudioSource FlapSound1;
    public AudioSource FlapSound2;
    public AudioSource FlapSound3;
    public AudioSource FlapSound4;

    int index;

    public void PlayFlapSound()
    {
        index++;
        
        if(index > 4)
        {
            index = 1;
        }

        switch (index)
        {
            case 1:
                FlapSound1.Play(); break;
            case 2:
                FlapSound2.Play(); break;
            case 3:
                FlapSound3.Play(); break;
            case 4:
                FlapSound4.Play(); break;
        }
    }
}
