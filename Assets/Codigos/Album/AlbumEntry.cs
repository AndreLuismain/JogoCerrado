using System;
using UnityEngine;
using Cerrado.Data;

namespace Cerrado.Album
{
    [Serializable]
    public class AlbumEntry
    {
        public SpeciesData Species;
        public bool IsDiscovered;
        public int TimesPhotographed;
        public int BestScore;
        public int BestStars;
        public string CaptureDate;
        public string PhotoFileName;
        [NonSerialized] public Texture2D CachedTexture;

        public AlbumEntry(SpeciesData species)
        {
            Species = species;
            IsDiscovered = false;
            TimesPhotographed = 0;
            BestScore = 0;
            BestStars = 0;
            CaptureDate = "";
            PhotoFileName = "";
            CachedTexture = null;
        }
    }
}

