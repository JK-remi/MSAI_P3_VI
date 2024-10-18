using System.Collections.Generic;

public class VideoListResponse
{
    public List<VideoItem> items;
}

public class VideoItem
{
    public LiveStreamingDetails liveStreamingDetails;
}

public class LiveStreamingDetails
{
    public string activeLiveChatId;
}

public class LiveChatMessageListResponse
{
    public List<LiveChatMessageItem> items;
    public string nextPageToken;
    public int pollingIntervalMillis;
}

public class LiveChatMessageItem
{
    public LiveChatSnippet snippet;
    public LiveChatAuthorDetails authorDetails;
}

public class LiveChatSnippet
{
    public string displayMessage;
    public string publishedAt;
}

public class LiveChatAuthorDetails
{
    public string displayName;
}
