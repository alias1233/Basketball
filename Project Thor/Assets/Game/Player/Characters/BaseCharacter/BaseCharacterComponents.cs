using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BaseCharacterComponents : MonoBehaviour
{
    public BaseCharacterMovement CharacterMovement;
    public BasePlayerManager PlayerManager;

    public Transform FPOrientation;
    public Transform TPOrientation;

    public Transform SpellsFistTransform;
    public Transform SpellsFistParentTransform;

    public GameObject CharacterModel;
    public BaseCharacterModelAnimScript CharacterAnimations;
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
    public AudioSource HitSound;
    public GameObject RightHand;

    public CameraScript camerascript;
    public Camera FPCamera;
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

    public GameObject Letter1;
    public GameObject Letter2;
    public GameObject Letter3;
    public AudioSource DomainExpansionVoice;
    public AudioSource Boom1;
    public AudioSource Boom2;
}
