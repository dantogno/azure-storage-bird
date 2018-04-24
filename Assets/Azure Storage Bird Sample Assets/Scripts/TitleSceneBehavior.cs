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
    [SerializeField]
    private Button playButton, quitButton;

    [SerializeField]
    private Animator downloadingPanelAnimator;

    // Name of the blob container on Azure Storage
    private const string blockBlobContainerName = "music";
    
    // Parameter used by the Downloading Panel animation controller to control it's visibility
    private const string isDownloadingAnimParameter = "isDownloading";

    // Scene name used for loading the next scene
    private const string gameSceneName = "Game Scene";

    // Local location assets downloaded from Azure Storage will be saved to.
    private string destinationPath;

    private Text playButtonText;


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
        playButton.interactable = false;
        playButtonText.color = playButton.colors.disabledColor;
        // This delay helps give the animation a chance to show properly.
        int animationDelay = 300;
        await Task.Delay(animationDelay);

        // Check if the required files exist locally. If not,
        // disable the Play button, display the downloading UI dialog panel,
        // and download the assets from Azure Storage!
        // In a production scenario, you would want to figure out a way to
        // gracefully handle a situation where we cannot connect to the internet
        // or some other issue blocks downloading of the assets.
        bool requiredFilesExist = CheckIfRequiredFilesExist();
        if (!requiredFilesExist)
        {
            downloadingPanelAnimator.SetBool(isDownloadingAnimParameter, true);
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            await Task.Delay(animationDelay);
            await BlobStorageUtilities.DownloadAllBlobsInContainerAsync(blockBlobContainerName, destinationPath);
            downloadingPanelAnimator.SetBool(isDownloadingAnimParameter, false);
            await Task.Delay(animationDelay);
        }
        playButton.interactable = true;
        playButtonText.color = playButton.colors.normalColor;
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
}
