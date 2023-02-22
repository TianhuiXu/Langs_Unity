// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public abstract class TextRevealBehaviour
    {
        public bool Revealing => revealState.InProgress;
        public int LastRevealedCharIndex { get; private set; }

        private static readonly int lineClipRectPropertyId = Shader.PropertyToID("_LineClipRect");
        private static readonly int charClipRectPropertyId = Shader.PropertyToID("_CharClipRect");
        private static readonly int charFadeWidthPropertyId = Shader.PropertyToID("_CharFadeWidth");
        private static readonly int charSlantAnglePropertyId = Shader.PropertyToID("_CharSlantAngle");

        private readonly TextRevealState revealState = new TextRevealState();
        private readonly MaskableGraphic graphic;
        private readonly RectTransform contentTransform;
        private readonly bool slideClipRect;
        private readonly bool rightToLeft;
        private readonly float fadeWidth;

        private int lastCharIndex => GetCharacterCount() - 1;
        private RectTransform canvasTransform => canvasTransformCache ? canvasTransformCache : canvasTransformCache = graphic.canvas.GetComponent<RectTransform>();
        private float slideProgress => slideClipRect && lastRevealDuration > 0 ? Mathf.Clamp01((Time.time - lastRevealTime) / lastRevealDuration) : 1f;
        private Vector3 scale => (contentTransform ? contentTransform.localScale : Vector3.one) * GetScaleModifier();

        private readonly Vector3[] worldCorners = new Vector3[4];
        private readonly Vector3[] canvasCorners = new Vector3[4];

        private RectTransform canvasTransformCache;
        private Vector4 curLineClipRect, curCharClipRect;
        private float curCharFadeWidth, curCharSlantAngle;
        private float lastRevealDuration, lastRevealTime, lastCharClipPos, lastCharFadeWidth;
        private RevealableCharacter revealStartChar = RevealableCharacter.Invalid;
        private bool rebuildPending;
        private float revealAfterRebuild = -1;

        protected TextRevealBehaviour (MaskableGraphic graphic, bool slideClipRect, bool rightToLeft, float fadeWidth)
        {
            this.graphic = graphic;
            this.slideClipRect = slideClipRect;
            this.rightToLeft = rightToLeft;
            this.fadeWidth = fadeWidth;

            var printerPanel = graphic.GetComponentInParent<UITextPrinterPanel>();
            if (printerPanel) contentTransform = printerPanel.Content;
        }

        public float GetRevealProgress ()
        {
            var result = 0f;
            if (lastCharIndex <= 0) result = LastRevealedCharIndex >= 0 ? 1f : 0f;
            else result = Mathf.Clamp01(LastRevealedCharIndex / (float)lastCharIndex);
            if (rebuildPending) result = Mathf.Clamp(result, 0, .999f);
            return result;
        }

        public void SetRevealProgress (float revealProgress)
        {
            if (revealProgress >= 1) RevealAll();
            else if (revealProgress <= 0) HideAll();
            else if (rebuildPending) revealAfterRebuild = revealProgress;
            else SetLastRevealedCharIndex(Mathf.CeilToInt(lastCharIndex * revealProgress));
        }

        public void RevealNextChars (int count, float duration, AsyncToken asyncToken)
        {
            revealState.Start(count, duration, asyncToken);
        }

        public Vector2 GetLastRevealedCharPosition ()
        {
            if (!IsCharIndexValid(LastRevealedCharIndex)) return default;

            var lastChar = GetCharacterAt(LastRevealedCharIndex);
            var lastLine = GetLineAt(lastChar.LineIndex);
            var localPos = new Vector2(rightToLeft ? curCharClipRect.z : curCharClipRect.x, curCharClipRect.w - lastLine.Height * scale.y);
            return canvasTransform.TransformPoint(localPos);
        }

        public void WaitForRebuild ()
        {
            rebuildPending = true;
        }

        public void Rebuild ()
        {
            // Set current last revealed char as the start position for the reveal effect to 
            // prevent it from affecting this char again when resuming the revealing without resetting the text.
            if (GetRevealProgress() == 0 || !IsCharIndexValid(LastRevealedCharIndex))
                revealStartChar = RevealableCharacter.Invalid; // Prevent flickering when starting to reveal first line.
            else revealStartChar = GetCharacterAt(LastRevealedCharIndex);

            rebuildPending = false;

            if (!Mathf.Approximately(revealAfterRebuild, -1))
            {
                SetRevealProgress(revealAfterRebuild);
                revealAfterRebuild = -1;
            }

            Render();
        }

        public void Render ()
        {
            UpdateRevealState();
            UpdateClipRects();

            if (slideClipRect)
            {
                var charClipPos = Mathf.Lerp(lastCharClipPos, rightToLeft ? curCharClipRect.z : curCharClipRect.x, slideProgress);
                var slidedCharClipRect = rightToLeft
                    ? new Vector4(curCharClipRect.x, curCharClipRect.y, charClipPos, curCharClipRect.w)
                    : new Vector4(charClipPos, curCharClipRect.y, curCharClipRect.z, curCharClipRect.w);
                var slidedFadeWidth = Mathf.Lerp(lastCharFadeWidth, curCharFadeWidth, slideProgress);
                SetMaterialProperties(curLineClipRect, slidedCharClipRect, slidedFadeWidth, curCharSlantAngle);
            }
            else SetMaterialProperties(curLineClipRect, curCharClipRect, curCharFadeWidth, curCharSlantAngle);
        }

        public void UpdateClipRects ()
        {
            if (LastRevealedCharIndex >= GetCharacterCount()) return;

            var fullClipRect = GetTextCornersInCanvasSpace();

            if (LastRevealedCharIndex < 0) // Hide all.
            {
                curLineClipRect = curCharClipRect = fullClipRect;
                ScaleClipRects();
                return;
            }

            var currentChar = GetCharacterAt(LastRevealedCharIndex);
            var currentLine = GetLineAt(currentChar.LineIndex);
            var lineFirstChar = GetCharacterAt(currentLine.FirstCharIndex);
            var lineLastChar = GetCharacterAt(currentLine.LastCharIndex);

            var rectSize = GetTextRectSize();
            var clipPosY = currentLine.Ascender + (graphic.rectTransform.pivot.y - 1f) * rectSize.y;
            var clipPosX = currentChar.RightX + graphic.rectTransform.pivot.x * rectSize.x;

            curLineClipRect = fullClipRect + new Vector4(0, 0, 0, (clipPosY - currentLine.Height) * scale.y);
            curCharClipRect = rightToLeft
                ? fullClipRect + new Vector4(0, 0, (clipPosX - lineFirstChar.RightX) * scale.x, clipPosY * scale.y)
                : fullClipRect + new Vector4(clipPosX * scale.x, 0, 0, clipPosY * scale.y);
            curCharClipRect.y = curLineClipRect.w;
            ScaleClipRects();

            var startPos = currentChar.LineIndex == revealStartChar.LineIndex
                ? revealStartChar.LeftX
                : rightToLeft ? float.PositiveInfinity : float.NegativeInfinity;
            var startLimit = rightToLeft ? startPos - currentChar.LeftX : currentChar.LeftX - startPos;
            var endLimit = rightToLeft ? currentChar.RightX - lineLastChar.RightX : lineLastChar.RightX - currentChar.RightX;
            var widthLimit = Mathf.Max(0, Mathf.Min(startLimit, endLimit));
            curCharFadeWidth = Mathf.Clamp(fadeWidth, 0f, widthLimit) * scale.x;

            curCharSlantAngle = currentChar.SlantAngle;
        }

        public void DrawClipRects ()
        {
            if (Camera.current != Engine.GetService<ICameraManager>().UICamera) return;

            var (lineCenter, lineSize) = GetDrawDimensions(curLineClipRect);
            var (charCenter, charSize) = GetDrawDimensions(curCharClipRect);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(lineCenter, lineSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(charCenter, charSize);

            (Vector2 center, Vector2 size) GetDrawDimensions (Vector4 rect)
            {
                var center = new Vector2(rect.x + rect.z, rect.y + rect.w) / 2;
                var size = new Vector2(Mathf.Abs(rect.z - rect.x), Mathf.Abs(rect.w - rect.y));
                return (canvasTransform.TransformPoint(center), canvasTransform.TransformPoint(size));
            }
        }

        protected abstract int GetCharacterCount ();
        protected abstract RevealableCharacter GetCharacterAt (int index);
        protected abstract RevealableLine GetLineAt (int index);
        protected abstract IReadOnlyList<Material> GetMaterials ();
        protected abstract Vector2 GetTextRectSize ();
        protected abstract Vector4 GetClipRectScale ();
        protected virtual float GetScaleModifier () => 1;

        private void ScaleClipRects ()
        {
            var clipScale = GetClipRectScale();
            curCharClipRect.Scale(clipScale);
            curLineClipRect.Scale(clipScale);
        }

        private void UpdateRevealState ()
        {
            if (!revealState.InProgress || revealState.AsyncToken.Canceled) return;

            if (LastRevealedCharIndex >= lastCharIndex)
            {
                revealState.Reset();
                return;
            }

            // While rebuild is pending, we can't rely on char indexes, so wait.
            if (rebuildPending) return;

            // Wait while the clip rects are slided over currently revealed character.
            if (slideClipRect && slideProgress < 1) return;

            if (revealState.CharactersRevealed == revealState.CharactersToReveal)
            {
                revealState.Reset();
                return;
            }

            lastRevealDuration = Mathf.Max(revealState.RevealDuration, 0);
            lastRevealTime = Time.time;

            SetLastRevealedCharIndex(LastRevealedCharIndex + 1);

            revealState.CharactersRevealed++;
        }

        private void RevealAll ()
        {
            if (rebuildPending) revealAfterRebuild = 1f;
            else SetLastRevealedCharIndex(lastCharIndex);
            lastRevealDuration = 0f; // Force the slide to complete instantly.
            revealState.Reset();
        }

        private void HideAll ()
        {
            SetLastRevealedCharIndex(-1);
            lastRevealDuration = 0f; // Force the slide to complete instantly.
            revealStartChar = RevealableCharacter.Invalid; // Invalidate the reveal start position.
            Render(); // Otherwise the unrevealed yet text could be visible for a moment.
            revealState.Reset();
        }

        private void SetMaterialProperties (Vector4 lineClipRect, Vector4 charClipRect, float charFadeWidth, float charSlantAngle)
        {
            var materials = GetMaterials();
            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].SetVector(lineClipRectPropertyId, lineClipRect);
                materials[i].SetVector(charClipRectPropertyId, charClipRect);
                materials[i].SetFloat(charFadeWidthPropertyId, charFadeWidth);
                materials[i].SetFloat(charSlantAnglePropertyId, charSlantAngle);
            }
        }

        private void SetLastRevealedCharIndex (int charIndex)
        {
            if (LastRevealedCharIndex == charIndex) return;

            var curChar = IsCharIndexValid(LastRevealedCharIndex) ? GetCharacterAt(LastRevealedCharIndex) : RevealableCharacter.Invalid;
            var nextChar = IsCharIndexValid(charIndex) ? GetCharacterAt(charIndex) : RevealableCharacter.Invalid;

            // Skip chars when (while at the same line) the caret is moving back (eg, when using ruby text).
            if (!rightToLeft && charIndex > 0 && nextChar.LineIndex == curChar.LineIndex && charIndex > LastRevealedCharIndex)
                SkipBackwardCharacters();

            lastCharClipPos = rightToLeft
                ? curChar.LineIndex < 0 ? curLineClipRect.z : curCharClipRect.z
                : curChar.LineIndex < 0 ? curLineClipRect.x : curCharClipRect.x;
            lastCharFadeWidth = curCharFadeWidth;

            LastRevealedCharIndex = charIndex;

            if (slideClipRect && curChar.LineIndex != nextChar.LineIndex)
                ResetSlide();

            void SkipBackwardCharacters ()
            {
                while (nextChar.LineIndex == curChar.LineIndex && nextChar.LeftX < curChar.RightX && charIndex < lastCharIndex)
                {
                    charIndex++;
                    nextChar = GetCharacterAt(charIndex);
                }

                // Last char is still behind the previous one; use pos. of the previous.
                if (nextChar.LineIndex == curChar.LineIndex && nextChar.LeftX < curChar.RightX)
                    nextChar = curChar;
            }
        }

        private void ResetSlide ()
        {
            lastCharClipPos = rightToLeft ? GetTextCornersInCanvasSpace().z : GetTextCornersInCanvasSpace().x;
            lastCharFadeWidth = curCharFadeWidth;
        }

        private Vector4 GetTextCornersInCanvasSpace ()
        {
            graphic.rectTransform.GetWorldCorners(worldCorners);
            for (int i = 0; i < 4; ++i)
                canvasCorners[i] = canvasTransform.InverseTransformPoint(worldCorners[i]);
            // Positions of diagonal corners.
            return new Vector4(canvasCorners[0].x, canvasCorners[0].y, canvasCorners[2].x, canvasCorners[2].y);
        }

        private bool IsCharIndexValid (int charIndex) => charIndex >= 0 && charIndex < GetCharacterCount();
    }
}
