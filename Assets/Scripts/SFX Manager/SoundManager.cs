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

        private void Start()
        {
            AudioMixerGroup group = audioSource != null ? audioSource.outputAudioMixerGroup : null;
            GameSettings.ApplyAudio(group != null ? group.audioMixer : null);
        }

        public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
        {
            if (!TryGetSound(sound, out SoundList soundList))
            {
                return;
            }
            AudioClip[] clips = soundList.sounds;
            if (clips == null || clips.Length == 0)
            {
                return;
            }
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (randomClip == null)
            {
                return;
            }

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

            if (!TryGetSound(sound, out SoundList soundList) || soundList.sounds == null)
            {
                return;
            }
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
                if (source != null)
                {
                    source.Stop();
                    Destroy(source);
                }
                loopSources.Remove(sound);
            }
        }

        private static bool TryGetSound(SoundType sound, out SoundList soundList)
        {
            soundList = default;
            int index = (int)sound;
            if (instance == null || instance.SO == null || instance.SO.sounds == null || index < 0 || index >= instance.SO.sounds.Length)
            {
                return false;
            }
            soundList = instance.SO.sounds[index];
            return true;
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }
            foreach (AudioSource source in loopSources.Values)
            {
                if (source != null)
                {
                    source.Stop();
                    Destroy(source);
                }
            }
            loopSources.Clear();
            instance = null;
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
