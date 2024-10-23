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
    public List<MediaItem> mediaItems;  // �̵�� ������ ����Ʈ

    public Image imageDisplay;          // ������ ǥ���� Image
    public VideoPlayer videoPlayer;     // �������� ����� VideoPlayer
    public RawImage videoDisplay;       // �������� ǥ���� RawImage

    private int mediaIndex = 0;         // ���� �̵�� �ε���

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
        // ��� �̵�� ��Ȱ��ȭ
        imageDisplay.gameObject.SetActive(false);
        videoDisplay.gameObject.SetActive(false);
        videoPlayer.Stop();

        MediaItem currentItem = mediaItems[mediaIndex];

        if (currentItem.mediaType == MediaItem.MediaType.Image)
        {
            // �̹��� ǥ��
            imageDisplay.gameObject.SetActive(true);
            imageDisplay.sprite = currentItem.image;
        }
        else if (currentItem.mediaType == MediaItem.MediaType.Video)
        {
            // ������ ǥ��
            videoDisplay.gameObject.SetActive(true);
            videoPlayer.clip = currentItem.video;
            videoPlayer.Play();
        }
    }
}
