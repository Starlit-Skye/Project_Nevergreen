using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Nevergreen.Data
{
    [Serializable]
    public class PartyMemberDTO
    {
        public string characterId;
        public int currentHP; // -1 represents null (max HP)
        public List<string> equippedSkillIds = new List<string>();
        public List<string> perfectionIds = new List<string>();
        public List<string> imperfectionIds = new List<string>();
    }

    [Serializable]
    public class SaveDataDTO
    {
        public bool hasActiveRun;
        public int roomProgression;
        public string nextRoomId;
        public string lastSelectedFormationId;
        public List<PartyMemberDTO> party = new List<PartyMemberDTO>();
    }

    /// <summary>
    /// Handles serialization and AES-256 encryption of the current RunSessionManager state.
    /// </summary>
    public static class SaveManager
    {
        private static string _customSavePath;
        private static string SavePath => _customSavePath ?? Path.Combine(Application.persistentDataPath, "save.dat");

        /// <summary>
        /// Sets a temporary save path override for unit tests.
        /// Pass null to restore the default production path.
        /// </summary>
        public static void SetSavePathForTesting(string path)
        {
            _customSavePath = path;
        }

        // Fixed obfuscated key for simple local AES encryption
        private static readonly byte[] Key = new byte[] {
            0x4F, 0x21, 0x7A, 0x98, 0xC3, 0x11, 0x4D, 0xBA,
            0x66, 0x20, 0xEE, 0x99, 0x01, 0x4B, 0x55, 0x1A,
            0x32, 0x67, 0x89, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE,
            0xFF, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77
        };
        private static readonly byte[] IV = new byte[] {
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
        };

        /// <summary>
        /// Maps the active run state to a JSON string, encrypts it, and writes to disk.
        /// </summary>
        public static void SaveRun()
        {
            var dto = new SaveDataDTO
            {
                hasActiveRun = true,
                roomProgression = RunSessionManager.RoomProgression,
                nextRoomId = RunSessionManager.NextRoomData != null ? RunSessionManager.NextRoomData.roomId : null,
                lastSelectedFormationId = RunSessionManager.LastSelectedFormation != null ? RunSessionManager.LastSelectedFormation.formationId : null,
                party = new List<PartyMemberDTO>()
            };

            foreach (var member in RunSessionManager.CurrentParty)
            {
                if (member == null) continue;
                
                var pDto = new PartyMemberDTO
                {
                    characterId = member.character != null ? member.character.characterId : null,
                    currentHP = member.currentHP ?? -1,
                    equippedSkillIds = member.equippedSkills.Where(s => s != null).Select(s => s.skillId).ToList(),
                    perfectionIds = member.perfections.Where(t => t != null).Select(t => t.traitId).ToList(),
                    imperfectionIds = member.imperfections.Where(t => t != null).Select(t => t.traitId).ToList()
                };
                dto.party.Add(pDto);
            }

            string json = JsonUtility.ToJson(dto, false);
            byte[] encryptedData = EncryptAES(json);
            File.WriteAllBytes(SavePath, encryptedData);
            
            Debug.Log($"[SaveManager] Run saved successfully to {SavePath}");
        }

        /// <summary>
        /// Reads the encrypted save file, decrypts it, and populates RunSessionManager.
        /// </summary>
        public static bool LoadRun()
        {
            if (!File.Exists(SavePath)) return false;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(SavePath);
                string json = DecryptAES(encryptedData);
                SaveDataDTO dto = JsonUtility.FromJson<SaveDataDTO>(json);

                if (!dto.hasActiveRun)
                {
                    return false;
                }

                var db = GameDatabase.Instance;
                if (db == null)
                {
                    Debug.LogError("[SaveManager] Cannot load run: GameDatabase instance is null.");
                    return false;
                }

                RunSessionManager.Clear();
                RunSessionManager.RoomProgression = dto.roomProgression;

                // Lookup NextRoomData
                if (!string.IsNullOrEmpty(dto.nextRoomId) && db.RoomDatabase != null)
                {
                    RunSessionManager.NextRoomData = db.RoomDatabase.availableRooms.FirstOrDefault(r => r != null && r.roomId == dto.nextRoomId);
                }

                // Lookup LastSelectedFormation
                if (!string.IsNullOrEmpty(dto.lastSelectedFormationId) && db.EnemyFormationDatabase != null)
                {
                    var allFormations = new List<EnemyFormationData>();
                    allFormations.AddRange(db.EnemyFormationDatabase.trivialFormations);
                    allFormations.AddRange(db.EnemyFormationDatabase.earlyGameFormations);
                    allFormations.AddRange(db.EnemyFormationDatabase.midGameFormations);
                    allFormations.AddRange(db.EnemyFormationDatabase.lateGameFormations);
                    
                    var formation = allFormations.FirstOrDefault(f => f != null && f.formationId == dto.lastSelectedFormationId);
                    if (formation != null)
                    {
                        RunSessionManager.LastSelectedFormation = formation;
                        RunSessionManager.ShouldUseSavedFormation = true;
                    }
                }

                // Lookup Party
                foreach (var pDto in dto.party)
                {
                    var member = new PartyMemberInfo();
                    
                    // Lookup Character
                    if (!string.IsNullOrEmpty(pDto.characterId))
                    {
                        if (db.GlobalConfig != null && db.GlobalConfig.ceciliaData != null && db.GlobalConfig.ceciliaData.characterId == pDto.characterId)
                        {
                            member.character = db.GlobalConfig.ceciliaData;
                        }
                        else if (db.MarionetteDatabase != null)
                        {
                            member.character = db.MarionetteDatabase.marionettes.FirstOrDefault(m => m != null && m.characterId == pDto.characterId);
                        }
                    }

                    member.currentHP = pDto.currentHP == -1 ? (int?)null : pDto.currentHP;

                    // Lookup Traits
                    if (db.TraitDatabase != null)
                    {
                        foreach (string tId in pDto.perfectionIds)
                        {
                            var trait = db.TraitDatabase.perfections.FirstOrDefault(t => t != null && t.traitId == tId);
                            if (trait != null) member.perfections.Add(trait);
                        }
                        foreach (string tId in pDto.imperfectionIds)
                        {
                            var trait = db.TraitDatabase.imperfections.FirstOrDefault(t => t != null && t.traitId == tId);
                            if (trait != null) member.imperfections.Add(trait);
                        }
                    }

                    // Lookup Skills
                    // We search through Cecilia's and the Marionette's total pools to find the exact skill.
                    var availableSkillPool = new List<SkillData>();
                    if (member.character != null)
                    {
                        if (member.character.totalSkillPool != null) availableSkillPool.AddRange(member.character.totalSkillPool);
                        if (member.character.availableSkills != null) availableSkillPool.AddRange(member.character.availableSkills);
                    }

                    foreach (string sId in pDto.equippedSkillIds)
                    {
                        var skill = availableSkillPool.FirstOrDefault(s => s != null && s.skillId == sId);
                        if (skill != null) member.equippedSkills.Add(skill);
                    }

                    RunSessionManager.CurrentParty.Add(member);
                }

                Debug.Log("[SaveManager] Run loaded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load run: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets hasActiveRun to false and clears run state, persisting the file for analytics or meta-progression.
        /// </summary>
        public static void ClearActiveRun()
        {
            var dto = new SaveDataDTO { hasActiveRun = false };
            
            // We could preserve existing meta-progression data here if we had any.
            
            string json = JsonUtility.ToJson(dto, false);
            byte[] encryptedData = EncryptAES(json);
            File.WriteAllBytes(SavePath, encryptedData);
            
            Debug.Log("[SaveManager] Active run cleared.");
        }

        /// <summary>
        /// Checks if there is an ongoing run without loading the full state.
        /// </summary>
        public static bool HasSavedRun()
        {
            if (!File.Exists(SavePath)) return false;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(SavePath);
                string json = DecryptAES(encryptedData);
                SaveDataDTO dto = JsonUtility.FromJson<SaveDataDTO>(json);
                return dto.hasActiveRun;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] EncryptAES(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return ms.ToArray();
                }
            }
        }

        private static string DecryptAES(byte[] cipherData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream(cipherData))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
