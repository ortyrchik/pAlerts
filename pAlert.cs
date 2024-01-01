using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("pAlert", "promise", "1.0.2")]
    [Description("Alert telegram when raid")]
    class pAlert : RustPlugin
    {

        #region data
        private string BotToken;
        private Dictionary<string, string> UserTelegramIDs;
        private Dictionary<string, string> VerificationCodes;
        private Dictionary<string, string> ActiveVerificationCodes = new Dictionary<string, string>();
        private string CurrentLanguage;

        protected override void LoadDefaultConfig()
        {
            if (Config["BotToken"] == null)
            {
                Config.Set("BotToken", "ваш_токен");
            }
            if (Config["Language"] == null)
            {
                Config.Set("Language", "en");
            }
            SaveConfig();
            CurrentLanguage = Config.Get<string>("Language");

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
            LoadDefaultMessages();
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

        #endregion
        #region local


        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["WoodenDoor"] = "Wooden Door",
                ["MetalDoor"] = "Metal Door",
                ["DoubleWoodenDoor"] = "Double Wooden Door",
                ["DoubleMetalDoor"] = "Double Metal Door",
                ["GarageDoor"] = "Garage Door",
                ["ArmoredDoor"] = "Armored Door",
                ["DoubleArmoredDoor"] = "Double Armored Door",
                ["UnknownDoor"] = "Unknown Door",
                ["DoorDestroyed"] = "{0} destroyed your {1} at {2}.",
                ["ObjectDestroyed"] = "{0} destroyed your structure: quality {1} ({2}) at coordinates {3}",
                ["ConnectUsage"] = "Usage: /connect [TG ID]",
                ["CheckUsage"] = "Usage: /check [Code]",
                ["TelegramAlreadyLinked"] = "Your account is already linked to Telegram.",
                ["TelegramLinkSuccess"] = "Your account has been successfully linked to Telegram ID.",
                ["ErrorLinkingTelegram"] = "Error: Your Telegram account was not previously linked. Use /connect [TG ID]",
                ["CodeMismatch"] = "Incorrect code. Please make sure you entered the correct code from the Telegram message.",
                ["CodeUsedOrNotSent"] = "Error: This code has already been used or you were not sent a code.",
                ["endConnect"] = "A code has been sent to you on Telegram. To complete the linking, enter the command: /check Code",
                ["fiveN"] = "Error: The code must be 5 digits long.",
                ["Disconnect"] = "Your Telegram account has been successfully disconnected.",
                ["noConnect"] = "Your account is not linked to any Telegram ID."
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["WoodenDoor"] = "Деревянная дверь",
                ["MetalDoor"] = "Железная дверь",
                ["DoubleWoodenDoor"] = "Двойная деревянная дверь",
                ["DoubleMetalDoor"] = "Двойная железная дверь",
                ["GarageDoor"] = "Гаражная дверь",
                ["ArmoredDoor"] = "Бронированная дверь",
                ["DoubleArmoredDoor"] = "Двойная бронированная дверь",
                ["UnknownDoor"] = "Неизвестная дверь",
                ["DoorDestroyed"] = "{0} разрушил вашу {1} в вашем строении на координатах {2}.",
                ["ObjectDestoyed"] = "{0} разрушил ваше строение: качество {1} ({2}) на координатах {3}",
                ["ConnectUsage"] = "Использование: /connect [ТГ ID]",
                ["CheckUsage"] = "Использование: /check [код]",
                ["TelegramAlreadyLinked"] = "Ваш аккаунт уже привязан к Телеграм.",
                ["TelegramLinkSuccess"] = "Ваш аккаунт успешно привязан к Telegram ID.",
                ["ErrorLinkingTelegram"] = "Ошибка: Ваш телеграм аккаунт не был предварительно привязан. Используйте /connect [ТГ ID]",
                ["CodeMismatch"] = "Неверный код. Пожалуйста, убедитесь, что вы ввели правильный код из сообщения в Телеграм.",
                ["CodeUsedOrNotSent"] = "Ошибка: Этот код уже использован или вам не был отправлен код.",
                ["endConnect"] = "Вам был отправлен код в Telegram. Для завершения привязки введите команду: /check Код",
                ["fiveN"] = "Ошибка: Код должен быть 5-значным.",
                ["Disconnect"] = "Ваш телеграм аккаунт был успешно отвязан!",
                ["noConnect"] = "Ваш аккаунт не привязан к телеграм аккаунту!."


            }, this, "ru");
        }

        private string GetMessage(string key, string userId = null)
        {
            Dictionary<string, string> messages = lang.GetMessages(CurrentLanguage, this);
            if (messages.TryGetValue(key, out string message))
            {
                return message;
            }
            else
            {
                return $"[Missing Translation: {key}]";
            }
        }
        #endregion
        #region xz
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

        private string GetDoorDescription(string shortPrefabName, string userId)
        {
            string key = shortPrefabName switch
            {
                "door.hinged.wood" => "WoodenDoor",
                "door.hinged.metal" => "MetalDoor",
                "door.double.hinged.wood" => "DoubleWoodenDoor",
                "door.double.hinged.metal" => "DoubleMetalDoor",
                "wall.frame.garagedoor" => "GarageDoor",
                "door.hinged.toptier" => "ArmoredDoor",
                "door.double.hinged.toptier" => "DoubleArmoredDoor",
                _ => "UnknownDoor"
            };

            return lang.GetMessage(key, this, userId);
        }
        #endregion
        #region hooks
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
                        if (owner != null && UserTelegramIDs.TryGetValue(owner.UserIDString, out string chatId))
                        {

                            string message = string.Format(GetMessage("ObjectDestoyed"), attacker.displayName, buildingGrade, entityType, entityPosition);
                            SendTelegramMessage(chatId, message);
                        }
                    }
                }
            }

            // дверь

            if (entityType.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string doorType = GetDoorDescription(entity.ShortPrefabName, attacker.UserIDString);

                if (owner != null && attacker != owner)
                {
                    if (owner != null && UserTelegramIDs.TryGetValue(owner.UserIDString, out string chatId))
                    {
                        string entityPosition = entity.transform.position.ToString();
                        string message = string.Format(GetMessage("DoorDestroyed"), attacker.displayName, doorType, entityPosition);
                        SendTelegramMessage(chatId, message);
                    }
                }
            }
        }
        #endregion
        #region commands
        [ChatCommand("connect")]
        private void ConnectCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                SendReply(player, GetMessage("ConnectUsage", player.UserIDString));
                return;
            }

            string telegramId = args[0];
            string steamId = player.UserIDString;

            if (UserTelegramIDs.ContainsKey(steamId))
            {
                SendReply(player, GetMessage("TelegramAlreadyLinked", player.UserIDString));
                return;
            }

            if (ActiveVerificationCodes.ContainsKey(telegramId))
            {
                SendReply(player, GetMessage("CodeUsedOrNotSent", player.UserIDString));
                return;
            }

            string randomCode = GenerateRandomCode();
            SendTelegramMessage(telegramId, $"[pAlert] Для завершения привязки введите команду: /check {randomCode}");

            UserTelegramIDs[steamId] = telegramId;
            ActiveVerificationCodes[telegramId] = steamId;
            VerificationCodes[telegramId] = randomCode;
            SaveData();

            SendReply(player, GetMessage("endConnect", player.UserIDString));
        }

        [ChatCommand("check")]
        private void CheckCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                SendReply(player, GetMessage("CheckUsage", player.UserIDString));
                return;
            }

            string code = args[0];
            string steamId = player.UserIDString;

            if (!UserTelegramIDs.TryGetValue(steamId, out string telegramId))
            {
                SendReply(player, GetMessage("ErrorLinkingTelegram", player.UserIDString));
                return;
            }

            if (code.Length != 5)
            {
                SendReply(player, GetMessage("fiveN", player.UserIDString));
                return;
            }

            if (ActiveVerificationCodes.TryGetValue(telegramId, out string ownerSteamId) && ownerSteamId == steamId)
            {
                if (VerificationCodes.TryGetValue(telegramId, out string randomCode) && randomCode == code)
                {

                    ActiveVerificationCodes.Remove(telegramId);
                    VerificationCodes.Remove(telegramId);
                    SaveData();
                    SendReply(player, GetMessage("TelegramLinkSuccess", player.UserIDString));
                }
                else
                {
                    SendReply(player, GetMessage("CodeMismatch", player.UserIDString));
                }
            }
            else
            {
                SendReply(player, GetMessage("CodeUsedOrNotSent", player.UserIDString));
            }
        }

        [ChatCommand("disconnect")]
        private void DisconnectCommand(BasePlayer player, string command, string[] args)
        {
            string steamId = player.UserIDString;

            if (UserTelegramIDs.ContainsKey(steamId))
            {
                UserTelegramIDs.Remove(steamId);
                ActiveVerificationCodes.Remove(steamId);
                SaveData();


                SendReply(player, GetMessage("Disconnect", player.UserIDString));
            }
            else
            {
                SendReply(player, GetMessage("noConnect", player.UserIDString));
            }
        }

        [ChatCommand("palert")]
        private void PAlertCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "Available commands:");
                SendReply(player, "/connect [Telegram ID] - Connect your Telegram account.");
                SendReply(player, "/check [Code] - Check and complete the Telegram account linking.");
                SendReply(player, "/disconnect - Disconnect your Telegram account.");
                return;
            }


        }



        #endregion


        private string GenerateRandomCode()
        {
            System.Random random = new System.Random();
            int code = random.Next(10000, 100000); // 10000-99999
            return code.ToString();
        }
    }
}
