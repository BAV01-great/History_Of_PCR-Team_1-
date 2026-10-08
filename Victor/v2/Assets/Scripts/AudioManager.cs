using UnityEngine;

/// ---- EDIT THESE NAMES to match your audio file names (without extension) ----
/// Put the files here:
///   Assets/Resources/Audio/VO/   -> voice overs
///   Assets/Resources/Audio/SFX/  -> sound effects
public static class AudioNames
{
    // Voice overs
    public const string VO_Welcome        = "vo_welcome";
    public const string VO_RTPCR          = "vo_rtpcr";
    public const string VO_QPCR           = "vo_qpcr";
    public const string VO_Multiplex      = "vo_multiplex";
    public const string VO_Nested         = "vo_nested";
    public const string VO_Conventional   = "vo_conventional";
    public const string VO_AllComplete    = "vo_all_complete";

    // Sound effects
    public const string SFX_Grab          = "sfx_grab";
    public const string SFX_Place         = "sfx_place";
    public const string SFX_StationDone   = "sfx_station_done";
    public const string SFX_ReactionStart = "sfx_reaction_start";
    public const string SFX_DoorOpen      = "sfx_door_open";
}

/// One instance in the scene. Add an AudioSource child/component for voice (set Spatial Blend = 0).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    public AudioSource voiceSource;        // 2D voice over source (Spatial Blend 0)
    public AudioSource sfxSource;          // 2D sfx source (Spatial Blend 0)

    [Range(0f, 1f)] public float voiceVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    const string VoPath = "Audio/VO/";
    const string SfxPath = "Audio/SFX/";

    void Awake()
    {
        Instance = this;
    }

    // ---- Voice overs (a new one replaces the current one) ----
    // These take a string, so you can also call them from UnityEvents in the Inspector.
    public void PlayVO(string clipName)
    {
        var clip = Load(VoPath, clipName);
        if (clip == null) return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume;
        voiceSource.Play();
    }

    public void StopVO()
    {
        voiceSource.Stop();
    }

    // ---- Sound effects ----
    public void PlaySFX(string clipName)
    {
        var clip = Load(SfxPath, clipName);
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// 3D sound at a world position (e.g. from the station the player is at)
    public void PlaySFXAt(string clipName, Vector3 position)
    {
        var clip = Load(SfxPath, clipName);
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
    }

    // ---- Loading ----
    AudioClip Load(string path, string clipName)
    {
        var clip = Resources.Load<AudioClip>(path + clipName);
        if (clip == null)
            Debug.LogWarning($"AudioManager: clip not found at Resources/{path}{clipName}");
        return clip;
    }
}
