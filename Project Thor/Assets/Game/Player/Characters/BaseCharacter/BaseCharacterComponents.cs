using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BaseCharacterComponents : MonoBehaviour
{
    public Transform FPOrientation;
    public Transform TPOrientation;

    public GameObject CharacterModel;
    public CharacterModelAnimScript CharacterAnimations;
    public Transform HandTransform;
    public Transform ThrowBallLocationTransform;
    public List<MonoBehaviour> DisabledForOwnerScripts;
    public List<MonoBehaviour> DisabledForOthersScripts;
    public GameObject FirstPersonComponents;
    public GameObject ThirdPersonComponents;
    public GameObject FPPlayerCamera;
    public GameObject FirstPersonPlayerUI;
    public GameObject[] RenderOnTop;
    public ProgressBar FirstPersonHealthBar;
    public TMP_Text FirstPersonHealthBarText;
    public GameObject ThirdPersonHealthBarObject;
    public ProgressBar ThirdPersonHealthBar;
    public AudioSource DeathSound;

    public CameraVisualsScript CameraVisuals;
    public AudioSource LandAudio;
    public AudioSource JumpAudio;
    public AudioSource BoostedJumpAudio;
    public GameObject SlideChargeJumpUIObject;
    public ProgressBar SlideChargeJumpProgressBar;
    public ParticleSystem SlideParticles;
    public ParticleSystem SlideSmoke;
    public AudioSource SlideSound;
    public AudioSource SlideExitSound;
    public ParticleSystem GroundPoundImpactParticle;
    public ParticleSystem GroundPoundTrails;
    public AudioSource GroundPoundLandAudio;
}
