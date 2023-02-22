// Copyright 2022 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    public interface IVoiceoverDocumentGenerator
    {
        void GenerateVoiceoverDocument (ScriptPlaylist list, string locale, string outDir);
    }
}
