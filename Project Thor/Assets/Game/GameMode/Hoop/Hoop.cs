using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hoop : NetworkBehaviour
{
    public Teams team;
    public Transform AfterScoreLocation;

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer)
        {
            return;
        }

        if(other.gameObject.layer == 8)
        {
            if(other.TryGetComponent(out Ball ball))
            {
                if(ball.Velocity.y < 0 && !ball.bAttached)
                {
                    GameManager.Singleton.ScorePoint(team);

                    ball.TeleportTo(AfterScoreLocation.position);
                }
            }

            return;
        }

        if(other.gameObject.layer == 3)
        {
            if (other.TryGetComponent(out PlayerManager player))
            {
                if (player.GetVelocity().y < 0 && player.GetIsHoldingBall())
                {
                    GameManager.Singleton.ScorePoint(team);

                    Ball.Singleton.Detach();

                    Ball.Singleton.TeleportTo(AfterScoreLocation.position);
                }
            }
        }
    }
}
