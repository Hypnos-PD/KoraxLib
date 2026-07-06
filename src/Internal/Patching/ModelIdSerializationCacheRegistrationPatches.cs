using HarmonyLib;
using KoraxLib.Enemies;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace KoraxLib.Internal.Patching;

/// <summary>
/// 将 KoraxLib 注册内容的 <see cref="ModelId" /> 补入 STS2 的网络/存档序列化 ID 缓存。
/// </summary>
/// <remarks>
/// STS2 的 <see cref="ModelIdSerializationCache.Init" /> 从 subtype 扫描建立 entry 映射，某些 mod 类型即使已注入
/// <see cref="ModelDb" /> 也可能不会被该扫描拾取。若不补入缓存，保存 run 时会把 model id 写成 NONE。
/// </remarks>
[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
internal static class ModelIdSerializationCacheRegistrationPatches
{
    /// <summary>
    /// 在 STS2 建立序列化缓存后，追加 KoraxLib 已注册的 monster / encounter id。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void AddRegisteredContentIdsAfterCacheInit()
    {
        var categoryMap = GetCacheField<Dictionary<string, int>>("_categoryNameToNetIdMap");
        var categoryList = GetCacheField<List<string>>("_netIdToCategoryNameMap");
        var entryMap = GetCacheField<Dictionary<string, int>>("_entryNameToNetIdMap");
        var entryList = GetCacheField<List<string>>("_netIdToEntryNameMap");

        if (categoryMap is null || categoryList is null || entryMap is null || entryList is null)
        {
            Entry.Logger.Warn("KoraxLib could not access ModelIdSerializationCache internals; registered encounter ids may serialize as NONE.");
            return;
        }

        var added = 0;
        foreach (var modelType in RegisteredModelTypes())
        {
            var id = ModelDb.GetId(modelType);
            added += EnsureId(id, categoryMap, categoryList, entryMap, entryList) ? 1 : 0;
        }

        if (added == 0)
        {
            return;
        }

        SetCacheProperty(nameof(ModelIdSerializationCache.CategoryIdBitSize), GetBitSize(categoryList.Count));
        SetCacheProperty(nameof(ModelIdSerializationCache.EntryIdBitSize), GetBitSize(entryList.Count));
        Entry.Logger.Info($"KoraxLib added {added} registered model id(s) to ModelIdSerializationCache.");
    }

    private static IEnumerable<Type> RegisteredModelTypes()
    {
        return EnemyRegistry.RegisteredMonsters
            .Concat(EnemyRegistry.RegisteredActEncounters.Values
            .SelectMany(static encounterTypes => encounterTypes)
            .Concat(EnemyRegistry.RegisteredGlobalEncounters))
            .Distinct();
    }

    private static bool EnsureId(
        ModelId id,
        Dictionary<string, int> categoryMap,
        List<string> categoryList,
        Dictionary<string, int> entryMap,
        List<string> entryList)
    {
        var added = false;
        if (!categoryMap.ContainsKey(id.Category))
        {
            categoryMap[id.Category] = categoryList.Count;
            categoryList.Add(id.Category);
            added = true;
        }

        if (!entryMap.ContainsKey(id.Entry))
        {
            entryMap[id.Entry] = entryList.Count;
            entryList.Add(id.Entry);
            added = true;
        }

        return added;
    }

    private static T? GetCacheField<T>(string name)
        where T : class
    {
        return AccessTools.DeclaredField(typeof(ModelIdSerializationCache), name)?.GetValue(null) as T;
    }

    private static void SetCacheProperty(string name, object value)
    {
        var property = typeof(ModelIdSerializationCache).GetProperty(name);
        property?.GetSetMethod(true)?.Invoke(null, [value]);
    }

    private static int GetBitSize(int count)
    {
        return count <= 1 ? 0 : Godot.Mathf.CeilToInt(Math.Log2(count));
    }
}
