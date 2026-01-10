using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public AudioSource source;
    public AudioSource muzzleSource;
    public AudioClip footstep;
    public AudioClip crawl;
    public AudioClip ruffle;
    public AudioClip musket;

    public void BindMuzzleSource(AudioSource newMuzzleSource)
    {
        if (newMuzzleSource != null)
            muzzleSource = newMuzzleSource;
    }

    public void PlayFootstepLoop()
    {
        if (source == null || footstep == null) return;

        if (!source.isPlaying || source.clip != footstep)
        {
            source.clip = footstep;
            source.loop = true;
            source.pitch = 1.8f;
            source.Play();
        }
    }

    public void PlayCrawlLoop()
    {
        if (source == null || crawl == null) return;

        if (!source.isPlaying || source.clip != crawl)
        {
            source.clip = crawl;
            source.loop = true;
            source.pitch = 1f;
            source.Play();
        }
    }

    public void StopMovementLoop()
    {
        if (source == null) return;

        if (source.isPlaying && (source.clip == footstep || source.clip == crawl))
            source.Stop();
    }

    public void PlayRuffle()
    {
        if (source == null || ruffle == null) return;

        source.loop = false;
        source.PlayOneShot(ruffle);
    }

    public void PlayShot()
    {
        var s = muzzleSource != null ? muzzleSource : source;
        if (s == null) return;

        s.loop = false;
        if (musket)
            s.PlayOneShot(musket);
    }
}
