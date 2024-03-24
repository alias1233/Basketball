using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Hoop : NetworkBehaviour
{
    public Teams team;

    public Transform AfterScoreLocation;

    public float ScoreCooldown;
    private float LastTimeScored;

    public ParticleSystem OnScoreExplosion;
    public AudioSource OnScoreSound;
    public AudioSource CrowdCheer;

    public GameObject BlueTeamParticle;
    public GameObject RedTeamParticle;

    public AudioSource DunkSound;
    public GameObject DunkPictureGameObject;
    public Image DunkPicture;
    public float DunkPictureDuration;

    private void Awake()
    {
        if(team == Teams.Red)
        {
            BlueTeamParticle.SetActive(false);
        }

        else
        {
            RedTeamParticle.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer)
        {
            return;
        }

        if (Time.time - LastTimeScored < ScoreCooldown)
        {
            return;
        }

        if (other.gameObject.layer == 8)
        {
            if(other.TryGetComponent(out Ball ball))
            {
                if(!ball.bAttached)
                {
                    LastTimeScored = Time.time;

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
                if (player.GetIsHoldingBall())
                {
                    LastTimeScored = Time.time;

                    if (player.GetIsDunking() || player.GetIsFlying())
                    {
                        OnDunk(player.GetClientID());
                    }

                    GameManager.Singleton.ScorePoint(team);

                    Ball.Singleton.Detach();
                    Ball.Singleton.TeleportTo(AfterScoreLocation.position);

                    return;
                }

                player.TeleportTo(AfterScoreLocation.position);
            }
        }
    }

    public void OnScore()
    {
        OnScoreExplosion.Play();
        OnScoreSound.Play();
        CrowdCheer.Play();
    }

    private void OnDunk(ulong id)
    {
        ClientRpcParams TargetClientID = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { id }
            }
        };

        DunkClientRpc(TargetClientID);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void DunkClientRpc(ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(ShowDunkPicture(DunkPictureDuration));
        DunkSound.Play();
    }

    public IEnumerator ShowDunkPicture(float duration)
    {
        DunkPicture.color = new Color(255, 255, 255, 255);
        DunkPictureGameObject.SetActive(true);
        float elapsed = 0;

        while (elapsed < duration)
        {
            if(elapsed > duration / 2)
            {
                DunkPicture.color = new Color(255, 255, 255, 1 - (elapsed - duration / 2) / (duration / 2));
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        DunkPictureGameObject.SetActive(false);
    }
}
