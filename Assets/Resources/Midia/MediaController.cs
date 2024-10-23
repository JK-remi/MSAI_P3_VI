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
    public List<MediaItem> mediaItems;      // �̵�� ������ ����Ʈ

    public Image imageDisplay;              // ������ ǥ���� Image
    public VideoPlayer videoPlayer;         // �������� ����� VideoPlayer
    public RawImage videoDisplay;           // �������� ǥ���� RawImage
    public Image specialImage;              // Ư�� �̹����� ǥ���� Image

    private int mediaIndex = 0;             // ���� �̵�� �ε���
    private bool isSpecialImageActive = false;  // Ư�� �̹��� Ȱ��ȭ ����
    private bool isVideoPlaying = false;    // ������ ��� �� ����
    private bool isVideoPaused = false;     // ������ �Ͻ� ���� ����

    void Start()
    {
        videoPlayer.playOnAwake = false;    // �ڵ� ��� ��Ȱ��ȭ
        videoPlayer.loopPointReached += OnVideoFinished;
        specialImage.gameObject.SetActive(false); // Ư�� �̹��� ��Ȱ��ȭ
        ShowMedia();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ToggleSpecialImage();
        }
    }

    public void OnNext()
    {
        if (isSpecialImageActive) return; // Ư�� �̹����� ǥ�� ���̸� �������� ����

        mediaIndex = (mediaIndex + 1) % mediaItems.Count;
        ShowMedia();
    }

    public void OnPrevious()
    {
        if (isSpecialImageActive) return; // Ư�� �̹����� ǥ�� ���̸� �������� ����

        mediaIndex = (mediaIndex - 1 + mediaItems.Count) % mediaItems.Count;
        ShowMedia();
    }

    public void OnScreenClick()
    {
        if (isSpecialImageActive) return; // Ư�� �̹����� ǥ�� ���̸� �������� ����

        MediaItem currentItem = mediaItems[mediaIndex];

        if (currentItem.mediaType == MediaItem.MediaType.Video)
        {
            if (!isVideoPlaying)
            {
                // ������ ��� ����
                videoPlayer.Play();
                isVideoPlaying = true;
                isVideoPaused = false;
            }
            else
            {
                if (!isVideoPaused)
                {
                    // ������ �Ͻ� ����
                    videoPlayer.Pause();
                    isVideoPaused = true;
                }
                else
                {
                    // ������ �ٽ� ���
                    videoPlayer.Play();
                    isVideoPaused = false;
                }
            }
        }
    }

    void ToggleSpecialImage()
    {
        isSpecialImageActive = !isSpecialImageActive;

        if (isSpecialImageActive)
        {
            // Ư�� �̹��� ǥ��
            specialImage.gameObject.SetActive(true);

            // �ٸ� �̵�� ��Ȱ��ȭ
            imageDisplay.gameObject.SetActive(false);
            videoDisplay.gameObject.SetActive(false);
            videoPlayer.Stop();
            isVideoPlaying = false;
            isVideoPaused = false;
        }
        else
        {
            // Ư�� �̹��� �����
            specialImage.gameObject.SetActive(false);

            // ���� �̵�� ǥ��
            ShowMedia();
        }
    }

    void ShowMedia()
    {
        if (isSpecialImageActive) return; // Ư�� �̹����� ǥ�� ���̸� �������� ����

        // ��� �̵�� ��Ȱ��ȭ
        imageDisplay.gameObject.SetActive(false);
        videoDisplay.gameObject.SetActive(false);
        videoPlayer.Stop();
        isVideoPlaying = false;
        isVideoPaused = false;

        MediaItem currentItem = mediaItems[mediaIndex];

        if (currentItem.mediaType == MediaItem.MediaType.Image)
        {
            // �̹��� ǥ��
            imageDisplay.gameObject.SetActive(true);
            imageDisplay.sprite = currentItem.image;
        }
        else if (currentItem.mediaType == MediaItem.MediaType.Video)
        {
            // ������ ǥ�� (�ڵ� ������� ����)
            videoDisplay.gameObject.SetActive(true);

            videoPlayer.Stop();                  // ������ ��� ����
            videoPlayer.clip = currentItem.video; // ������ Ŭ�� �Ҵ�
            // videoPlayer.Play();               // �ڵ� ��� �ڵ� ����
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
        isVideoPaused = false;
    }
}
