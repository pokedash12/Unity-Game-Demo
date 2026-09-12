using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicLibrary : MonoBehaviour
{
    [SerializeField] private MusicGroup[] musicGroups;
    private Dictionary<string, List<AudioClip>> musDictionary;
    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        musDictionary = new Dictionary<string, List<AudioClip>>();
        foreach(MusicGroup musicGroup in musicGroups)
        {
            musDictionary[musicGroup.name] = musicGroup.audioClips;
        }
    }

    public AudioClip GetRandomClip(string name)
    {
        if (musDictionary.ContainsKey(name))
        {
            List<AudioClip> audioClips = musDictionary[name];
            if(audioClips.Count > 0)
            {
                return audioClips[UnityEngine.Random.Range(0, audioClips.Count)];
            }
        }
        return null;
    }
    
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[System.Serializable]
public struct MusicGroup
{
    public string name;
    public List<AudioClip> audioClips;
}