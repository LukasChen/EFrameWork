using TMPro;
using UnityEngine;

namespace EFramework.Runtime.UI.UIHelper
{
    //MIT License

    //Copyright(c) 2019 Antony Vitillo(a.k.a. "Skarredghost")

    //Permission is hereby granted, free of charge, to any person obtaining a copy
    //of this software and associated documentation files (the "Software"), to deal
    //in the Software without restriction, including without limitation the rights
    //to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    //copies of the Software, and to permit persons to whom the Software is
    //furnished to do so, subject to the following conditions:

    //The above copyright notice and this permission notice shall be included in all
    //copies or substantial portions of the Software.

    //THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    //IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    //FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    //AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    //LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    //OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    //SOFTWARE.
    /// <summary>
    ///     Class for drawing a Text Pro text following a circle arc
    /// </summary>
    [ExecuteInEditMode]
    public class TextProOnACircle : TextProOnACurve
    {
        /// <summary>
        ///     The radius of the text circle arc
        /// </summary>
        [SerializeField]
        [Tooltip("The radius of the text circle arc")]
        protected float m_radius = 10.0f;
        private int m_oldMaxDegreesPerLetter = int.MaxValue;
        // 自动适配，不再需要手动指定圆弧角度

        /// <summary>
        ///     The angular offset at which the arc should be centered, in degrees.
        ///     -90 degrees means that the text is centered on the heighest point
        /// </summary>
        [SerializeField]
        [Tooltip("The angular offset at which the arc should be centered, in degrees")]
        private float m_angularOffset = -90;

        /// <summary>
        ///     How many maximum degrees per letters should be. For instance, if you specify
        ///     10 degrees, the distance between the letters will never be superior to 10 degrees.
        ///     It is useful to create text that gracefully expands until it reaches the full arc,
        ///     without making the letters to sparse when the string is short
        /// </summary>
        [SerializeField]
        [Tooltip("The maximum angular distance between letters, in degrees")]
        private int m_maxDegreesPerLetter = 360;

        /// <summary>
        ///     Previous value of <see cref="m_angularOffset" />
        /// </summary>
        private float m_oldAngularOffset = float.MaxValue;

        /// <summary>
        ///     Previous value of <see cref="m_maxDegreesPerLetter" />
    // 移除 m_arcDegrees 相关的旧值记录
        ///     Previous value of <see cref="m_radius" />
        /// </summary>
        private float m_oldRadius = float.MaxValue;

        /// <summary>
        ///     Method executed at every frame that checks if some parameters have been changed
        /// </summary>
        /// <returns></returns>
        protected override bool ParametersHaveChanged()
        {
            //check if paramters have changed and update the old values for next frame iteration
            bool retVal = m_radius != m_oldRadius || m_angularOffset != m_oldAngularOffset ||
                          m_oldMaxDegreesPerLetter != m_maxDegreesPerLetter;

            m_oldRadius = m_radius;
            m_oldAngularOffset = m_angularOffset;
            m_oldMaxDegreesPerLetter = m_maxDegreesPerLetter;

            return retVal;
        }

        /// <summary>
        ///     Computes the transformation matrix that maps the offsets of the vertices of each single character from
        ///     the character's center to the final destinations of the vertices so that the text follows a curve
        /// </summary>
        /// <param name="charMidBaselinePosfloat">Position of the central point of the character</param>
        /// <param name="zeroToOnePos">Horizontal position of the character relative to the bounds of the box, in a range [0, 1]</param>
        /// <param name="textInfo">Information on the text that we are showing</param>
        /// <param name="charIdx">Index of the character we have to compute the transformation for</param>
        /// <returns>Transformation matrix to be applied to all vertices of the text</returns>
        protected override Matrix4x4 ComputeTransformationMatrix(Vector3 charMidBaselinePos, float zeroToOnePos, TMP_TextInfo textInfo,
            int charIdx)
        {
            // 统计所有可见字符的宽度总和
            float totalWidth = textInfo.textComponent.bounds.size.x;
            // 计算实际圆弧角度（弧度 -> 角度），保留最大角度限制
            float arcRadians = totalWidth / m_radius;
            float arcDegrees = Mathf.Min(arcRadians * Mathf.Rad2Deg, textInfo.characterCount / textInfo.lineCount * m_maxDegreesPerLetter);

            // 计算当前字符在圆弧上的角度
            float angle = ((zeroToOnePos - 0.5f) * arcDegrees + m_angularOffset) * Mathf.Deg2Rad;

            float x0 = Mathf.Cos(angle);
            float y0 = Mathf.Sin(angle);
            float radiusForThisLine = m_radius - textInfo.lineInfo[0].lineExtents.max.y * textInfo.characterInfo[charIdx].lineNumber;
            Vector2 newMideBaselinePos = new(x0 * radiusForThisLine, -y0 * radiusForThisLine);

            return Matrix4x4.TRS(new Vector3(newMideBaselinePos.x, newMideBaselinePos.y, 0),
                Quaternion.AngleAxis(-Mathf.Atan2(y0, x0) * Mathf.Rad2Deg - 90, Vector3.forward), Vector3.one);
        }
    }
}
