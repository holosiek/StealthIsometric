using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour{
    /*
        Based on Brackeys AudioManager
    */
    
    // #############################################
    // ##### VARIABLES
    
    [Tooltip("Instance of AudioManager")]
	public static AudioManager instance;
    
    [Tooltip("Array containing sounds to play")]
	public Sound[] sounds;

    // #############################################
    // ##### METHODS
    
    // Play sound
	public void Play(string a_sound){
        // Find sound in array
		Sound sound = Array.Find(sounds, item => item.name == a_sound);
        // If sound doesn't exist, throw warning and return function
		if(sound == null){
			Debug.LogWarning("Sound: " + a_sound + " not found!");
			return;
		}
        // Apply random pitch (if set other than 0)
        float temp = sound.pitchRandom/2f;
        sound.source.pitch = sound.pitch + UnityEngine.Random.Range(-temp, temp);
        // Play found sound
		sound.source.Play();
    }
    
    // Stop sound
	public void Stop(string a_sound){
        // Find sound in array
		Sound sound = Array.Find(sounds, item => item.name == a_sound);
        // If sound doesn't exist, throw warning and return function
		if(sound == null){
			Debug.LogWarning("Sound: " + a_sound + " not found!");
			return;
		}
        // Stop found sound
		sound.source.Stop();
    }
    
    // #############################################
    // ##### EVENTS
    
	void Awake(){
        // Check if AudioManager already exists, if yes, destroy this component, if not, set this as main instance;
        // Also don't destroy it on load another scene
		if(instance != null){
			Destroy(gameObject);
		} else {
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
        
        // For each sound in array
		foreach(Sound sound in sounds){
            // Create AudioSource component
			sound.source = gameObject.AddComponent<AudioSource>();
            // Apply sound settings to AudioSource
			sound.source.clip = sound.clip;
            sound.source.loop = sound.loop;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            // If autoplay is on, play sound
            if(sound.autoplay){
                sound.source.Play();
            }
		}
	}
}