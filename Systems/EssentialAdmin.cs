using Life.Network;
using Life.UI;
using Life;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Life.VehicleSystem;
using Mirror;

namespace Essentials
{
    public class EssentialAdmin : BaseEssential
    {
        public static string adminConfigPath;

        public AnnouncerConfig config;

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
                    if(args.Length > 0)
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

            stowCommand.Register();
            destroyCommand.Register();
            saveCommand.Register();
            clearInventoryCommand.Register();
            refuelAllCommand.Register();
            refuelCommand.Register();
            changeNumberCommand.Register();
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