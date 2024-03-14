using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBaseNetworkObject
{
    bool bIsActive { get; set; }

    void Spawn() { }

    void Despawn() { }

    void Activate() { }

    void Deactivate() { }
}
