using UnityEngine;
using Unity.Netcode;

public class V1PlayerManager : BasePlayerManager
{
    private V1Movement V1movement;

    protected override void Awake()
    {
        base.Awake();

        V1movement = (V1Movement)Movement;
    }

    public override bool GetCanCarryBall()
    {
        return !V1movement.GetIsFlying();
    }
}
