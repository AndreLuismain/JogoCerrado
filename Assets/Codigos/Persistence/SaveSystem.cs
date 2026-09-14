using System;
using System.IO;
using UnityEngine;

namespace Cerrado.Persistence
{
    public static class SaveSystem
    {
        private const string SaveFileName = "cerrado_save.json";
        private const string PhotosFolder = "Photos";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string PhotosDirectoryPath => Path.Combine(Application.persistentDataPath, PhotosFolder);

        public static void SaveGame(GameSaveData data)
        {
            try
            {
                data.LastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveSystem] Jogo salvo com sucesso em: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Falha ao salvar o jogo: {ex.Message}");
            }
        }

        public static GameSaveData LoadGame()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    Debug.Log("[SaveSystem] Nenhum arquivo de save existente. Criando novo jogo.");
                    return new GameSaveData();
                }

                string json = File.ReadAllText(SaveFilePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                Debug.Log("[SaveSystem] Dados carregados com sucesso.");
                return data ?? new GameSaveData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Erro ao carregar save. Inicializando padrão: {ex.Message}");
                return new GameSaveData();
            }
        }

        public static bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }

                if (Directory.Exists(PhotosDirectoryPath))
                {
                    Directory.Delete(PhotosDirectoryPath, true);
                }
                Debug.Log("[SaveSystem] Save e fotos excluídos com sucesso.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Erro ao deletar save: {ex.Message}");
            }
        }

        /// <summary>
        /// Salva uma Texture2D no disco em formato JPG e retorna o nome do arquivo.
        /// </summary>
        public static string SavePhotoToDisk(string speciesId, Texture2D texture)
        {
            if (texture == null) return string.Empty;

            try
            {
                if (!Directory.Exists(PhotosDirectoryPath))
                {
                    Directory.CreateDirectory(PhotosDirectoryPath);
                }

                string fileName = $"photo_{speciesId}.jpg";
                string fullPath = Path.Combine(PhotosDirectoryPath, fileName);

                byte[] bytes = texture.EncodeToJPG(85);
                File.WriteAllBytes(fullPath, bytes);

                return fileName;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Erro ao salvar foto no disco: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Carrega uma foto do disco para Texture2D.
        /// </summary>
        public static Texture2D LoadPhotoFromDisk(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            try
            {
                string fullPath = Path.Combine(PhotosDirectoryPath, fileName);
                if (!File.Exists(fullPath)) return null;

                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Erro ao carregar foto do disco: {ex.Message}");
                return null;
            }
        }
    }
}

