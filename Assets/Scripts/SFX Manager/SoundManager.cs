//Author: Small Hedge Games
//Updated: 13/06/2024

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SmallHedge.SoundManager
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundsSO SO;
        private static SoundManager instance = null;
        private AudioSource audioSource;
        private static Dictionary<SoundType, AudioSource> loopSources = new Dictionary<SoundType, AudioSource>();

        private void Awake()
        {
            if(!instance)
            {
                instance = this;
                audioSource = GetComponent<AudioSource>();
            }
        }

        public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
        {
            SoundList soundList = instance.SO.sounds[(int)sound];
            AudioClip[] clips = soundList.sounds;
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];

            if (source)
            {
                StopLoop(sound);
                source.outputAudioMixerGroup = soundList.mixer;
                source.clip = randomClip;
                source.loop = soundList.loop;
                source.bypassReverbZones = soundList.bypassReverbZones;
                source.volume = volume * soundList.volume;
                source.Play();
            }
            else
            {
                if (soundList.loop)
                {
                    StopLoop(sound);
                    AudioSource loopSource = instance.gameObject.AddComponent<AudioSource>();
                    loopSource.outputAudioMixerGroup = soundList.mixer;
                    loopSource.clip = randomClip;
                    loopSource.loop = true;
                    loopSource.bypassReverbZones = soundList.bypassReverbZones;
                    loopSource.volume = volume * soundList.volume;
                    loopSource.Play();
                    loopSources[sound] = loopSource;
                }
                else
                {
                    instance.audioSource.outputAudioMixerGroup = soundList.mixer;
                    instance.audioSource.bypassReverbZones = soundList.bypassReverbZones;
                    instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);
                }
            }
        }

        public static void StopSound(SoundType sound, AudioSource source)
        {
            if (source == null)
            {
                StopLoop(sound);
                return;
            }

            SoundList soundList = instance.SO.sounds[(int)sound];
            AudioClip[] clips = soundList.sounds;

            foreach (AudioClip clip in clips)
            {
                if (source.clip == clip)
                {
                    Debug.Log($"Stopped sound: {clip.name}");
                    source.Stop();
                    source.loop = false;
                    return;
                }
            }

            Debug.LogWarning("The provided AudioSource is not playing any of the specified sounds.");
        }

        public static void StopSound(SoundType sound)
        {
            StopLoop(sound);
        }

        private static void StopLoop(SoundType sound)
        {
            if (loopSources.TryGetValue(sound, out AudioSource source))
            {
                source.Stop();
                Destroy(source);
                loopSources.Remove(sound);
            }
        }
    }

    [Serializable]
    public struct SoundList
    {
        [HideInInspector] public string name;
        [Range(0, 1)] public float volume;
        public bool loop;
        public bool bypassReverbZones;
        public AudioMixerGroup mixer;
        public AudioClip[] sounds;

        public SoundList(bool useDefaults = true)
        {
            name = "";
            volume = 1f;
            loop = false;
            bypassReverbZones = true;
            mixer = null;
            sounds = null;
        }
    }
}