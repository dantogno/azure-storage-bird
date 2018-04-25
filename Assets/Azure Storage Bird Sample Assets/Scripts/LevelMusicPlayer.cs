using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// This class plays music downloaded from Azure Blob Storage into the local
/// streaming assets directory.
/// New tracks are played as the player progresses through levels by subscribing to the
/// GameControl.StartedNewLevel event.
/// </summary>
public class LevelMusicPlayer : MonoBehaviour 
{
    public static event Action LoadingAudioClipsFinished;
    // These track names are added to the array in a specific order of progressing intensity
    // (based on BPM and my own judgement) to match increasing level challenge.
    public static readonly string[] MusicFileNamesInLevelOrder =
        { "Track 6.ogg", "Track 7.ogg", "Track 4.ogg", "Track 3.ogg",
          "Track 1.ogg", "Track 8.ogg", "Track 9.ogg", "Track 2.ogg",
          "Track 5.ogg", "Track 10.ogg" };
    private AudioSource audioSource;
    private AudioClip[] audioClips;

    // Use this for initialization
    private void Awake () 
	{
        DontDestroyOnLoad(this.gameObject);
        audioClips = new AudioClip[MusicFileNamesInLevelOrder.Length];
        audioSource = GetComponent<AudioSource>();
    }

    private IEnumerator PreloadClipsFromStreamingAssets()
    {
        for (int i = 0; i < MusicFileNamesInLevelOrder.Length; i++)
        {
            var path = Path.Combine(Application.streamingAssetsPath, MusicFileNamesInLevelOrder[i]);
            WWW www = new WWW($"file://{path}");
            yield return www;

            var audioClip = www.GetAudioClip(false, false);
            audioClips[i] = audioClip;
        }
        LoadingAudioClipsFinished?.Invoke();
    }

    /// <summary>
    /// Play a an AudioClip from the StreamingAssets location in Unity.
    /// </summary>
    /// <param name="fileNameWithExtension">Filename of clip to play.</param>
    private IEnumerator PlayClipFromStreamingAssets(string fileNameWithExtension)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileNameWithExtension);
        WWW www = new WWW($"file://{path}");
        yield return www;

        var audioClip = www.GetAudioClip(false, false);
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private void PlayAudioClipForNewLevel(int levelIndex)
    {
        if (audioClips.Length > levelIndex)
        {
            audioSource.clip = audioClips[levelIndex];
            audioSource.Play();
        }
    }

    #region Event handlers
    /// <summary>
    /// GameControl.StartedNewLevel event handler.
    /// </summary>
    /// <param name="levelIndex">The event passes us the current levelIndex.</param>
    private void OnStartedNewLevel(int levelIndex)
    {
        PlayAudioClipForNewLevel(levelIndex);
    }

    /// <summary>
    /// GameControl.GameOver event handler.
    /// </summary>
    private void OnGameOver()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// TitleSceneBehavior.DownloadingMusicFilesFinished event handler.
    /// We need to wait for the files to be done downloading, then begin preloading.
    /// </summary>
    private void OnDownloadingMusicFilesFinished()
    {
        StartCoroutine(PreloadClipsFromStreamingAssets());
    }
    #endregion

    #region Event subscription / unsubscription
    private void OnEnable()
    {
        GameControl.StartedNewLevel += OnStartedNewLevel;
        GameControl.GameOver += OnGameOver;
        TitleSceneBehavior.DownloadingMusicFilesFinished += OnDownloadingMusicFilesFinished;
    }

    private void OnDisable()
    {
        GameControl.StartedNewLevel -= OnStartedNewLevel;
        GameControl.GameOver -= OnGameOver;
        TitleSceneBehavior.DownloadingMusicFilesFinished += OnDownloadingMusicFilesFinished;
    }
    #endregion
}
