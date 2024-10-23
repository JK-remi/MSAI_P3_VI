using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

[System.Serializable]
public class MediaItem
{
    public enum MediaType { Image, Video }
    public MediaType mediaType;
    public Sprite image;
    public VideoClip video;
}

public class MediaController : MonoBehaviour
{
    public List<MediaItem> mediaItems;  // 미디어 아이템 리스트

    public Image imageDisplay;          // 사진을 표시할 Image
    public VideoPlayer videoPlayer;     // 동영상을 재생할 VideoPlayer
    public RawImage videoDisplay;       // 동영상을 표시할 RawImage

    private int mediaIndex = 0;         // 현재 미디어 인덱스

    void Start()
    {
        ShowMedia();
    }

    public void OnNext()
    {
        mediaIndex = (mediaIndex + 1) % mediaItems.Count;
        ShowMedia();
    }

    public void OnPrevious()
    {
        mediaIndex = (mediaIndex - 1 + mediaItems.Count) % mediaItems.Count;
        ShowMedia();
    }

    void ShowMedia()
    {
        // 모든 미디어 비활성화
        imageDisplay.gameObject.SetActive(false);
        videoDisplay.gameObject.SetActive(false);
        videoPlayer.Stop();

        MediaItem currentItem = mediaItems[mediaIndex];

        if (currentItem.mediaType == MediaItem.MediaType.Image)
        {
            // 이미지 표시
            imageDisplay.gameObject.SetActive(true);
            imageDisplay.sprite = currentItem.image;
        }
        else if (currentItem.mediaType == MediaItem.MediaType.Video)
        {
            // 동영상 표시
            videoDisplay.gameObject.SetActive(true);
            videoPlayer.clip = currentItem.video;
            videoPlayer.Play();
        }
    }
}
