using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AudioManager : MonoBehaviour
{
    static AudioManager m_instance;
    public static AudioManager Instance { get => m_instance; }

    [SerializeField] AudioClip m_hoverSound;
    public AudioClip HoverSound { get => m_hoverSound; }

    [SerializeField] AudioClip m_clickSound;
    public AudioClip ClickSound { get => m_clickSound; }

    private void Awake()
    {
        m_instance = this;
    }

    public void PlaySound(AudioClip clip)
    {
        GameObject audio = new GameObject("audio");
        AudioSource audioSource = audio.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        Destroy(audio, clip.length);
    }
}