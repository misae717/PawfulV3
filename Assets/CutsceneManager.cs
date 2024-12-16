using UnityEngine;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [SerializeField]
    private string[] videoPaths; // Array of video file names

    private int currentVideoIndex = 0;

    void Start()
    {
        videoPlayer.loopPointReached += OnVideoEnd; // Callback when a video ends
        PlayVideo(0); // Start playing the first video
    }

    void PlayVideo(int index)
    {
        if (index < videoPaths.Length)
        {
            videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoPaths[index]);
            videoPlayer.Play();
            currentVideoIndex = index;
        }
        else
        {
            EndCutscene();
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        currentVideoIndex++;
        PlayVideo(currentVideoIndex); // Play the next video
    }

    void EndCutscene()
    {
        Debug.Log("Cutscene Complete!");
        // Add scene transition or other logic here
        UnityEngine.SceneManagement.SceneManager.LoadScene("Saleh"); // Replace with your next scene name
    }
}
