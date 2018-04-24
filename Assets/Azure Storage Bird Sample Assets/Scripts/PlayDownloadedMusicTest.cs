using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Test class used to demonstrate downloading blobs and playing music from Streaming Assets.
/// </summary>
public class PlayDownloadedMusicTest : MonoBehaviour 
{
    private const string blockBlobContainerName = "music";
    private AudioSource audioSource;

    // Use this for initialization
    async void Start () 
	{
        audioSource = GetComponent<AudioSource>();
        await TestDownloadFileAsync();
        await TestDownloadAllBlobsAsync();
    }

    private IEnumerator PlayClipFromStreamingAssets(string fileNameWithExtension)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileNameWithExtension);
        WWW www = new WWW($"file://{path}");
        yield return www;

        var audioClip = www.GetAudioClip(false, false);
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private async Task TestDownloadAllBlobsAsync()
    {
        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }
        await BlobStorageUtilities.DownloadAllBlobsInContainerAsync(blockBlobContainerName, Application.streamingAssetsPath);
    }

    private async Task TestDownloadFileAsync()
    {
        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }
        const string fileNameToDownload = "Track 1.ogg";
        var path = Path.Combine(Application.streamingAssetsPath, fileNameToDownload);
        if (!File.Exists(path))
        {
            await BlobStorageUtilities.DownloadBlobFromAzureStorageAsync(
                fileNameToDownload, blockBlobContainerName, Application.streamingAssetsPath);
        }
        StartCoroutine(PlayClipFromStreamingAssets(fileNameToDownload));
    }
}
