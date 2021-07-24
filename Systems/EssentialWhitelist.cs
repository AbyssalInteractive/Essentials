using Life.Network;
using Life.UI;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Essentials
{
    public class EssentialWhitelist : BaseEssential
    {
        public static EssentialWhitelist instance;

        public static string whitelistConfigPath;
        public static List<PlayerWhitelist> wl = new List<PlayerWhitelist>();

        private WhitelistConfig config;

        /// <summary>
        /// Init this class
        /// </summary>
        /// <param name="essentials"></param>
        /// <param name="server"></param>
        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);
            this.server = server;
            instance = this;

            InitConfig();
            LoadWhitelist();
            CreateAdminCommands();
        }

        /// <summary>
        /// Create whitelist configuration file or read it
        /// </summary>
        void InitConfig()
        {
            var settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            whitelistConfigPath = $"{EssentialsPlugin.essentialDirectoryPath}/whitelist.json";

            if (!File.Exists(whitelistConfigPath))
            {
                config = new WhitelistConfig();

                string json = JsonConvert.SerializeObject(config, settings);

                File.WriteAllText(whitelistConfigPath, json);
            }
            else
            {
                string json = File.ReadAllText(whitelistConfigPath);

                try
                {
                    config = JsonConvert.DeserializeObject<WhitelistConfig>(json, settings);

                    string newJson = JsonConvert.SerializeObject(config, settings);

                    File.WriteAllText(whitelistConfigPath, json);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        void LoadWhitelist()
        {
            string[] files = Directory.GetFiles(EssentialsPlugin.essentialPlayersPath);

            for(int i = 0; i < files.Length; i++)
            {
                try
                {
                    string json = File.ReadAllText(files[i]);

                    PlayerData playerData = PlayerData.LoadFromJson(json);

                    if(playerData.lastWhitelist != null && playerData.lastWhitelist.answers.Length > 0 && playerData.whitelisted == false && (playerData.lastWhitelist.moderator == null || playerData.lastWhitelist.moderator.Length == 0))
                    {
                        wl.Add(playerData.lastWhitelist);
                    }
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void CreateAdminCommands()
        {
            SChatCommand wlCommand = new SChatCommand("/whitelist", new string[] { "/wl" },
                "Show whitelist request of provided player or all whitelist requests",
                "/wl <characterId?>",
                (player, args) =>
            {
                if(!player.IsAdmin || !player.isAuthAdmin)
                    return;

                if(args.Length == 1)
                {
                    if(!int.TryParse(args[0], out int characterId))
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>USAGE: /wl <characterId></color>");
                        return;
                    }

                    PlayerWhitelist playerWl = wl.Where(p => p.characterId == characterId).FirstOrDefault();

                    if(playerWl == null)
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>Cette candidature n'existe pas ou a déjà été traitée</color>");
                        return;
                    }

                    if(playerWl.currentModerator > 0)
                    {
                        Player currentMod = server.GetPlayer(playerWl.currentModerator);

                        if(currentMod != null)
                        {
                            player.SendText($"<color={LifeServer.COLOR_RED}>Cette demande de whitelist est en cours d'examination par <color=white>{currentMod.GetFullName()}</color>.</color>");
                            return;
                        }
                    }

                    playerWl.currentModerator = player.character.Id;

                    ShowModWlAnswer(player, playerWl, 0);
                }else
                {
                    UIPanel wlTabPanel = new UIPanel($"Liste des candidatures ({wl.Count})", UIPanel.PanelType.Tab)
                        .AddButton("Fermer", (ui) =>
                        {
                            player.ClosePanel(ui);
                        })
                        .AddButton("Examiner", (ui) =>
                        {
                            ui.SelectTab();
                        });

                    foreach(PlayerWhitelist pWl in wl)
                    {
                        string exam = "";

                        if(pWl.currentModerator > 0)
                        {
                            Player currentMod = server.GetPlayer(pWl.currentModerator);

                            if(currentMod != null)
                                exam = $"<color={LifeServer.COLOR_ORANGE}>examination par {currentMod.character.Firstname}</color>";
                        }

                        wlTabPanel.AddTabLine($"{pWl.characterId} {exam}", (ui) =>
                        {
                            if(pWl.currentModerator > 0)
                            {
                                player.SendText($"<color={LifeServer.COLOR_RED}>Un modérateur examine actuellement cette candidature.</color>");
                                return;
                            }

                            ShowModWlAnswer(player, pWl, 0);
                        });
                    }

                    player.ShowPanelUI(wlTabPanel);
                }
            });

            SChatCommand forceWlCommand = new SChatCommand("/forcewhitelist", new string[] { "/fwl" },
                "Force show whitelist request of provided player with bypassing current mod inspection",
                "/fwl <characterId>",
                (player, args) =>
                {
                    if (!player.IsAdmin || !player.isAuthAdmin)
                        return;

                    if (args.Length != 1)
                        return;

                    if (!int.TryParse(args[0], out int characterId))
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>USAGE: /fwl <characterId></color>");
                        return;
                    }

                    PlayerWhitelist playerWl = wl.Where(p => p.characterId == characterId).FirstOrDefault();

                    if (playerWl == null)
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>Cette candidature n'existe pas ou a déjà été traitée</color>");
                        return;
                    }

                    playerWl.currentModerator = player.character.Id;

                    ShowModWlAnswer(player, playerWl, 0);
                });

            forceWlCommand.Register();
            wlCommand.Register();
        }

        void ShowRejectPanel(Player player, PlayerWhitelist wl)
        {
            UIPanel wlRejectPanel = new UIPanel($"Refuser {wl.firstname} {wl.lastname}", UIPanel.PanelType.Input)
                .AddButton("Retour", (ui) =>
                {
                    ShowModWlAnswer(player, wl, wl.answers.Length);
                })
                .AddButton("Refuser", (ui) =>
                {
                    if (ui.inputText?.Length == 0)
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>Merci d'entrer une raison de refus.</color>");
                        return;
                    }

                    wl.Reject(ui.inputText);
                })
                .AddButton("Blacklister", (ui) =>
                {
                    if (ui.inputText?.Length == 0)
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>Merci d'entrer une raison de refus permanent.</color>");
                        return;
                    }

                    wl.Blacklist(ui.inputText);

                    player.ClosePanel(ui);
                });

            player.ShowPanelUI(wlRejectPanel);
        }

        void ShowModWlAnswer(Player player, PlayerWhitelist wl, int index)
        {
            if (index == wl.answers.Length)
            {
                UIPanel wlValidatePanel = new UIPanel($"Validation de {wl.firstname} {wl.lastname}", UIPanel.PanelType.Text)
                    .AddButton("Retour", (ui) =>
                    {
                        index--;
                        ShowModWlAnswer(player, wl, index);
                    })
                    .AddButton("Refuser", (ui) =>
                    {
                        ShowRejectPanel(player, wl);
                    })
                    
                    .AddButton("Accepter", (ui) =>
                    {
                        wl.Accept();

                        player.ClosePanel(ui);
                    });

                player.ShowPanelUI(wlValidatePanel);

                return;
            }

            player.SendText($"<color={LifeServer.COLOR_BLUE}>Question {index + 1}: {wl.answers[index].question}</color> Réponse: <color={LifeServer.COLOR_ORANGE}>{wl.answers[index].answer}</color>");

            UIPanel wlAnswerPanel = new UIPanel($"Whitelist de {wl.firstname} {wl.lastname} {index + 1}/{wl.answers.Length}", UIPanel.PanelType.Text)
                .SetText($"<color={LifeServer.COLOR_BLUE}>Question {index + 1}: {wl.answers[index].question}</color> Réponse: <color={LifeServer.COLOR_ORANGE}>{wl.answers[index].answer}</color>")
                .AddButton("Fermer", (ui) =>
                {
                    wl.currentModerator = 0;
                    player.ClosePanel(ui);
                });

            if(index > 0)
            {
                wlAnswerPanel.AddButton("Retour", (ui) =>
                {
                    index--;

                    ShowModWlAnswer(player, wl, index);
                });
            }

            wlAnswerPanel.AddButton("Suivant", (ui) =>
            {
                index++;

                ShowModWlAnswer(player, wl, index);
            });

            player.ShowPanelUI(wlAnswerPanel);
        }

        public void OnPlayerWhitelist(Player player)
        {
            PlayerData playerData = player.GetPlayerData();

            if(playerData == null)
            {
                Debug.LogError($"Player Data of {player.steamId} is null");
            }

            if (playerData.lastWhitelist != null && playerData.lastWhitelist.blacklisted)
            {
                player.SendText($"<color={LifeServer.COLOR_RED}>Votre personnage a été blacklisté pour la raison suivante : {playerData.lastWhitelist.reason}</color>");

                player.Disconnect($"Personnage blacklisté : {playerData.lastWhitelist.reason}");
            } else if(!playerData.whitelisted)
            {
                UIPanel whiteListPanel = new UIPanel("Whitelist", UIPanel.PanelType.Text)
                    .SetText("Voulez-vous passer la whitelist maintenant ? (elle sera obligatoire à compter du Lundi 2 Août 2021)")
                    .AddButton("Non", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Oui", (ui) =>
                    {
                        ShowWhitelist(player);
                    });

                player.ShowPanelUI(whiteListPanel);
            }
        }

        public void ShowWhitelistRetry(Player player, string reason)
        {
            UIPanel whiteListPanel = new UIPanel("Whitelist", UIPanel.PanelType.Text)
                    .SetText($"Votre candidature a été rejetée pour la raison suivante: {reason}")
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Réessayer", (ui) =>
                    {
                        ShowWhitelist(player);
                    });

            player.ShowPanelUI(whiteListPanel);
        }

        void ShowWhitelist(Player player)
        {
            player.setup.TargetSetPosition(new Vector3(1980f, 51f, 24f));

            if (config == null || config.questions.Length == 0)
            {
                player.SendText($"<color={LifeServer.COLOR_RED}>Whitelist non configurée ou fichier de configuration manquant</color>");
            }

            PlayerData data = player.GetPlayerData();

            data.lastWhitelist = new PlayerWhitelist() { answers = new WhitelistAnswer[config.questions.Length]  };
            data.Save();

            ShowQuestion(player, 0);
        }

        void ShowQuestion(Player player, int i)
        {
            if (i == config.questions.Length)
            {
                PlayerData playerData = player.GetPlayerData();

                playerData.lastWhitelist.firstname = player.character.Firstname;
                playerData.lastWhitelist.lastname = player.character.Lastname;

                player.SendText($"<color={LifeServer.COLOR_BLUE}>Votre candidature pour passer la whitelist a bien été envoyée, vous recevrez une réponse sous peu.</color>");
                
                playerData.Save();

                PlayerWhitelist playerDataWhitelist = playerData.lastWhitelist;

                playerDataWhitelist.steamId = player.steamId;
                playerDataWhitelist.characterId = player.character.Id;

                wl.Add(playerDataWhitelist);

                foreach (Player admin in server.Players.Where(p => p.account.adminLevel > 0 && p.serviceAdmin).ToList())
                {
                    admin.SendText($"<color={LifeServer.COLOR_BLUE}>Whitelist de <color=white>{player.GetFullName()}</color>. <color={LifeServer.COLOR_RED}>/wl {player.character.Id}</color> pour traiter la candidature.</color>");
                }

                return;
            }

            WhitelistQuestion question = config.questions[i];

            UIPanel whitelistPanel = new UIPanel($"Whitelist {i + 1}/{config.questions.Length}", UIPanel.PanelType.Input)
                .SetText(question.title)
                .SetInputPlaceholder(question.placeholder)
                .AddButton("Suivant", (ui) =>
                {
                    if (ui.inputText == null || ui.inputText.Length == 0)
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>Merci de compléter le champ.</color>");
                        return;
                    }

                    WhitelistAnswer answer = new WhitelistAnswer()
                    {
                        question = question.title,
                        answer = ui.inputText
                    };

                    PlayerData playerData = player.GetPlayerData();

                    playerData.lastWhitelist.answers[i] = answer;

                    player.ClosePanel(ui);

                    i++;

                    ShowQuestion(player, i);
                });

            player.ShowPanelUI(whitelistPanel);
        }
    }

    [Serializable]
    public class WhitelistConfig
    {
        public WhitelistQuestion[] questions = new WhitelistQuestion[] { new WhitelistQuestion() { title = "Comment avez-vous connu le serveur ?", placeholder = "Entrez la réponse..." } };

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            File.WriteAllText(EssentialWhitelist.whitelistConfigPath, json);
        }
    }

    [Serializable]
    public struct WhitelistQuestion
    {
        public string title;
        public string placeholder;
    }

    [Serializable]
    public struct WhitelistAnswer
    {
        public string question;
        public string answer;
    }

    [Serializable]
    public class PlayerWhitelist
    {
        public WhitelistAnswer[] answers;

        public bool blacklisted;
        public string reason;
        public string moderator;

        public string firstname;
        public string lastname;

        [NonSerialized]
        public int currentModerator;

        public int characterId;
        public ulong steamId;

        public void Blacklist(string reason)
        {
            PlayerData data = PlayerData.GetPlayerData(steamId, characterId);

            this.reason = reason;
            blacklisted = true;
            moderator = currentModerator == 0 ? "Indéfini" : Life.Nova.server.GetPlayer(currentModerator).GetFullName();

            data.lastWhitelist = this;
            data.Save();

            Player player = Life.Nova.server.GetPlayer(characterId);
            
            if(player != null)
            {
                player.SendText($"<color={LifeServer.COLOR_RED}>Vous avez été blacklisté pour la raison suivante: {reason}</color>");
                player.Disconnect($"Vous avez été blacklisté pour la raison suivante: {reason}");
            }

            EssentialWhitelist.wl.Remove(this);
        }

        public void Reject(string reason)
        {
            PlayerData data = PlayerData.GetPlayerData(steamId, characterId);

            this.reason = reason;

            Player currentMod = Life.Nova.server.GetPlayer(currentModerator);

            moderator = "Indéfini";

            if (currentMod != null)
            {
                moderator = currentMod.GetFullName();
            }

            data.lastWhitelist = this;
            data.Save();

            Player player = Life.Nova.server.GetPlayer(characterId);

            if (player != null)
            {
                player.SendText($"<color={LifeServer.COLOR_RED}>Votre candidature à la whitelist a été rejetée pour la raison suivante: {reason}</color>");
                EssentialWhitelist.instance.ShowWhitelistRetry(player, reason);
            }

            EssentialWhitelist.wl.Remove(this);
        }

        public void Accept()
        {
            PlayerData data = PlayerData.GetPlayerData(steamId, characterId);

            data.whitelisted = true;
            moderator = currentModerator == 0 ? "Indéfini" : Life.Nova.server.GetPlayer(currentModerator).GetFullName();

            data.lastWhitelist = this;
            data.Save();

            Player player = Life.Nova.server.GetPlayer(characterId);

            if (player != null)
            {
                player.SendText($"<color={LifeServer.COLOR_GREEN}>Votre candidature à la whitelist a été acceptée par l'équipe de modération. Bon jeu à vous !</color>");
                player.setup.TargetSetPosition(new Vector3(771.466f, 50f, 676f));
            }

            EssentialWhitelist.wl.Remove(this);
        }
    }
}
