using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

        LastTimeScored = Time.time;

        if (other.gameObject.layer == 8)
        {
            if(other.TryGetComponent(out Ball ball))
            {
                if(!ball.bAttached)
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
                if (player.GetIsHoldingBall())
                {
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
}
