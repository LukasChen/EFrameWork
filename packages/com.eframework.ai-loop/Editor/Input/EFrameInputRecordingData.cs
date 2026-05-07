using System;
using System.Collections.Generic;

namespace EFramework.Editor.AILoop
{
    [Serializable]
    public sealed class EFrameInputRecordingData
    {
        public EFrameInputRecordingMetadata metadata = new();
        public List<EFrameInputFrameEvents> frames = new();

        public int GetTotalEventCount()
        {
            int count = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                count += frames[i].events.Count;
            }

            return count;
        }
    }

    [Serializable]
    public sealed class EFrameInputRecordingMetadata
    {
        public string recordedAt = "";
        public string unityVersion = "";
        public string eframeVersion = "";
        public int screenWidth;
        public int screenHeight;
        public int totalFrames;
        public float durationSeconds;
    }

    [Serializable]
    public sealed class EFrameInputFrameEvents
    {
        public int frame;
        public List<EFrameRecordedInputEvent> events = new();
    }

    [Serializable]
    public sealed class EFrameRecordedInputEvent
    {
        public string type = "";
        public string data = "";
    }

    internal static class EFrameInputEventTypes
    {
        public const string KeyDown = "keyDown";
        public const string KeyUp = "keyUp";
        public const string MouseDown = "mouseDown";
        public const string MouseUp = "mouseUp";
        public const string MouseDelta = "mouseDelta";
        public const string MouseScroll = "mouseScroll";
        public const string MousePosition = "mousePosition";
    }
}
