//Author: Small Hedge Games
//Updated: 13/06/2024

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SmallHedge.SoundManager
{
    [CustomEditor(typeof(SoundsSO))]
    public class SoundsSOEditor : Editor
    {
        private void OnEnable()
        {
            ref SoundList[] soundList = ref ((SoundsSO)target).sounds;

            if (soundList == null)
                return;

            string[] names = Array.FindAll(Enum.GetNames(typeof(SoundType)), name => name != nameof(SoundType.NONE));
            bool differentSize = names.Length != soundList.Length;

            Dictionary<string, SoundList> sounds = new();

            if (differentSize)
            {
                for (int i = 0; i < soundList.Length; ++i)
                {
                    if (!string.IsNullOrEmpty(soundList[i].name))
                    {
                        sounds[soundList[i].name] = soundList[i];
                    }
                }
            }

            Array.Resize(ref soundList, names.Length);
            for (int i = 0; i < soundList.Length; i++)
            {
                string currentName = names[i];
                soundList[i].name = currentName;

                if (differentSize)
                {
                    if (sounds.TryGetValue(currentName, out SoundList current))
                    {
                        soundList[i] = current;
                    }
                    else
                    {
                        soundList[i] = new SoundList(true);
                        soundList[i].sounds = Array.Empty<AudioClip>();
                    }
                    soundList[i].name = currentName;
                }
            }
        }
    }
}
#endif
