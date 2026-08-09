using System.Collections.Generic;
using System.Linq;
using System.Text;
using SextantHorizon.Utils;
using UnityEngine;

namespace OpenLockerLib
{
    public static class Logic
    {
        private static float updateTime = -1f;

        private static List<StorageContainer> storages = new();

        public static void RefreshLocalStorage(float range = 30f)
        {
            Hooks.containers.RemoveAll(s => s == null);
            Vector3 player = Player.main.transform.position;
            List<StorageContainer> localStorage = Hooks.containers.FindAll(s => (s != null) && (Vector3.Distance(player, s.transform.position) <= range));
            storages = localStorage;
            Plugin.Logger.LogInfo($"Refreshed local storage. Found {storages.Count} storage containers within {range} meters, from the list of {Hooks.containers.Count}.");
            updateTime = Time.time;
        }

        public static int GetLocalPickupCount(TechType type)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage();
            int count = 0;
            StringBuilder stringBuilder = new();
            foreach (StorageContainer container in storages)
            {
                int current = container.container.GetCount(type);
                stringBuilder.AppendLine($"Local pickup count for {type} in {container.name}: {current}");
                count += current;
            }
            stringBuilder.AppendLine($"\nTotal local pickup count for {type}: {count}");
            Plugin.Logger.LogInfo(stringBuilder);
            return count;
        }

        public static List<StorageResource> GetStorageResources(StorageContainer container)
        {
            List<StorageResource> resources = new();
            foreach (TechType type in container.container.GetItemTypes())
            {
                resources.Add(new StorageResource(container, type));
            }
            return resources;
        }

        public static List<StorageResource> GetAllStorageResources()
        {
            List<StorageResource> resources = new();
            foreach (StorageContainer container in storages)
            {
                List<StorageResource> containerResources = GetStorageResources(container);
                foreach (TechType type in container.container.GetItemTypes())
                {
                    int total = containerResources.FindAll(r => r.Type == type).Sum(re => re.PickupCount);
                    resources.Add(new StorageResource(container, new Resource(type, total)));
                }
            }
            return resources;
        }

        public static bool ConsumeLocalResource(TechType type, int amount)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage();
            if(GetLocalPickupCount(type) < amount) return false;
            foreach(StorageContainer container in storages)
            {
                if(amount <= 0) break;
                if (container.container.GetCount(type) <= 0) continue;
                int consume = Mathf.Min(amount, container.container.GetCount(type));
                amount -= ConsumeResource(new(container, new Resource(type, consume))) ? consume : 0;
            }
            return amount <= 0;
        }

        public static bool ConsumeResource(StorageContainer container, TechType type, int amount) => ConsumeResources(new List<StorageResource> { new(container, new Resource(type, amount)) });

        public static bool ConsumeResources(List<(StorageContainer container, TechType type, int amount)> resourceList) => ConsumeResources(resourceList.Select(r => new StorageResource(r.container, new Resource(r.type, r.amount))).ToList());

        public static bool ConsumeResource(StorageResource resource) => ConsumeResources(new List<StorageResource> { resource });

        public static bool ConsumeResources(List<StorageResource> resources)
        {
            foreach (StorageResource removal in resources)
            {
                List<InventoryItem> items = removal.Container.container.GetItems(removal.Type).ToList();
                Resource resource = removal.Resource;
                foreach (InventoryItem item in items)
                {
                    if (resource <= 0) break;
                    Pickupable pickupable = item.item;

                    removal.Container.container.RemoveItem(pickupable, false);
                    resource -= 1;
                }
                if (resource > 0)
                {
                    Plugin.Logger.LogWarning($"Not enough {removal.Type} in {removal.Container.name} to consume {removal.Amount}. Remaining: {resource.Amount}");
                    return false;
                }
            }
            return true; // resources.All(r => r.Resource.Amount <= 0);
        }
    }
}
