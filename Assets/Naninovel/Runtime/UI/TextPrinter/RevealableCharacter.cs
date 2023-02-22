// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel.UI
{
    public readonly struct RevealableCharacter : IEquatable<RevealableCharacter>
    {
        public static readonly RevealableCharacter Invalid = new RevealableCharacter(-1, -1, -1, -1, -1);

        public readonly int CharIndex;
        public readonly int LineIndex;
        public readonly float LeftX;
        public readonly float RightX;
        public readonly float SlantAngle;

        public RevealableCharacter (int charIndex, int lineIndex, float leftX, float rightX, float slantAngle)
        {
            CharIndex = charIndex;
            LineIndex = lineIndex;
            LeftX = leftX;
            RightX = rightX;
            SlantAngle = slantAngle;
        }

        public bool Equals (RevealableCharacter other) => CharIndex == other.CharIndex && LineIndex == other.LineIndex;
        public override bool Equals (object obj) => obj is RevealableCharacter other && Equals(other);
        public static bool operator == (RevealableCharacter left, RevealableCharacter right) => left.Equals(right);
        public static bool operator != (RevealableCharacter left, RevealableCharacter right) => !left.Equals(right);
        public override int GetHashCode ()
        {
            unchecked
            {
                return (CharIndex * 397) ^ LineIndex;
            }
        }
    }
}
