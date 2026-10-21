using UnityEngine;

public class BallVFXEvents : MonoBehaviour
{
    [Header("Game Manager Link")]
    public RouletteController gameManagerController; // Drag Game Manager here

    [Header("VFX Prefabs")]
    public GameObject pillar1ImpactVFXPrefab;
    public Transform pillar1HitPoint; 
     public GameObject pillar1ImpactVFXPrefab3;
    public Transform pillar1HitPoint3; 

    public GameObject pillar2ImpactVFXPrefab;
    public Transform pillar2HitPoint; 

    [Header("Audio")]
    public AudioSource ballAudioSource;
    public AudioClip bounceSFX;

    // 1. Call this from the Animation Event when the ball lands in the final slot
   

    // 2. Call this from Animation Event on Pillar 1 hit
    public void BlastConfetie()
    {
        if (pillar1ImpactVFXPrefab3 != null)
        {
            Vector3 pos = pillar1HitPoint3 != null ? pillar1HitPoint3.position : transform.position;
            GameObject vfx = Instantiate(pillar1ImpactVFXPrefab3, pos, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        
    }
    public void HitPillar1()
    {
        if (pillar1ImpactVFXPrefab != null)
        {
            Vector3 pos = pillar1HitPoint != null ? pillar1HitPoint.position : transform.position;
            GameObject vfx = Instantiate(pillar1ImpactVFXPrefab, pos, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        if (ballAudioSource != null && bounceSFX != null) ballAudioSource.PlayOneShot(bounceSFX);
    }

    // 3. Call this from Animation Event on Pillar 2 hit
    public void HitPillar2()
    {
        if (pillar2ImpactVFXPrefab != null)
        {
            Vector3 pos = pillar2HitPoint != null ? pillar2HitPoint.position : transform.position;
            GameObject vfx = Instantiate(pillar2ImpactVFXPrefab, pos, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        if (ballAudioSource != null && bounceSFX != null) ballAudioSource.PlayOneShot(bounceSFX);
    }
}