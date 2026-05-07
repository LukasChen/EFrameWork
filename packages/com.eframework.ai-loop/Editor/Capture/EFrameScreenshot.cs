using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.AILoop
{
    public static class EFrameScreenshot
    {
        public static async Task<EFrameScreenshotResult> CaptureAsync(
            EFrameScreenshotOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= new EFrameScreenshotOptions();
            ValidateOptions(options);

            if (options.CaptureMode == EFrameCaptureMode.EditorWindow)
            {
                return await CaptureEditorWindowAsync(options, cancellationToken);
            }

            return await CaptureGameRenderingAsync(options, cancellationToken);
        }

        private static async Task<EFrameScreenshotResult> CaptureEditorWindowAsync(
            EFrameScreenshotOptions options,
            CancellationToken cancellationToken)
        {
            EditorWindow window = EFrameEditorWindowCaptureUtility.FindWindowByName(options.WindowName);
            if (window == null)
            {
                return new EFrameScreenshotResult
                {
                    Success = false,
                    Message = $"EditorWindow '{options.WindowName}' was not found."
                };
            }

            Texture2D texture = await EFrameEditorWindowCaptureUtility.CaptureWindowAsync(
                window,
                options.ResolutionScale,
                cancellationToken);

            if (texture == null)
            {
                return new EFrameScreenshotResult
                {
                    Success = false,
                    Message = $"Failed to capture EditorWindow '{options.WindowName}'."
                };
            }

            return SaveTexture(texture, ResolveOutputPath(options.OutputDirectory, options.WindowName));
        }

        private static async Task<EFrameScreenshotResult> CaptureGameRenderingAsync(
            EFrameScreenshotOptions options,
            CancellationToken cancellationToken)
        {
            if (!EditorApplication.isPlaying)
            {
                return new EFrameScreenshotResult
                {
                    Success = false,
                    Message = "Game rendering capture requires PlayMode."
                };
            }

            List<EFrameUiElementInfo> elements = new();
            GameObject annotationOverlay = null;
            try
            {
                if (options.AnnotateElements)
                {
                    elements = EFrameUiElementAnnotator.CollectInteractiveElements();
                    EFrameUiElementAnnotator.AssignLabels(elements);
                }

                if (options.ElementsOnly)
                {
                    EFrameUiElementAnnotator.ConvertToTopLeftCoordinates(elements, GetGameViewHeight());
                    return new EFrameScreenshotResult
                    {
                        Success = true,
                        Message = $"Collected {elements.Count} interactive UI elements.",
                        Elements = elements
                    };
                }

                if (options.AnnotateElements)
                {
                    annotationOverlay = EFrameUiElementAnnotator.CreateAnnotationOverlay(
                        elements,
                        options.ResolutionScale);
                    Canvas.ForceUpdateCanvases();
                    await EFrameAiLoopDelay.DelayFrames(2, cancellationToken);
                }

                (Texture2D texture, int yOffset) =
                    await EFrameEditorWindowCaptureUtility.CaptureGameRenderingAsync(
                        options.ResolutionScale,
                        cancellationToken);

                if (texture == null)
                {
                    return new EFrameScreenshotResult
                    {
                        Success = false,
                        Message = "Game View render texture is unavailable. Open Game View and wait for one frame."
                    };
                }

                EFrameScreenshotResult result =
                    SaveTexture(texture, ResolveOutputPath(options.OutputDirectory, "GameRendering"));
                EFrameUiElementAnnotator.ConvertToTopLeftCoordinates(elements, GetGameViewHeight());
                result.Elements = elements;
                result.YOffset = yOffset;
                return result;
            }
            finally
            {
                if (annotationOverlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(annotationOverlay);
                }
            }
        }

        private static EFrameScreenshotResult SaveTexture(Texture2D texture, string path)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? ".");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                FileInfo fileInfo = new(path);
                return new EFrameScreenshotResult
                {
                    Success = true,
                    Message = $"Screenshot saved: {path}",
                    Path = path,
                    FileSizeBytes = fileInfo.Length,
                    Width = texture.width,
                    Height = texture.height
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ResolveOutputPath(string outputDirectory, string name)
        {
            string directory = string.IsNullOrEmpty(outputDirectory)
                ? System.IO.Path.Combine(".eframe", "outputs", "Screenshots")
                : outputDirectory;
            string safeName = string.Join("_", name.Split(System.IO.Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            return System.IO.Path.Combine(directory, $"{safeName}_{timestamp}.png");
        }

        private static int GetGameViewHeight()
        {
            return Mathf.RoundToInt(Handles.GetMainGameViewSize().y);
        }

        private static void ValidateOptions(EFrameScreenshotOptions options)
        {
            if (options.ResolutionScale < 0.1f || options.ResolutionScale > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.ResolutionScale),
                    options.ResolutionScale,
                    "ResolutionScale must be between 0.1 and 1.0.");
            }

            if (options.CaptureMode == EFrameCaptureMode.EditorWindow &&
                string.IsNullOrWhiteSpace(options.WindowName))
            {
                throw new ArgumentException("WindowName is required for EditorWindow capture.");
            }

            if (options.ElementsOnly && !options.AnnotateElements)
            {
                throw new ArgumentException("ElementsOnly requires AnnotateElements.");
            }
        }
    }
}
