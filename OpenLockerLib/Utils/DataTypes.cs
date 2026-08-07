using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenLockerLib.Utils
{
    public record StorageResource(StorageContainer Container, Resource Resource)
    {
        public StorageResource(StorageContainer container, TechType type) : this(container, new Resource(type)) { }
        public int PickupCount => Container.container.GetCount(Resource.Type);
        public int Yield => Resources.Yield(Resource.Type);
        public TechType Type => Resource.Type;
        public List<Resource> Components => Resources.ComponentsOf(Resource.Type);
        public float CraftTime => Resources.CraftTime(Resource.Type);
        public static implicit operator Resource(StorageResource storageResource) => storageResource.Resource;
    }

    public static class Resources
    {
        public static bool Craftable(TechType type) => CraftTree.IsCraftable(type);
        public static int PickupCount(TechType type) => Compatibility.ResourceCount(type);
        public static int Yield(TechType type) => Mathf.Max(TechData.GetCraftAmount(type), 1);
        public static float CraftTime(TechType type) => TechData.GetCraftTime(type, out float time) ? time : 0;

        public static List<Resource> ListOf(Dictionary<TechType, int> dictionary) => dictionary.Select(keyPair => (Resource)keyPair).ToList();
        public static List<Resource> ListOf(List<Ingredient> ingredients) => ingredients.Select(ing => (Resource)ing).ToList();
        public static List<Ingredient> ListOf(List<Resource> resources) => resources.Select(res => (Ingredient)res).ToList();

        public static List<Resource> ComponentsOf(TechType type)
        {
            List<Resource> ingredients = new();
            if (type == TechType.None || !CraftTree.IsCraftable(type)) return ingredients;
            ReadOnlyCollection<Ingredient> ingredientArray = TechData.GetIngredients(type);
            foreach (Ingredient ingredient in ingredientArray) ingredients.Add(ingredient);
            return ingredients;
        }
    }


    public record Resource(TechType Type, int Amount)
    {
        public Resource(TechType type) : this(type, 1) { }
        public Resource(KeyValuePair<TechType, int> pair) : this(pair.Key, pair.Value) { }
        public Resource(Ingredient ingredient) : this(ingredient.techType, ingredient.amount) { }
        public Resource() : this(TechType.None) { }
        public Resource(uGUI_CraftingMenu.Node node) : this(node.techType, 1) { }


        public bool Craftable => Resources.Craftable(Type);
        public int PickupCount => Resources.PickupCount(Type);
        public int Yield => Resources.Yield(Type);
        public List<Resource> Components => Resources.ComponentsOf(Type);
        public float CraftTime => Resources.CraftTime(Type);


        public static implicit operator Ingredient(Resource resource) => new(resource.Type, resource.Amount);
        public static implicit operator Resource(uGUI_CraftingMenu.Node node) => new(node);
        public static implicit operator Resource(KeyValuePair<TechType, int> pair) => new(pair);
        public static implicit operator Resource(Ingredient ingredient) => new(ingredient);
        public static Resource operator +(Resource resource, int value) => resource with { Amount = resource.Amount + value };
        public static Resource operator -(Resource resource, int value) => resource with { Amount = Mathf.Max(0, resource.Amount - value) };
        public static Resource operator *(Resource resource, int value) => resource with { Amount = resource.Amount * value };
        public static Resource operator /(Resource resource, int value) => resource with { Amount = resource.Amount / Mathf.Max(1, value) };
        public static bool operator >(Resource resource, int value) => resource.Amount > value;
        public static bool operator <(Resource resource, int value) => resource.Amount < value;
        public static bool operator >=(Resource resource, int value) => resource.Amount >= value;
        public static bool operator <=(Resource resource, int value) => resource.Amount <= value;
        public static bool operator ==(Resource resource, int value) => resource.Amount == value;
        public static bool operator !=(Resource resource, int value) => resource.Amount != value;
        public static bool operator ==(Resource resource, TechType value) => resource.Type == value;
        public static bool operator !=(Resource resource, TechType value) => resource.Type != value;


        public override string ToString() => $"{Type}: {Amount}";
    }

    public record ResourceTable(Dictionary<TechType, int> Table)
    {
        public ResourceTable() : this(new Dictionary<TechType, int>()) { }
        public ResourceTable(List<Resource> resources) : this(resources.ToDictionary(entry => entry.Type, entry => entry.Amount)) { }
        public ResourceTable(List<Ingredient> resources) : this(Resources.ListOf(resources)) { }
        public bool Contains(TechType type) => Table.ContainsKey(type);
        public bool Contains(Resource resource) => Contains(resource.Type);
        public Dictionary<TechType, int>.KeyCollection Keys => Table.Keys;
        public Dictionary<TechType, int>.ValueCollection Values => Table.Values;

        public Resource this[TechType type]
        {
            get
            {
                if (Table.TryGetValue(type, out int amount)) return new(type, amount);
                return null;
            }
            set
            {
                Set(type, value.Amount);
            }
        }
        public void Set(TechType type, int ammount) => Table[type] = ammount;

        public bool Add(TechType type, int amount)
        {
            if (Contains(type))
            {
                Table[type] += amount;
                return true;
            }
            Table[type] = amount;
            return false;
        }
        public bool TryAdd(TechType type, int amount)
        {
            if (Contains(type)) return false;
            Table[type] = amount;
            return true;
        }
        public Resource GetOrSet(TechType type, int amount)
        {
            if (Contains(type)) return this[type];
            Table[type] = amount;
            return this[type];
        }
        public bool AddAll(List<Resource> resources)
        {
            bool anyAdded = false;
            foreach (Resource resource in resources) anyAdded |= Add(resource);
            return anyAdded;
        }
        public void Subtract(TechType type, int amount)
        {
            if (Contains(type))
            {
                Table[type] -= amount;
                if (Table[type] <= 0) Remove(type);
            }
        }
        public int AmountOf(TechType type) => Contains(type) ? Table[type] : 0;
        public void Remove(TechType type) => Table.Remove(type);
        public void Set(Resource resource) => Set(resource.Type, resource.Amount);
        public bool Add(Resource resource) => Add(resource.Type, resource.Amount);
        public bool TryAdd(Resource resource) => TryAdd(resource.Type, resource.Amount);
        public Resource GetOrSet(Resource resource) => GetOrSet(resource.Type, resource.Amount);
        public bool AddAll(ResourceTable resourceTable) => AddAll(resourceTable.ToList());
        public int AmountOf(Resource resource) => AmountOf(resource.Type);
        public void Remove(Resource resource) => Remove(resource.Type);
        public void Subtract(Resource resource) => Subtract(resource.Type, resource.Amount);
        public void Clear() => Table.Clear();
        public List<Resource> ToList() => ToList(this);
        public Dictionary<TechType, int>.Enumerator GetEnumerator() => Table.GetEnumerator();

        public static List<Resource> ToList(ResourceTable resourceTable) => Resources.ListOf(resourceTable.Table);
        public static implicit operator List<Resource>(ResourceTable resources) => ToList(resources);
        public static implicit operator ResourceTable(List<Resource> resources) => new(resources);
        public static implicit operator ResourceTable(Dictionary<TechType, int> resources) => new(resources);

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (Resource resource in Table) sb.AppendLine(resource.ToString());
            return sb.ToString();
        }
    }
}
