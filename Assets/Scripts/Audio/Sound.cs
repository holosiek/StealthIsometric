using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class Sound{
    // #############################################
    // ##### VARIABLES
    
    [Tooltip("Name of sound")]
	public string name;
    
    [Tooltip("Audio clip source")]
	public AudioClip clip;

	[Range(0f, 1f)]
    [Tooltip("Volume of audio")]
	public float volume = 1f;

	[Range(0.01f, 3f)]
    [Tooltip("Pitch of audio")]
	public float pitch = 0f;
    
	[Range(0f, 1f)]
    [Tooltip("Random pitch to be applied on play")]
	public float pitchRandom = 0f;

    [Tooltip("Should audio be looped?")]
	public bool loop = false;
    
    [Tooltip("Should be autoplayed?")]
	public bool autoplay = false;

    // AudioSource component
	[HideInInspector]
	public AudioSource source;
}
