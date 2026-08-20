using SextantHorizon.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenLockerAPI
{
    public static class Logic
    {
        private static float updateTime = -1f;

        private static List<StorageContainer> storages = [];

        public static void RefreshLocalStorage(float range, Vector3? searchOrigin)
        {
            Hooks.containers.RemoveAll(s => s == null);
            Vector3 origin = searchOrigin ?? Player.main.transform.position;
            List<StorageContainer> localStorage = Hooks.containers.FindAll(s => (s != null) && (Vector3.Distance(origin, s.transform.position) <= range));
            storages = localStorage;
            updateTime = Time.time;
        }

        public static int GetLocalPickupCount(TechType type, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            int count = storages.Sum(s => s.container.GetCount(type));
            return count;
        }

        public static List<StorageResource> GetStorageResources(StorageContainer container)
        {
            List<StorageResource> resources = [];
            foreach (TechType type in container.container.GetItemTypes())
            {
                resources.Add(new StorageResource(container, type));
            }
            return resources;
        }

        public static List<StorageResource> GetAllStorageResources()
        {
            List<StorageResource> resources = [];
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

        public static bool ConsumeLocalResource(TechType type, int amount, Vector3? searchOrigin = null, float range = 30f)
        {
            if(GetLocalPickupCount(type, searchOrigin, range) < amount) return false;
            foreach(StorageContainer container in storages)
            {
                if(amount <= 0) break;
                if (container.container.GetCount(type) <= 0) continue;
                int consume = Mathf.Min(amount, container.container.GetCount(type));
                amount -= ConsumeResource(new(container, new Resource(type, consume))) ? consume : 0;
            }
            return amount <= 0;
        }
        public static bool AddResource(this StorageContainer container, InventoryItem resource) => ((IItemsContainer)container.container).AddItem(resource);
        public static bool HasRoomForResource(this StorageContainer container, TechType type) => container.container.HasRoomFor(type);
        public static bool HasRoomForResource(this StorageContainer container, Pickupable resource) => container.container.HasRoomFor(resource);
        public static bool HasRoomForResource(this StorageContainer container, int x, int y) => container.container.HasRoomFor(x, y);
        public static bool HasRoomForResource(this StorageContainer container, List<Vector2int> sizes) => container.container.HasRoomFor(sizes);
        public static bool HasRoomForResource(this StorageContainer container, InventoryItem resource) => container.container.HasRoomFor(resource.techType);
        public static bool HasRoomForLocalResource(InventoryItem resource, Vector3? searchOrigin = null, float range = 30f) => HasRoomForLocalResource(resource.item, searchOrigin, range);
        public static bool HasRoomForLocalResource(Pickupable resource, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            return storages.Any(s => s.container.HasRoomFor(resource));
        }
        public static bool HasRoomForLocalResource(TechType type, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            return storages.Any(s => s.container.HasRoomFor(type));
        }
        public static bool HasRoomForLocalResource(int x, int y, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            return storages.Any(s => s.container.HasRoomFor(x, y));
        }
        public static bool HasRoomForLocalResource(List<Vector2int> sizes, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            return storages.Any(s => s.container.HasRoomFor(sizes));
        }


        public static bool DepositLocalResource(InventoryItem resource, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            return storages.Any(s => s.HasRoomForResource(resource) && s.AddResource(resource));
        }
        public static bool DepositLocalResources(IList<InventoryItem> resources, Vector3? searchOrigin = null, float range = 30f)
        {
            if (Time.time - updateTime > 1) RefreshLocalStorage(range, searchOrigin);
            while(resources.Count > 0)
            {
                foreach (InventoryItem resource in resources)
                {
                    if(!storages.Any(s => s.HasRoomForResource(resource) && s.AddResource(resource))) return false;
                }
            }
            return true;
        }

        public static bool ConsumeResource(StorageContainer container, TechType type, int amount) => ConsumeResources(new List<StorageResource> { new(container, new Resource(type, amount)) });

        public static bool ConsumeResources(List<(StorageContainer container, TechType type, int amount)> resourceList) => ConsumeResources(resourceList.Select(r => new StorageResource(r.container, new Resource(r.type, r.amount))).ToList());

        public static bool ConsumeResource(StorageResource resource) => ConsumeResources([resource]);

        public static bool ConsumeResources(List<StorageResource> resources)
        {
            foreach (StorageResource removal in resources)
            {
                List<InventoryItem> items = [.. removal.Container.container.GetItems(removal.Type)];
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
                    Plugin.Logger.LogError($"Not enough {removal.Type} in {removal.Container.name} to consume {removal.Amount}. Remaining: {resource.Amount}");
                    return false;
                }
            }
            return true;
        }
    }
}
