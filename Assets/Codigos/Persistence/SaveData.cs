using System;
using System.Collections.Generic;

namespace Cerrado.Persistence
{
    [Serializable]
    public class SavedSpeciesEntry
    {
        public string SpeciesId;
        public int BestScore;
        public int BestStars;
        public int TimesPhotographed;
        public string PhotoFileName;
        public string CaptureDate;

        public SavedSpeciesEntry()
        {
            TimesPhotographed = 0;
            BestScore = 0;
            BestStars = 0;
            CaptureDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }
    }

    [Serializable]
    public class GameSaveData
    {
        public int PlayerCoins = 0;
        public int TotalPhotosTaken = 0;
        public List<SavedSpeciesEntry> DiscoveredSpecies = new List<SavedSpeciesEntry>();
        public List<string> CompletedQuestIds = new List<string>();
        public List<string> UnlockedEquipmentIds = new List<string>();
        public string LastSaveTimestamp = "";

        public GameSaveData()
        {
            LastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}

