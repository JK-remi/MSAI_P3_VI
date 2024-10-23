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
    public List<MediaItem> mediaItems;      // 미디어 아이템 리스트

    public Image imageDisplay;              // 사진을 표시할 Image
    public VideoPlayer videoPlayer;         // 동영상을 재생할 VideoPlayer
    public RawImage videoDisplay;           // 동영상을 표시할 RawImage
    public Image specialImage;              // 특별 이미지를 표시할 Image

    private int mediaIndex = 0;             // 현재 미디어 인덱스
    private bool isSpecialImageActive = false;  // 특별 이미지 활성화 여부
    private bool isVideoPlaying = false;    // 동영상 재생 중 여부
    private bool isVideoPaused = false;     // 동영상 일시 정지 여부

    void Start()
    {
        videoPlayer.playOnAwake = false;    // 자동 재생 비활성화
        videoPlayer.loopPointReached += OnVideoFinished;
        specialImage.gameObject.SetActive(false); // 특별 이미지 비활성화
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
        if (isSpecialImageActive) return; // 특별 이미지가 표시 중이면 동작하지 않음

        mediaIndex = (mediaIndex + 1) % mediaItems.Count;
        ShowMedia();
    }

    public void OnPrevious()
    {
        if (isSpecialImageActive) return; // 특별 이미지가 표시 중이면 동작하지 않음

        mediaIndex = (mediaIndex - 1 + mediaItems.Count) % mediaItems.Count;
        ShowMedia();
    }

    public void OnScreenClick()
    {
        if (isSpecialImageActive) return; // 특별 이미지가 표시 중이면 동작하지 않음

        MediaItem currentItem = mediaItems[mediaIndex];

        if (currentItem.mediaType == MediaItem.MediaType.Video)
        {
            if (!isVideoPlaying)
            {
                // 동영상 재생 시작
                videoPlayer.Play();
                isVideoPlaying = true;
                isVideoPaused = false;
            }
            else
            {
                if (!isVideoPaused)
                {
                    // 동영상 일시 정지
                    videoPlayer.Pause();
                    isVideoPaused = true;
                }
                else
                {
                    // 동영상 다시 재생
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
            // 특별 이미지 표시
            specialImage.gameObject.SetActive(true);

            // 다른 미디어 비활성화
            imageDisplay.gameObject.SetActive(false);
            videoDisplay.gameObject.SetActive(false);
            videoPlayer.Stop();
            isVideoPlaying = false;
            isVideoPaused = false;
        }
        else
        {
            // 특별 이미지 숨기기
            specialImage.gameObject.SetActive(false);

            // 현재 미디어 표시
            ShowMedia();
        }
    }

    void ShowMedia()
    {
        if (isSpecialImageActive) return; // 특별 이미지가 표시 중이면 동작하지 않음

        // 모든 미디어 비활성화
        imageDisplay.gameObject.SetActive(false);
        videoDisplay.gameObject.SetActive(false);
        videoPlayer.Stop();
        isVideoPlaying = false;
        isVideoPaused = false;

        MediaItem currentItem = mediaItems[mediaIndex];

        if (currentItem.mediaType == MediaItem.MediaType.Image)
        {
            // 이미지 표시
            imageDisplay.gameObject.SetActive(true);
            imageDisplay.sprite = currentItem.image;
        }
        else if (currentItem.mediaType == MediaItem.MediaType.Video)
        {
            // 동영상 표시 (자동 재생하지 않음)
            videoDisplay.gameObject.SetActive(true);

            videoPlayer.Stop();                  // 동영상 재생 중지
            videoPlayer.clip = currentItem.video; // 동영상 클립 할당
            // videoPlayer.Play();               // 자동 재생 코드 제거
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
        isVideoPaused = false;
    }
}
