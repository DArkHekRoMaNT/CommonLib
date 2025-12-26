// Credit to Apache from Vintage Story Discord

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.ServerMods.NoObf;

namespace CommonLib.Extensions
{
    /// <summary>
    ///     Extension methods for Tavis JSON Patching Engine.
    /// </summary>
    /// <example>
    ///     var patch = new JsonPatch
    ///     {
    ///         Op = EnumJsonPatchOp.Replace,
    ///         File = new AssetLocation("game:entities/land/drifter.json"),
    ///         Path = "/server/spawnconditions/runtime/maxQuantityByType/*-normal",
    ///         Value = JsonObject.FromJson("5")
    ///     };
    ///     api.ApplyJsonPatch(patch);
    /// </example>
    public static class TavisExtensions
    {
        private static int _dummyValue = 0;

        /// <summary>
        ///     Applies a single patch to a JSON file.
        /// </summary>
        /// <param name="api">The core API used by the game, on both the client, and the server.</param>
        /// <param name="patch">The patch to apply.</param>
        public static void ApplyJsonPatch(this ICoreAPI api, Vintagestory.ServerMods.NoObf.JsonPatch patch)
        {
            if (api == null)
            {
                const string message = "TavisExtensions.ApplyJsonPatch: api is null.";
                CommonLib.Utils.CommonLibLogger.Error(message);
                throw new ArgumentNullException(nameof(api), message);
            }
            if (patch == null)
            {
                const string message = "TavisExtensions.ApplyJsonPatch: patch is null.";
                api.Logger.Error(message);
                throw new ArgumentNullException(nameof(patch), message);
            }

            // Still using these awkward pass by reference dummy values.
            // Ideally, the part of the method that actually adds the patch should be extracted.
            var jsonPatcher = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
            if (jsonPatcher == null)
            {
                var message = $"TavisExtensions.ApplyJsonPatch: ModJsonPatchLoader not found. File={patch.File}, Path={patch.Path}, Op={patch.Op}";
                api.Logger.Error(message);
                throw new InvalidOperationException(message);
            }

            try
            {
                jsonPatcher.ApplyPatch(0, patch.File, patch, ref _dummyValue, ref _dummyValue, ref _dummyValue);
            }
            catch (Exception e)
            {
                var message = $"TavisExtensions.ApplyJsonPatch: ApplyPatch failed. File={patch.File}, Path={patch.Path}, Op={patch.Op}";
                api.Logger.Error(message);
                api.Logger.Error(e);
                throw new InvalidOperationException(message, e);
            }
        }

        /// <summary>
        ///     Applies a number of patches to the JSON assets of the game.
        /// </summary>
        /// <param name="api">The core API used by the game, on both the client, and the server.</param>
        /// <param name="patches">The patches to apply.</param>
        public static void ApplyJsonPatches(this ICoreAPI api, IEnumerable<Vintagestory.ServerMods.NoObf.JsonPatch> patches)
        {
            if (api == null)
            {
                const string message = "TavisExtensions.ApplyJsonPatches: api is null.";
                CommonLib.Utils.CommonLibLogger.Error(message);
                throw new ArgumentNullException(nameof(api), message);
            }
            if (patches == null)
            {
                const string message = "TavisExtensions.ApplyJsonPatches: patches is null.";
                api.Logger.Error(message);
                throw new ArgumentNullException(nameof(patches), message);
            }

            var jsonPatcher = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
            if (jsonPatcher == null)
            {
                const string message = "TavisExtensions.ApplyJsonPatches: ModJsonPatchLoader not found.";
                api.Logger.Error(message);
                throw new InvalidOperationException(message);
            }

            foreach (var patch in patches)
            {
                if (patch == null)
                {
                    const string message = "TavisExtensions.ApplyJsonPatches: patch is null.";
                    api.Logger.Error(message);
                    throw new ArgumentException(message, nameof(patches));
                }

                try
                {
                    jsonPatcher.ApplyPatch(0, patch.File, patch, ref _dummyValue, ref _dummyValue, ref _dummyValue);
                }
                catch (Exception e)
                {
                    var message = $"TavisExtensions.ApplyJsonPatches: ApplyPatch failed. File={patch.File}, Path={patch.Path}, Op={patch.Op}";
                    api.Logger.Error(message);
                    api.Logger.Error(e);
                    throw new InvalidOperationException(message, e);
                }
            }
        }
    }
}
