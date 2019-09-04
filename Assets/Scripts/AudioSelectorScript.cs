using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSelectorScript : MonoBehaviour {

    public List<AudioClip> clips = new List<AudioClip>();
    int clipIndex = 0;
    AudioSource source;

	// Use this for initialization
	void Start () {


        //clips.Add(Resources.Load<AudioClip>("Sounds/Music/BillWurtz/and-the-day-goes-on.mp3") );
        //Object[] resources = Resources.LoadAll("Sounds/Music/BillWurtz");

        clips.AddRange(Resources.LoadAll<AudioClip>("Sounds/Music/BillWurtz"));

        /*Debug.Log("Loaded resources: " + resources.Length);

        foreach (Object obj in resources)
        {
            if (obj is AudioClip) clips.Add((AudioClip)obj);
        }*/

        Debug.Log("Loaded " + clips.Count + " clips!");

        shuffleClips(200);

        source = GetComponent<AudioSource>();

	}
	
	// Update is called once per frame
	void Update () {
		if (!source.isPlaying)
        {
            nextTrack();
        }
        if (Input.GetKeyDown("r")) nextTrack();

	}

    void nextTrack()
    {
        source.Stop();
        source.clip = clips[clipIndex];
        source.Play();

        clipIndex++;
        clipIndex %= clips.Count;
    }

    void shuffleClips(int times)
    {
        for (int i=0;i<times;i++)
        {
            int first = (int) Mathf.Round(Random.Range(0, clips.Count - 1));
            int second = (int) Mathf.Round(Random.Range(0, clips.Count - 1));
            AudioClip swap = clips[first];
            clips[first] = clips[second];
            clips[second] = swap;
        }
    }
}
