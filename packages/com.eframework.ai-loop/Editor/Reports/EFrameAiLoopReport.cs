using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameAiLoopReport
    {
        private static readonly string DefaultDirectory =
            Path.Combine(".eframe", "outputs", "Reports");

        public static string WriteScreenshotReport(EFrameScreenshotResult result, string title)
        {
            if (result == null || !result.Success)
            {
                return "";
            }

            return TryWrite("screenshot", builder =>
            {
                AppendHeader(builder, title);
                AppendLine(builder, "## 摘要");
                AppendBullet(builder, "状态", result.Success ? "成功" : "失败");
                AppendBullet(builder, "消息", result.Message);
                AppendBullet(builder, "尺寸", $"{result.Width} x {result.Height}");
                AppendBullet(builder, "坐标系", result.CoordinateSystem);
                AppendBullet(builder, "UI 元素数", result.Elements?.Count.ToString(CultureInfo.InvariantCulture) ?? "0");
                AppendLine(builder);

                AppendArtifacts(builder, result.Path, "", "");
                AppendUiElements(builder, result);
            });
        }

        public static string WriteInputRecordingReport(EFrameRecordInputResult result)
        {
            if (result == null || !result.Success)
            {
                return "";
            }

            return TryWrite("input-recording", builder =>
            {
                AppendHeader(builder, "AI Loop 输入录制报告");
                AppendLine(builder, "## 摘要");
                AppendBullet(builder, "状态", result.Success ? "成功" : "失败");
                AppendBullet(builder, "消息", result.Message);
                AppendBullet(builder, "总帧数", result.TotalFrames.ToString(CultureInfo.InvariantCulture));
                AppendBullet(builder, "时长", result.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s");
                AppendBullet(builder, "事件数", result.EventCount.ToString(CultureInfo.InvariantCulture));
                AppendLine(builder);

                AppendArtifacts(builder, "", result.OutputPath, "");
            });
        }

        public static string WriteReplayReport(EFrameReplayInputResult result, string title)
        {
            if (result == null || !result.Success)
            {
                return "";
            }

            return TryWrite("input-replay", builder =>
            {
                AppendHeader(builder, title);
                AppendLine(builder, "## 摘要");
                AppendBullet(builder, "状态", result.Success ? "成功" : "失败");
                AppendBullet(builder, "消息", result.Message);
                AppendBullet(builder, "当前帧", result.CurrentFrame.ToString(CultureInfo.InvariantCulture));
                AppendBullet(builder, "总帧数", result.TotalFrames.ToString(CultureInfo.InvariantCulture));
                AppendBullet(builder, "进度", result.Progress.ToString("0.###", CultureInfo.InvariantCulture));
                AppendBullet(builder, "回放中", result.IsReplaying ? "是" : "否");
                AppendLine(builder);

                AppendArtifacts(builder, "", result.InputPath, "");
            });
        }

        private static string TryWrite(string name, Action<StringBuilder> append)
        {
            try
            {
                Directory.CreateDirectory(DefaultDirectory);
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
                string path = Path.Combine(DefaultDirectory, $"{timestamp}-{name}.md");
                StringBuilder builder = new();
                append(builder);
                File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to write AI Loop report: {exception.Message}");
                return "";
            }
        }

        private static void AppendHeader(StringBuilder builder, string title)
        {
            AppendLine(builder, "# " + EscapeText(title));
            AppendLine(builder);
            AppendLine(builder, "> 生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            AppendLine(builder);
        }

        private static void AppendArtifacts(
            StringBuilder builder,
            string imagePath,
            string jsonPath,
            string reportPath)
        {
            AppendLine(builder, "## 产物");
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                string imageLink = ToMarkdownLink(DefaultDirectory, imagePath);
                AppendLine(builder, "![AI Loop 截图](" + imageLink + ")");
                AppendLine(builder);
                AppendLine(builder, "- 截图：" + ToMarkdownFileLink(DefaultDirectory, imagePath));
            }

            if (!string.IsNullOrWhiteSpace(jsonPath))
            {
                AppendLine(builder, "- JSON：" + ToMarkdownFileLink(DefaultDirectory, jsonPath));
            }

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                AppendLine(builder, "- 报告：" + ToMarkdownFileLink(DefaultDirectory, reportPath));
            }

            if (string.IsNullOrWhiteSpace(imagePath) &&
                string.IsNullOrWhiteSpace(jsonPath) &&
                string.IsNullOrWhiteSpace(reportPath))
            {
                AppendLine(builder, "- 无文件产物。");
            }

            AppendLine(builder);
        }

        private static void AppendUiElements(StringBuilder builder, EFrameScreenshotResult result)
        {
            if (result.Elements == null || result.Elements.Count == 0)
            {
                return;
            }

            AppendLine(builder, "## UI 元素");
            AppendLine(builder, "| 标签 | 名称 | 类型 | 交互 | 坐标 | Bounds | 路径 |");
            AppendLine(builder, "| --- | --- | --- | --- | --- | --- | --- |");
            foreach (EFrameUiElementInfo element in result.Elements)
            {
                string position = FormatFloat(element.X) + ", " + FormatFloat(element.Y);
                string bounds = FormatFloat(element.BoundsMinX) + ", " +
                                FormatFloat(element.BoundsMinY) + " - " +
                                FormatFloat(element.BoundsMaxX) + ", " +
                                FormatFloat(element.BoundsMaxY);
                AppendLine(
                    builder,
                    "| " + EscapeCell(element.Label) +
                    " | " + EscapeCell(element.Name) +
                    " | " + EscapeCell(element.Type) +
                    " | " + EscapeCell(element.Interaction) +
                    " | " + EscapeCell(position) +
                    " | " + EscapeCell(bounds) +
                    " | " + EscapeCell(element.Path) +
                    " |");
            }

            AppendLine(builder);
        }

        private static void AppendBullet(StringBuilder builder, string label, string value)
        {
            AppendLine(builder, "- " + label + "：" + EscapeText(value));
        }

        private static string ToMarkdownFileLink(string baseDirectory, string path)
        {
            string link = ToMarkdownLink(baseDirectory, path);
            return "[" + EscapeText(path) + "](" + link + ")";
        }

        private static string ToMarkdownLink(string baseDirectory, string path)
        {
            string relativePath = MakeRelativePath(baseDirectory, path).Replace('\\', '/');
            string escaped = relativePath
                .Replace(" ", "%20")
                .Replace("(", "%28")
                .Replace(")", "%29");
            return escaped;
        }

        private static string MakeRelativePath(string baseDirectory, string path)
        {
            string fullBase = Path.GetFullPath(baseDirectory);
            string fullPath = Path.GetFullPath(path);
            Uri baseUri = new(EnsureTrailingSeparator(fullBase));
            Uri fileUri = new(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
        }

        private static string EnsureTrailingSeparator(string path)
        {
            char separator = Path.DirectorySeparatorChar;
            if (path.EndsWith(separator.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + separator;
        }

        private static string EscapeCell(string value)
        {
            return EscapeText(value).Replace("|", "\\|");
        }

        private static string EscapeText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? ""
                : value.Replace("\r", " ").Replace("\n", " ");
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void AppendLine(StringBuilder builder, string value = "")
        {
            builder.AppendLine(value);
        }
    }
}
