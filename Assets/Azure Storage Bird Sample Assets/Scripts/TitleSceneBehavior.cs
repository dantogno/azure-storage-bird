using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class handles all of the Title Scene behavior, which includes menu for starting a new game and quitting,
/// as well as downloading all of the required music files if they are missing.
/// </summary>
public class TitleSceneBehavior : MonoBehaviour 
{
    public static event Action DownloadingMusicFilesFinished;

    #region Editor fields
    [SerializeField]
    private Button playButton, quitButton;

    [SerializeField]
    private Animator downloadingPanelAnimator;
    #endregion

    #region Private fields
    // Name of the blob container on Azure Storage
    private const string blockBlobContainerName = "music";
    
    // Parameter used by the Downloading Panel animation controller to control it's visibility
    private const string isDownloadingAnimParameter = "isDownloading";

    // Scene name used for loading the next scene
    private const string gameSceneName = "Game Scene";

    // Local location assets downloaded from Azure Storage will be saved to.
    private string destinationPath;

    private Text playButtonText;

    // Used to keep track of when we are ready to start the game.
    private bool filesAreDownloaded, filesArePreloaded;
    #endregion

    private void Awake()
    {
        // Hiding the quit button on platforms it doesn't make sense on.
#if UNITY_STANDALONE
        quitButton.gameObject.SetActive(true);
#else
        quitButton.gameObject.SetActive(false);
#endif
    }

    // Use this for initialization
    // Note that Start is async.
    private async void Start ()
    {
        playButtonText = playButton.GetComponentInChildren<Text>();
        destinationPath = Application.streamingAssetsPath;
        EnablePlayButton(false);
        await ShowDownloadingPopupAsync(true);
        await DownloadRequiredFiles();
    }

    /// <summary>
    /// Check if the required files exist locally. If not,
    /// download the assets from Azure Storage.
    /// In a production scenario, you would want to gracefully handle
    /// situations where we cannot connect to the internet or 
    /// some other issue blocks the downloading of the assets.
    /// </summary>
    private async Task DownloadRequiredFiles()
    {
        bool requiredFilesExist = CheckIfRequiredFilesExist();
        if (!requiredFilesExist)
        {            
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            await BlobStorageUtilities.DownloadAllBlobsInContainerAsync(blockBlobContainerName, destinationPath);            
        }
        DownloadingMusicFilesFinished?.Invoke();
    }

    private async Task ShowDownloadingPopupAsync(bool showPopup)
    {
        // Delay helps give the animation a chance to show properly.
        int animationDelay = 300;
        downloadingPanelAnimator.SetBool(isDownloadingAnimParameter, showPopup);
        await Task.Delay(animationDelay);
    }

    /// <summary>
    /// Use to visually and functionally change the Play button by
    /// setting whether or not it can be interacted with.
    /// The idea is to disable the button until the game can be safely started
    /// (i.e. music files are downloaded and preloaded).
    /// </summary>
    private void EnablePlayButton(bool isInteractable)
    {
        playButton.interactable = isInteractable;
        playButtonText.color = 
            isInteractable ? playButton.colors.normalColor : playButton.colors.disabledColor;
    }

    /// <summary>
    /// Check for required files.
    /// </summary>
    /// <returns> Returns true if all files exist, or false if anything is missing.</returns>
    private bool CheckIfRequiredFilesExist()
    {
        // The LevelMusicPlayer.MusicFileNamesInLevelOrder array represents all the file names
        // we need to download from Azure.
        foreach (var filename in LevelMusicPlayer.MusicFileNamesInLevelOrder)
        {
            var path = Path.Combine(destinationPath, filename);
            if (!File.Exists(path))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Updates if playbutton can be interacted with based on whether
    /// files are downloaded and preloaded.
    /// </summary>
    private void UpdatePlayButton()
    {
        bool shouldEnablePlayButton = filesAreDownloaded && filesArePreloaded;
        EnablePlayButton(shouldEnablePlayButton);
    }
    #region Event handlers
    private void OnDownloadingMusicFilesFinished()
    {
        filesAreDownloaded = true;
    }

    private async void OnLoadingAudioClipsFinishedAsync()
    {
        filesArePreloaded = true;
        await ShowDownloadingPopupAsync(false);
        UpdatePlayButton();
    }
    #endregion
    #region Unity UI Button click event handlers
    public void PlayButtonClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitButtonClicked()
    {
        Application.Quit();
    }
    #endregion
    #region Event subscription / unsubscription
    private void OnEnable()
    {
        DownloadingMusicFilesFinished += OnDownloadingMusicFilesFinished;
        LevelMusicPlayer.LoadingAudioClipsFinished += OnLoadingAudioClipsFinishedAsync;
    }

    private void OnDisable()
    {
        DownloadingMusicFilesFinished -= OnDownloadingMusicFilesFinished;
        LevelMusicPlayer.LoadingAudioClipsFinished -= OnLoadingAudioClipsFinishedAsync;
    }
    #endregion
}
