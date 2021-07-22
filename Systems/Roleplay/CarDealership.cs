using Life.Network;
using Life.UI;
using Life.CheckpointSystem;
using UnityEngine;
using Life;
using Life.PermissionSystem;
using Life.DB;

namespace Essentials.Roleplay
{
    public class CarDealership
    {
        private CarDealershipConfig config;
        private LifeServer server;

        public void Init(CarDealershipConfig config)
        {
            this.config = config;
            server = Nova.server;

            server.OnPlayerSpawnCharacterEvent += OnPlayerSpawnCharacter;
        }

        void OnPlayerSpawnCharacter(Player player)
        {
            NCheckpoint carDealerCheckpoint = new NCheckpoint(player.netId, config.shopPosition.ToVector3(), (c) =>
            {
                ShowCarDealerUI(player);
            });

            player.CreateCheckpoint(carDealerCheckpoint);
        }

        public void ShowCarDealerUI(Player player)
        {
            if (config.carForSales.Length < 1)
                return;

            UIPanel carDealerPanel = new UIPanel(config.carShopName, UIPanel.PanelType.TabPrice)
                .AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                })
                .AddButton("Acheter", (ui) =>
                {
                    ui.SelectTab();
                });

            for(int i = 0; i < config.carForSales.Length; i++)
            {
                int modelId = config.carForSales[i].modelId;
                string modelName = Nova.v.vehicleModels[modelId].vehicleName;
                int price = config.carForSales[i].price;

                carDealerPanel.AddTabLine(modelName, $"{price}€", -1, (ui) =>
                {
                    ShowConfirmPanel(player, modelId, modelName, price);
                });
            }

            player.ShowPanelUI(carDealerPanel);
        }

        void ShowConfirmPanel(Player player, int modelId, string name, int price)
        {
            UIPanel confirmPanel = new UIPanel($"Confirmation d'achat - {name}", UIPanel.PanelType.Text)
                .SetText($"Êtes-vous sûr de vouloir acheter un/une {name} pour {price}€")
                .AddButton("Retour", (ui) =>
                {
                    ShowCarDealerUI(player);
                })
                .AddButton("Confirmer l'achat", (ui) =>
                {
                    if (player.character.Money < price)
                    {
                        player.SendText("Vous n'avez pas assez d'argent !");
                        return;
                    }

                    player.AddMoney(-price, "BUY_CAR");

                    Permissions permissions = new Permissions()
                    {
                        owner = new Entity()
                        {
                            characterId = player.character.Id
                        }
                    };

                    _ = LifeDB.CreateVehicle(modelId, JsonUtility.ToJson(permissions));

                    player.Notify("Véhicule acheté !", $"Vous avez acheté un/une {name} avec succès, retrouvez-le dans la liste de vos véhicules.", NotificationManager.Type.Success);
                    player.ClosePanel(ui);
                });

            player.ShowPanelUI(confirmPanel);
        }
    }

    [System.Serializable]
    public struct Position
    {
        public float x;
        public float y;
        public float z;

        public Position(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public Vector3 ToVector3() { return new Vector3(x, y, z);  }
    }

    [System.Serializable]
    public struct CarDealershipConfig
    {
        public string carShopName;
        public Position shopPosition;
        public CarForSale[] carForSales;
    }

    [System.Serializable]
    public class CarForSale
    {
        public int modelId;
        public int price;

        public CarForSale(int modelId, int price)
        {
            this.modelId = modelId;
            this.price = price;
        }
    }
}