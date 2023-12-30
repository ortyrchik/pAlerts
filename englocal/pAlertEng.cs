using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Oxide.Core;


namespace Oxide.Plugins
{
    [Info("pAlert", "promise", "1.0.0")]
    [Description("Alert telegram when raid")]
    class pAlert : RustPlugin
    {
        private string BotToken;
        private Dictionary<string, string> UserTelegramIDs;
        private Dictionary<string, string> VerificationCodes;
        private Dictionary<string, string> ActiveVerificationCodes = new Dictionary<string, string>();


        protected override void LoadDefaultConfig()
        {
            if (Config["BotToken"] == null)
            {
                Config.Set("BotToken", "ваш_токен");
                SaveConfig();
            }
        }

        private T GetConfig<T>(string key, T defaultValue)
        {
            if (Config[key] == null)
                return defaultValue;
            return Config.ConvertValue<T>(Config[key]);
        }

        private void Init()
        {
            LoadDefaultConfig();
            LoadData();
        }

        private void LoadData()
        {
            UserTelegramIDs = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("pAlert_telegramIds");
            if (UserTelegramIDs == null)
            {
                UserTelegramIDs = new Dictionary<string, string>();
            }

            VerificationCodes = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("pAlert_verificationCodes");
            if (VerificationCodes == null)
            {
                VerificationCodes = new Dictionary<string, string>();
            }

            // Теперь устанавливаем токен из конфига только если он отсутствует
            if (Config["BotToken"] == null)
            {
                Config.Set("BotToken", "ваш_токен");
                SaveConfig();
            }
            else
            {
                BotToken = Config.Get<string>("BotToken");
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("pAlert_telegramIds", UserTelegramIDs);
            Interface.Oxide.DataFileSystem.WriteObject("pAlert_verificationCodes", VerificationCodes);
        }

        private void SendTelegramMessage(string chatId, string message)
        {

            string baseUrl = "https://api.telegram.org";
            string sendMessageUrl = $"{baseUrl}/bot{BotToken}/sendMessage";
            string queryParams = $"chat_id={chatId}&text={WebUtility.UrlEncode(message)}";
            string fullUrl = $"{sendMessageUrl}?{queryParams}";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(fullUrl);
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                Puts("err: " + ex.Message);
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || hitInfo.InitiatorPlayer == null)
            {
                return;
            }

            BasePlayer attacker = hitInfo.InitiatorPlayer;
            BasePlayer owner = entity.OwnerID != 0 ? BasePlayer.FindByID(entity.OwnerID) : null;
            string attackerSteamID = attacker.UserIDString;
            string ownerSteamID = owner?.UserIDString ?? "unknown";
            string entityType = entity.ShortPrefabName;

            BuildingBlock buildingBlock = entity as BuildingBlock;
            if (buildingBlock != null)
            {
                string buildingGrade = buildingBlock.grade.ToString();
                string entityPosition = entity.transform.position.ToString();

                // сам себя рейдит?
                if (owner != null && attacker != owner)
                {
                    // солому нахрен
                    if (!string.Equals(buildingGrade, "twigs", StringComparison.OrdinalIgnoreCase))
                    {
                        // Puts($"{attacker.displayName} ({attackerSteamID}) разрушил {buildingGrade} {entityType} игрока {owner?.displayName ?? "unknown"} ({ownerSteamID}) на координатах {entityPosition}");

                        if (owner != null && UserTelegramIDs.TryGetValue(owner.UserIDString, out string chatId))
                        {
                            string message = $"{attacker.displayName} destroyed your structure: the grade {buildingGrade} ({entityType}) at the coordinates {entityPosition}.";
                            SendTelegramMessage(chatId, message);
                        }
                    }
                }
            }

            // дверь
            if (entityType.Contains("door", StringComparison.OrdinalIgnoreCase))
            {
                // сам себя рейдит
                if (owner != null && attacker != owner)
                {
                    // Puts($"{attacker.displayName} ({attackerSteamID}) разрушил дверь игрока {owner?.displayName ?? "unknown"} ({ownerSteamID})");
                    if (owner != null && UserTelegramIDs.TryGetValue(owner.UserIDString, out string chatId))
                    {
                        string entityPosition = entity.transform.position.ToString();
                        string message = $"{attacker.displayName} destroyed the door to your structure at the coordinates {entityPosition}.";
                        SendTelegramMessage(chatId, message);
                    }
                }
            }
        }


        [ChatCommand("connect")]
        private void ConnectCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                SendReply(player, "[pAlert] Usage: /connect [TG ID] (@getmyid_bot)");
                return;
            }

            string telegramId = args[0];
            string steamId = player.UserIDString;

            if (UserTelegramIDs.ContainsKey(steamId))
            {
                SendReply(player, "[pAlert] You already have Telegram tied to your account.");
                return;
            }

            if (ActiveVerificationCodes.ContainsKey(telegramId))
            {
                SendReply(player, "[pAlert] The code has already been sent to another player.");
                return;
            }

            // рандом код
            string randomCode = GenerateRandomCode();
            SendTelegramMessage(telegramId, $"[pAlert] To complete the binding, enter the command: /check {randomCode}");

            
            UserTelegramIDs[steamId] = telegramId;
            ActiveVerificationCodes[telegramId] = steamId;
            VerificationCodes[telegramId] = randomCode;
            SaveData();

            SendReply(player, $"[pAlert]You have been sent a code in Telegram. To complete the binding, enter the command: /check [code]");
        }


        [ChatCommand("check")]
        private void CheckCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                SendReply(player, "[pAlert] Usage: /check [code]");
                return;
            }

            string code = args[0];
            string steamId = player.UserIDString;

            if (!UserTelegramIDs.TryGetValue(steamId, out string telegramId))
            {
                SendReply(player, "[pAlert] Error: Your Telegram account has not been pre-bound. Use the command /connect [TG ID] (@getmyid_bot)");
                return;
            }

            if (code.Length != 5)
            {
                SendReply(player, "[pAlert] Error: Code must be 5-digit code.");
                return;
            }

            if (ActiveVerificationCodes.TryGetValue(telegramId, out string ownerSteamId) && ownerSteamId == steamId)
            {
                if (VerificationCodes.TryGetValue(telegramId, out string randomCode) && randomCode == code)
                {
                    // Код совпал, привязываем Steam ID к Telegram ID
                    ActiveVerificationCodes.Remove(telegramId);
                    VerificationCodes.Remove(telegramId);
                    SaveData();
                    SendReply(player, "[pAlert]Your account has been successfully linked to your Telegram ID.");
                }
                else
                {
                    SendReply(player, "[pAlert] Incorrect code. Please make sure you have entered the correct code from the message in Telegram.");
                }
            }
            else
            {
                SendReply(player, "[pAlert] Error: This code has already been used or no code has been sent to you.");
            }
        }





        private string GenerateRandomCode()
        {
            System.Random random = new System.Random();
            int code = random.Next(10000, 100000); // 10000-99999
            return code.ToString();
        }
    }
}
