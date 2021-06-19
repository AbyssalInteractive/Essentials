using Life.Network;
using Life.UI;
using Life;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Life.VehicleSystem;
using Mirror;
using Life.PermissionSystem;
using Life.DB;
using Life.AreaSystem;
using System.Collections.Generic;

namespace Essentials
{
    public class EssentialAdmin : BaseEssential
    {
        public static string adminConfigPath;

        public AnnouncerConfig config;

        public Dictionary<Player, string> tickets = new Dictionary<Player, string>();

        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);

            InitConfig();
            CreateConsoleCommands();
            CreateAdminCommands();
        }

        void CreateConsoleCommands()
        {

        }

        void InitConfig()
        {
            adminConfigPath = $"{EssentialsPlugin.essentialDirectoryPath}/admin.json";

            if (!File.Exists(adminConfigPath))
            {
                config = new AnnouncerConfig()
                {

                };

                string json = JsonConvert.SerializeObject(config);

                File.WriteAllText(adminConfigPath, json);
            }
            else
            {
                string json = File.ReadAllText(adminConfigPath);

                try
                {
                    config = JsonConvert.DeserializeObject<AnnouncerConfig>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        void CreateAdminCommands()
        {
            SChatCommand changeNumberCommand = new SChatCommand("/changenumber", "Change the phone number of the nearest player", "/changenumber", (player, args) =>
            {
                if (player.account.adminLevel >= 9)
                {
                    Player closestPlayer = player.GetClosestPlayer();

                    string name = "null";

                    if (closestPlayer != null)
                    {
                        name = closestPlayer.GetFullName();
                    }
                    else
                    {
                        name = player.GetFullName();
                    }

                    UIPanel numberPanel = new UIPanel($"Changement de numéro de {name}", UIPanel.PanelType.Input)
                        .AddButton("Fermer", (ui) =>
                        {
                            player.ClosePanel(ui);
                        })
                        .SetInputPlaceholder("Numéro...")
                        .AddButton("Valider", (ui) =>
                        {
                            string number = ui.inputText;

                            if (closestPlayer != null)
                            {
                                closestPlayer.character.PhoneNumber = number;
                                _ = closestPlayer.Save();
                            }
                            else
                            {
                                player.character.PhoneNumber = number;
                                _ = player.Save();
                            }

                            player.SendText($"<color={LifeServer.COLOR_GREEN}>Numéro modifié avec succès !</color>");
                            player.ClosePanel(ui);
                        });

                    player.ShowPanelUI(numberPanel);
                }
            });

            SChatCommand refuelCommand = new SChatCommand("/refuel", "Refuel the current vehicle that you're in", "/refuel", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    Vehicle vehicle = player.GetClosestVehicle();

                    if (vehicle)
                    {
                        vehicle.fuel = 100f;
                    }
                }
                else
                {
                    player.SendText(string.Format("<color={0}>Permissions insuffisantes.</color>", LifeServer.COLOR_RED));
                }
            });

            SChatCommand refuelAllCommand = new SChatCommand("/refuelall", "Refual all spawned vehicles", "/refuelall", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    Vehicle[] vehicles = GameObject.FindObjectsOfType<Vehicle>();

                    for (int i = 0; i < vehicles.Length; i++)
                    {
                        vehicles[i].fuel = 100f;
                    }
                }
                else
                {
                    player.SendText(string.Format("<color={0}>Permissions insuffisantes.</color>", LifeServer.COLOR_RED));
                }
            });

            SChatCommand clearInventoryCommand = new SChatCommand("/clearinventory", new string[] { "/clearinv" }, "Clear your inventory", "/clearinv(entory)", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    player.setup.inventory.Clear();
                }
                else
                {
                    player.SendText(string.Format("<color={0}>Permissions insuffisantes.</color>", LifeServer.COLOR_RED));
                }
            });

            SChatCommand saveCommand = new SChatCommand("/save", "Save the server", "/save", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    _ = server.save.Save();
                }
                else
                {
                    player.SendText(string.Format("<color={0}>Permissions insuffisantes.</color>", LifeServer.COLOR_RED));
                }
            });

            SChatCommand destroyCommand = new SChatCommand("/destroy", "Destroy nearest quick owner vehicle", "/destroy", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    Vehicle vehicle = player.GetClosestVehicle(5f);

                    if (vehicle != null && vehicle.quickOwner > 0)
                    {
                        NetworkServer.Destroy(vehicle.gameObject);
                    }
                }
            });

            SChatCommand stowCommand = new SChatCommand("/stowvehicle", new string[] { "/stowveh", "/stow" }, "Stow nearest vehicle or specified vehicle db id", "/stow(vehicle) <vehicleDbId?>", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    if (args.Length > 0)
                    {
                        if (int.TryParse(args[0], out int vehicleDbId))
                        {
                            LifeVehicle lifeVehicle = Nova.v.GetVehicle(vehicleDbId);

                            if (lifeVehicle == null)
                            {
                                player.SendText($"<color={LifeServer.COLOR_RED}>Véhicule introuvable.</color>");
                                return;
                            }
                            else if (lifeVehicle.instance == null)
                            {
                                player.SendText($"<color={LifeServer.COLOR_RED}>Le véhicule est rangé au concessionnaire.</color>");
                                return;
                            }

                            Nova.v.StowVehicle(lifeVehicle.instance.vehicleDbId);
                        }
                        else
                        {
                            player.SendText($"<color={LifeServer.COLOR_RED}>Format de l'id du véhicule incorrect.</color>");
                        }
                    }
                    else
                    {
                        Vehicle vehicle = player.GetClosestVehicle(5f);

                        if (vehicle != null && vehicle.vehicleDbId > 0)
                        {
                            Nova.v.StowVehicle(vehicle.vehicleDbId);
                        }
                    }
                }
            });

            SChatCommand stowAllCommand = new SChatCommand("/stowallvehicle", new string[] { "/stowallveh", "/stowall" }, "Stow all spawned vehicle on the map", "/stow(allveh)", (player, args) =>
            {
                if (player.account.adminLevel >= 9)
                {
                    Vehicle[] vehicles = GameObject.FindObjectsOfType<Vehicle>();

                    foreach (Vehicle vehicle in vehicles)
                    {
                        if (vehicle != null && vehicle.vehicleDbId > 0)
                        {
                            Nova.v.StowVehicle(vehicle.vehicleDbId);
                        }
                    }
                }
            });

            SChatCommand bizCommand = new SChatCommand("/editbiz", "Edit the current biz that you're in", "/editbiz", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                UIPanel bizPanel = new UIPanel("Gestion entreprise", UIPanel.PanelType.Tab)
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Sélectionner", (ui) =>
                    {
                        ui.SelectTab();
                    })
                    .AddTabLine("Modifier terrain id", (ui) =>
                    {
                        UIPanel terrainPanel = new UIPanel("Modification de l'id terrain", UIPanel.PanelType.Input)
                        .SetInputPlaceholder("ID terrain...")
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int terrainId = int.Parse(ui2.inputText);

                            if (player.HasBiz())
                            {
                                player.biz.TerrainId = terrainId;
                                player.biz.Save();

                                player.SendText("Terrain id modifié");
                            }
                            else
                            {
                                player.SendText("Vous n'avez pas d'entreprise");
                            }
                        });

                        player.ShowPanelUI(terrainPanel);
                    })
                    .AddTabLine("Modifier id activité", (ui) =>
                    {
                        UIPanel terrainPanel = new UIPanel("Modification de l'id activité", UIPanel.PanelType.Input)
                        .SetInputPlaceholder("ID activité...")
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int activityId = int.Parse(ui2.inputText);

                            Activities activities = new Activities()
                            {
                                ids = new int[] { activityId }
                            };

                            if (player.HasBiz())
                            {
                                player.biz.Activities = JsonUtility.ToJson(activities);
                                player.biz.Save();

                                player.SendText("Activité id modifié");
                            }
                            else
                            {
                                player.SendText("Vous n'avez pas d'entreprise");
                            }
                        });

                        player.ShowPanelUI(terrainPanel);
                    })
                    .AddTabLine("Se définir propriétaire", (ui) =>
                    {
                        player.biz.OwnerId = player.character.Id;
                        player.biz.Save();

                        player.SendText($"<color={LifeServer.COLOR_GREEN}>Vous êtes désormais propriétaire de l'entreprise.</color>");
                    });

                player.ShowPanelUI(bizPanel);
            });

            SChatCommand vehicleCommand = new SChatCommand("/vehicle", new string[] { "/v", "/veh" }, "Vehicle admin menu", "/v(ehicle)", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                UIPanel vAdminPanel = new UIPanel("Gestion véhicule", UIPanel.PanelType.Tab)
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Sélectionner", (ui) =>
                    {
                        ui.SelectTab();
                    })
                    .AddTabLine("Définir id entreprise", (ui) =>
                    {
                        if (player.GetVehicleId() == 0)
                        {
                            player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                            return;
                        }

                        UIPanel idEntreprisePanel = new UIPanel("Définir id entreprise véhicule", UIPanel.PanelType.Input)
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int entId = int.Parse(ui2.inputText);

                            Vehicle vehicle = NetworkIdentity.spawned[player.GetVehicleId()].GetComponent<Vehicle>();

                            vehicle.bizId = entId;

                            LifeVehicle lifeVehicle = Nova.v.GetVehicle(vehicle.vehicleDbId);

                            lifeVehicle.bizId = entId;
                            lifeVehicle.Save();

                            player.ClosePanel(ui2);
                            player.SendText(string.Format("<color={0}>Entreprise modifiée avec succès.</color>", LifeServer.COLOR_GREEN));
                        });

                        player.ShowPanelUI(idEntreprisePanel);
                    })
                    .AddTabLine("Définir propriétaire", (ui) =>
                    {
                        if (player.GetVehicleId() == 0)
                        {
                            player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                            return;
                        }

                        UIPanel idProprietairePanel = new UIPanel("Définir id propriétaire", UIPanel.PanelType.Input)
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int ownerId = int.Parse(ui2.inputText);

                            Vehicle vehicle = NetworkIdentity.spawned[player.GetVehicleId()].GetComponent<Vehicle>();

                            LifeVehicle lifeVehicle = Nova.v.GetVehicle(vehicle.vehicleDbId);

                            lifeVehicle.permissions.owner = new Entity() { characterId = ownerId };
                            lifeVehicle.Save();

                            player.ClosePanel(ui2);
                            player.SendText(string.Format("<color={0}>Propriétaire modifié avec succès.</color>", LifeServer.COLOR_GREEN));
                        });

                        player.ShowPanelUI(idProprietairePanel);
                    })
                    .AddTabLine("Créer véhicule", (ui) =>
                    {
                        UIPanel createVehicle = new UIPanel("Créer véhicule", UIPanel.PanelType.Input)
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int vehicleId = int.Parse(ui2.inputText);

                            if (vehicleId >= Nova.v.vehiclesModelName.Length)
                                return;

                            Permissions permissions = new Permissions()
                            {
                                owner = new Entity()
                                {
                                    characterId = player.character.Id
                                }
                            };

                            CreateVehicle(player, vehicleId, permissions);
                        });

                        player.ShowPanelUI(createVehicle);
                    });

                player.ShowPanelUI(vAdminPanel);
            });

            SChatCommand serviceAdminCommand = new SChatCommand("/serviceadmin", new string[] { "/sa", "/adminservice" }, "Admin service", "/serviceadmin", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    if (!player.isAuthAdmin)
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>Vous n'êtes pas authentifié.</color>");
                        return;
                    }

                    if (player.serviceAdmin)
                    {
                        player.SetAdminService(false);
                        player.SendText(string.Format("<color={0}>Service admin désactivé !</color>", LifeServer.COLOR_RED));
                    }
                    else
                    {
                        player.SetAdminService(true);
                        player.SendText(string.Format("<color={0}>Service admin activé !</color>", LifeServer.COLOR_GREEN));
                    }

                    player.serviceAdmin = !player.serviceAdmin;
                }

            });

            SChatCommand stopCommand = new SChatCommand("/stop", "Stop the server", "/stop", (player, args) =>
            {
                if (player.IsAdmin)
                    server.Stop();
                else
                    player.SendText(string.Format("<color={0}>Permissions insuffisantes.</color>", LifeServer.COLOR_RED));
            });

            SChatCommand fpsCommand = new SChatCommand("/fps", "Show server fps", "/fps", (player, args) =>
            {
                float msec = Time.deltaTime * 1000.0f;
                float fps = 1.0f / Time.deltaTime;

                player.SendText(string.Format("STATS SERVEUR: {0:0.0} ms ({1:0.} fps)", msec, fps));
            });

            SChatCommand prisonCommand = new SChatCommand("/prison", "Put nearest player in prison", "/prison", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                Player closestPlayer = player.GetClosestPlayer();

                if (closestPlayer != null)
                {
                    if (closestPlayer.GetVehicleId() != 0)
                    {
                        player.SendText(string.Format("<color={0}>Impossible de mettre en prison, faites le sortir du véhicule !</color>", LifeServer.COLOR_RED));
                    }
                    else
                    {
                        UIPanel prisonPanel = new UIPanel(string.Format("Mise en prison de {0}", closestPlayer.GetFullName()), UIPanel.PanelType.Input)
                        .SetText("Entrez la durée de la prison en minutes :")
                        .SetInputPlaceholder("Durée...")
                        .AddButton("Annuler", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int time = 0;

                            int.TryParse(ui2.inputText, out time);

                            if (time > 0)
                            {
                                closestPlayer.setup.prisonTime = time * 60;
                                player.SendText(string.Format("<color={0}>Vous avez mis {1} en prison.</color>", LifeServer.COLOR_GREEN, closestPlayer.GetFullName()));
                                closestPlayer.SendText(string.Format("<color={0}>Vous avez été placé en prison admin par {1}.</color>", LifeServer.COLOR_RED, player.GetFullName()));
                            }
                            else
                            {
                                player.SendText(string.Format("<color={0}>Saisissez une valeur supérieur à 0 !</color>", LifeServer.COLOR_RED));
                            }
                        });

                        player.ShowPanelUI(prisonPanel);
                    }
                }
            });

            SChatCommand bcrCommand = new SChatCommand("/removebcr", "Remove bcr to nearest player", "/removebcr", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                Player closestPlayer = player.GetClosestPlayer();

                if (closestPlayer != null)
                {
                    if (closestPlayer.GetVehicleId() != 0)
                    {
                        player.SendText(string.Format("<color={0}>Impossible de mettre en prison, faites le sortir du véhicule !</color>", LifeServer.COLOR_RED));
                    }
                    else
                    {
                        UIPanel prisonPanel = new UIPanel(string.Format("Retrait du BCR de {0}", closestPlayer.GetFullName()), UIPanel.PanelType.Text)
                        .SetText("Retrait du BCR :")
                        .AddButton("Annuler", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            closestPlayer.character.HasBCR = false;
                            closestPlayer.setup.TargetLockCFM(true);
                            closestPlayer.setup.TargetSetPosition(new Vector3(757.7316f, 50.6f, 679.578f)); // spawn position
                            closestPlayer.SendText($"<color={LifeServer.COLOR_RED}>Un administrateur vous a retiré votre brevet de connaissance des règles.</color>");
                        });

                        player.ShowPanelUI(prisonPanel);
                    }
                }
            });

            SChatCommand flipCommand = new SChatCommand("/flip", "Flip the current vehicle that you're in", "/flip", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    uint vehicleId = player.GetVehicleId();

                    if (vehicleId > 0)
                    {
                        NetworkIdentity.spawned[vehicleId].GetComponent<Vehicle>().RpcFlip();

                        NetworkIdentity.spawned[vehicleId].transform.rotation = Quaternion.Euler(Vector3.zero);
                        NetworkIdentity.spawned[vehicleId].transform.position += Vector3.up * 2f;
                    }
                    else
                    {
                        player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                    }
                }
            });

            SChatCommand setAdminCommand = new SChatCommand("/setadmin", "Set nearest player admin", "/setadmin", (player, args) =>
            {
                if (player.account.adminLevel > 7)
                {
                    Player closest = player.GetClosestPlayer();

                    if (closest != null)
                    {
                        UIPanel setAdminPanel = new UIPanel(string.Format("Définir rang admin de {0}", closest.account.username), UIPanel.PanelType.Input)
                            .SetInputPlaceholder("Rang admin...")
                            .AddButton("Fermer", (ui) =>
                            {
                                player.ClosePanel(ui);
                            })
                            .AddButton("Valider", (ui) =>
                            {
                                closest.account.adminLevel = int.Parse(ui.inputText);
                                _ = closest.Save();
                                player.ClosePanel(ui);
                            });

                        player.ShowPanelUI(setAdminPanel);
                    }
                }
            });

            SChatCommand editTerrainCommand = new SChatCommand("/terrain", new string[] { "/t" }, "Edit the terrain that you're in", "/t(errain", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                UIPanel terrainPanel = new UIPanel("Gestion terrain", UIPanel.PanelType.Tab)
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Sélectionner", (ui) =>
                    {
                        ui.SelectTab();
                    })
                    .AddTabLine("Modifier propriétaire", (ui) =>
                    {
                        UIPanel proprioPanel = new UIPanel("Modification du proprio", UIPanel.PanelType.Input)
                        .SetInputPlaceholder("ID perso...")
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int proprio = int.Parse(ui2.inputText);

                            if (player.setup.areaId > 0)
                            {
                                LifeArea area = Nova.a.GetAreaById(player.setup.areaId);

                                area.permissions = new Permissions() { owner = new Entity() { characterId = proprio } };

                                area.Save();

                                player.SendText("Propriétaire modifié !");
                                player.ClosePanel(ui2);
                            }
                        });

                        player.ShowPanelUI(proprioPanel);
                    })
                     .AddTabLine("Modifier prix location", (ui) =>
                     {
                         UIPanel pricePanel = new UIPanel("Modification du prix de location", UIPanel.PanelType.Input)
                         .SetInputPlaceholder("Prix...")
                         .AddButton("Fermer", (ui2) =>
                         {
                             player.ClosePanel(ui2);
                         })
                         .AddButton("Valider", (ui2) =>
                         {
                             int price = int.Parse(ui2.inputText);

                             if (player.setup.areaId > 0)
                             {
                                 LifeArea area = Nova.a.GetAreaById(player.setup.areaId);

                                 area.rentPrice = price;

                                 if (area.rentPrice > 0)
                                 {
                                     area.isRentable = true;
                                 }
                                 else
                                 {
                                     area.isRentable = false;
                                 }

                                 area.Save();

                                 player.SendText("Prix de location modifié !");
                                 player.ClosePanel(ui2);
                             }
                         });

                         player.ShowPanelUI(pricePanel);
                     })
                    .AddTabLine("Modifier prix", (ui) =>
                    {
                        UIPanel pricePanel = new UIPanel("Modification du prix", UIPanel.PanelType.Input)
                        .SetInputPlaceholder("Prix...")
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            int price = int.Parse(ui2.inputText);

                            if (player.setup.areaId > 0)
                            {
                                LifeArea area = Nova.a.GetAreaById(player.setup.areaId);

                                area.price = price;

                                area.Save();

                                player.SendText("Prix modifié !");
                                player.ClosePanel(ui2);
                            }
                        });

                        player.ShowPanelUI(pricePanel);
                    });

                player.ShowPanelUI(terrainPanel);
            });

            SChatCommand giveBcrCommand = new SChatCommand("/givebcr", new string[] { "/addbcr" }, "Give bcr to nearest player or yourself", "/givebcr", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                Player closestPlayer = player.GetClosestPlayer();

                if (closestPlayer != null)
                {
                    closestPlayer.character.HasBCR = true;
                    closestPlayer.setup.TargetLockCFM(false);

                    closestPlayer.SendText($"<color={LifeServer.COLOR_RED}>Un administrateur vous a donné le BCR.</color>");
                }else
                {
                    player.character.HasBCR = true;
                    player.setup.TargetLockCFM(false);

                    player.SendText($"<color={LifeServer.COLOR_RED}>Vous vous êtes donné le BCR.</color>");
                }
            });

            SChatCommand dayCommand = new SChatCommand("/day", "Set day", "/day", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                EnviroSkyMgr.instance.SetTimeOfDay(12.0f);
            });

            SChatCommand nightCommand = new SChatCommand("/night", "Set night", "/night", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                EnviroSkyMgr.instance.SetTimeOfDay(20.0f);
            });

            SChatCommand morningCommand = new SChatCommand("/morning", "Set morning", "/morning", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                EnviroSkyMgr.instance.SetTimeOfDay(8.0f);
            });

            SChatCommand timesetCommand = new SChatCommand("/timeset", new string[] { "/time" }, "Set time of day", "/timeset <12,5>", (player, args) =>
            {
                if (!player.IsAdmin)
                    return;

                if (args.Length == 1)
                {
                    if(float.TryParse(args[0], out float result))
                    {
                        EnviroSkyMgr.instance.SetTimeOfDay(result);

                        player.SendText($"Time set to {result}");
                    }
                    else
                    {
                        player.SendText($"<color={LifeServer.COLOR_RED}>USAGE: <color=white>/timeset <12,5></color></color>");
                    }
                }
                else
                {
                    player.SendText($"<color={LifeServer.COLOR_RED}>USAGE: <color=white>/timeset <12,5></color></color>");
                }
            });

            SChatCommand setPinCommand = new SChatCommand("/setpin", "Set admin pin", "/setpin", (player, args) => 
            {
                if (!player.IsAdmin && player.account.adminLevel < 10 && !player.isAuthAdmin)
                    return;

                Player closestPlayer = player.GetClosestPlayer();

                if (closestPlayer != null)
                {
                    UIPanel pin = new UIPanel(string.Format("Code pin de {0}", closestPlayer.GetFullName()), UIPanel.PanelType.Input)
                    .SetText("Entrez le code pin :")
                    .SetInputPlaceholder("Code...")
                    .AddButton("Annuler", (ui2) =>
                    {
                        player.ClosePanel(ui2);
                    })
                    .AddButton("Valider", (ui2) =>
                    {
                        closestPlayer.account.adminPin = ui2.inputText;
                        _ = closestPlayer.Save();
                        player.SendText("Pin modifié !");
                        player.ClosePanel(ui2);
                    });

                    player.ShowPanelUI(pin);
                }
            });

            SChatCommand forwardCommand = new SChatCommand("/forward", "Move vehicle forward", "/forward", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    uint vehicleId = player.GetVehicleId();

                    if (vehicleId > 0)
                    {
                        NetworkIdentity.spawned[vehicleId].GetComponent<Vehicle>().RpcAddPosition(NetworkIdentity.spawned[vehicleId].transform.forward);

                        NetworkIdentity.spawned[vehicleId].transform.position += NetworkIdentity.spawned[vehicleId].transform.forward;
                    }
                    else
                    {
                        player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                    }
                }
            });

            SChatCommand backwardCommand = new SChatCommand("/backward", "Move vehicle backward", "/backward", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    uint vehicleId = player.GetVehicleId();

                    if (vehicleId > 0)
                    {
                        NetworkIdentity.spawned[vehicleId].GetComponent<Vehicle>().RpcAddPosition(-NetworkIdentity.spawned[vehicleId].transform.forward);

                        NetworkIdentity.spawned[vehicleId].transform.position -= NetworkIdentity.spawned[vehicleId].transform.forward;
                    }
                    else
                    {
                        player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                    }
                }
            });

            SChatCommand rightCommand = new SChatCommand("/right", "Move vehicle right", "/right", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    uint vehicleId = player.GetVehicleId();

                    if (vehicleId > 0)
                    {
                        NetworkIdentity.spawned[vehicleId].GetComponent<Vehicle>().RpcAddPosition(NetworkIdentity.spawned[vehicleId].transform.right);

                        NetworkIdentity.spawned[vehicleId].transform.position += NetworkIdentity.spawned[vehicleId].transform.right;
                    }
                    else
                    {
                        player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                    }
                }
            });

            SChatCommand leftCommand = new SChatCommand("/left", "Move vehicle left", "/left", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    uint vehicleId = player.GetVehicleId();

                    if (vehicleId > 0)
                    {
                        NetworkIdentity.spawned[vehicleId].GetComponent<Vehicle>().RpcAddPosition(-NetworkIdentity.spawned[vehicleId].transform.right);

                        NetworkIdentity.spawned[vehicleId].transform.position -= NetworkIdentity.spawned[vehicleId].transform.right;
                    }
                    else
                    {
                        player.SendText(string.Format("<color={0}>Vous n'êtes pas dans un véhicule.</color>", LifeServer.COLOR_RED));
                    }
                }
            });

            SChatCommand ticketsCommand = new SChatCommand("/tickets", "Open ticket panel", "/tickets", (player, args) =>
            {
                if (player.IsAdmin)
                {
                    UIPanel tickets = new UIPanel("Liste des tickets", UIPanel.PanelType.Tab)
                        .AddButton("Fermer", (ui) =>
                        {
                            player.ClosePanel(ui);
                        })
                        .AddButton("Intervenir (TP)", (ui) =>
                        {
                            ui.SelectTab();
                        });

                    foreach (KeyValuePair<Player, string> ticket in this.tickets)
                    {
                        if (ticket.Key == null)
                            continue;
                        if (ticket.Key.conn == null)
                            continue;
                        if (ticket.Key.setup == null)
                            continue;

                        string value = "";

                        if (ticket.Value.Length > 30)
                            value = ticket.Value.Substring(0, 30);
                        else
                            value = ticket.Value;

                        tickets.AddTabLine(string.Format("{0} {1} - {2}", new string[] { ticket.Key.character.Firstname, ticket.Key.character.Lastname, value }), (ui2) =>
                        {
                            player.ClosePanel(ui2);
                            player.setup.TargetSetPosition(ticket.Key.setup.transform.position);
                            ticket.Key.ticketOpen = false;
                            this.tickets.Remove(ticket.Key);
                        });
                    }

                    player.ShowPanelUI(tickets);
                }
            });

            ticketsCommand.Register();
            leftCommand.Register();
            rightCommand.Register();
            backwardCommand.Register();
            forwardCommand.Register();
            setPinCommand.Register();
            timesetCommand.Register();
            dayCommand.Register();
            nightCommand.Register();
            morningCommand.Register();
            giveBcrCommand.Register();
            editTerrainCommand.Register();
            setAdminCommand.Register();
            flipCommand.Register();
            bcrCommand.Register();
            prisonCommand.Register();
            fpsCommand.Register();
            stopCommand.Register();
            serviceAdminCommand.Register();
            vehicleCommand.Register();
            bizCommand.Register();
            stowAllCommand.Register();
            stowCommand.Register();
            destroyCommand.Register();
            saveCommand.Register();
            clearInventoryCommand.Register();
            refuelAllCommand.Register();
            refuelCommand.Register();
            changeNumberCommand.Register();
        }

        public void OnPlayerTicket(Player player, string ticket)
        {
            tickets.Add(player, ticket);
            player.ticketOpen = true;

            for (int i = 0; i < server.Players.Count; i++)
            {
                if (server.Players[i].IsAdmin && server.Players[i].serviceAdmin)
                {
                    string text = string.Format("Nouveau ticket de {0} {1}. Message : {2}. \n /tickets pour voir la liste des tickets", new string[] { player.character.Firstname, player.character.Lastname, ticket });
                    server.Players[i].SendText(text);
                }
            }
        }

        async void CreateVehicle(Player player, int vehicleId, Permissions permissions)
        {
            if (Nova.v.vehicleModels[vehicleId].isDeprecated)
            {
                player.SendText(string.Format("<color={0}>Impossible de faire apparaître un véhicule déprécié.</color>", LifeServer.COLOR_RED));
                return;
            }

            Vehicles vehicles = await LifeDB.CreateVehicle(vehicleId, JsonUtility.ToJson(permissions));

            player.SendText(string.Format("<color={0}>Véhicule créé avec succès</color>", LifeServer.COLOR_GREEN));

            LifeVehicle vehicle = Nova.v.GetVehicle(vehicles.Id);

            if (vehicle.instance == null)
            {
                Vehicle instance = GameObject.Instantiate(Nova.v.vehicleModels[vehicle.modelId], player.setup.transform.position, player.setup.transform.rotation);

                NetworkServer.Spawn(instance.gameObject);

                instance.color = Nova.HexToColor(vehicle.color);
                instance.plate = vehicle.plate;

                if (instance.engineInventory)
                {
                    if (vehicle.engineInventory.Length > 0)
                        instance.engineInventory.DeserializeJson(vehicle.engineInventory);
                }

                if (instance.vehicleInventory)
                {
                    if (vehicle.inventory.Length > 0)
                        instance.vehicleInventory.DeserializeJson(vehicle.inventory);
                }


                vehicle.instance = instance;
                instance.vehicleDbId = vehicle.vehicleId;
                instance.fuel = vehicle.fuel;
            }

            vehicle.isStowed = false;
            vehicle.Save();
        }
    }

    [System.Serializable]
    public class AdminConfig
    {
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            File.WriteAllText(EssentialAdmin.adminConfigPath, json);
        }
    }
}