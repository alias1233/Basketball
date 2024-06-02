using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TornadoScript : BaseProjectile
{
    public AudioSource TornadoSound;

    public override void Activate()
    {
        base.Activate();

        TornadoSound.Play();
    }
}
