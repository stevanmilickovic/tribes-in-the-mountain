using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public AudioSource source;
    public AudioSource muzzleSource;
    public AudioClip footstep;
    public AudioClip crawl;
    public AudioClip ruffle;
    public AudioClip musket;

    public void PlayFootstepLoop()
    {
        if ((!source.isPlaying) || source.clip != footstep)
        {
            source.clip = footstep;
            source.loop = true;
            source.pitch = 1.8f;
            source.Play();
        }
    }

    public void PlayCrawlLoop()
    {
        if ((!source.isPlaying) || source.clip != crawl)
        {
            source.clip = crawl;
            source.loop = true;
            source.Play();
        }
    }


    public void StopMovementLoop()
    {
        if (source.isPlaying && (source.clip == footstep || source.clip == crawl))
            source.Stop();
    }

    public void PlayRuffle()
    {
        source.loop = false;
        if (ruffle)
            source.PlayOneShot(ruffle);
    }

    public void PlayShot()
    {
        muzzleSource.loop = false;
        if (musket)
            muzzleSource.PlayOneShot(musket);
    }
}
