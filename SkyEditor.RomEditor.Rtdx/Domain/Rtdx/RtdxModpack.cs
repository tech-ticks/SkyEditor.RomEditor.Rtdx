using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkyEditor.IO.FileSystem;
using SkyEditor.RomEditor.Domain.Rtdx.Models;
using SkyEditor.RomEditor.Domain.Rtdx.Constants;
using SkyEditor.RomEditor.Infrastructure.Automation.Modpacks;

namespace SkyEditor.RomEditor.Domain.Rtdx
{
  public class RtdxModpack : Modpack
  {
    public RtdxModpack(string path, IFileSystem fileSystem) : base(path, fileSystem)
    {
    }

    public static Modpack CreateInDirectory(ModpackMetadata metadata, string directory, IFileSystem fileSystem)
    {
      void ensureDirectoryExists(string directory)
      {
        if (!fileSystem.DirectoryExists(directory))
        {
          fileSystem.CreateDirectory(directory);
        }
      }

      string basePath = Path.Combine(directory, metadata.Id ?? "NewModpack");

      if (fileSystem.FileExists(Path.Combine(basePath, "modpack.yaml"))
        || fileSystem.FileExists(Path.Combine(basePath, "modpack.json"))
        || fileSystem.FileExists(Path.Combine(basePath, "mod.json")))
      {
        throw new InvalidOperationException("Cannot create modpack in directory with an existing modpack");
      }

      ensureDirectoryExists(basePath);
      ensureDirectoryExists(Path.Combine(basePath, "Scripts"));

      SaveMetadata(metadata, basePath, fileSystem).Wait();

      return new RtdxModpack(basePath, fileSystem);
    }

    protected async override Task ApplyModels(IModTarget target)
    {
      var rom = (IRtdxRom) target;
      foreach (var mod in Mods ?? Enumerable.Empty<Mod>())
      {
        // TODO: create derived RtdxMod class and move this stuff there
        if (mod.ModelExists("actors.yaml"))
        {
          rom.SetActors(await mod.LoadModel<ActorCollection>("actors.yaml"));
        }

        if (mod.ModelExists("starters.yaml"))
        {
          rom.SetStarters(await mod.LoadModel<StarterCollection>("starters.yaml"));
        }

        var dungeons = rom.GetDungeons();
        for (int i = 1; i < (int) DungeonIndex.END; i++)
        {
          await LoadDungeon(mod, rom, (DungeonIndex) i, dungeons);
        }

        if (mod.ModelExists("dungeon_maps.yaml"))
        {
          rom.SetDungeonMaps(await mod.LoadModel<DungeonMapCollection>("dungeon_maps.yaml"));
        }
        if (mod.ModelExists("dungeon_music.yaml"))
        {
          rom.SetDungeonMusic(await mod.LoadModel<DungeonMusicCollection>("dungeonmusic.yaml"));
        }
      }
    }

    private async Task LoadDungeon(Mod mod, IModTarget rom, DungeonIndex index, IDungeonCollection dungeons)
    {
      string dungeonFolder = Path.Combine("dungeons", index.ToString());
      string mainDataPath = Path.Combine(dungeonFolder, "dungeon.yaml");
      if (mod.ModelExists(mainDataPath))
      {
        var dungeonModel = await mod.LoadModel<DungeonModel>(mainDataPath);

        var itemSetsPath = Path.Combine(dungeonFolder, "itemsets");
        foreach (var path in mod.GetModelFilesInDirectory(itemSetsPath)
          .OrderBy(path => Path.GetFileNameWithoutExtension(path)))
        {
          dungeonModel.ItemSets.Add(await mod.LoadModel<ItemSetModel>(path));
        }

        var floorsPath = Path.Combine(dungeonFolder, "floors");
        var floorModels = new List<DungeonFloorModel>();
        foreach (var path in mod.GetModelFilesInDirectory(floorsPath))
        {
          var model = await mod.LoadModel<DungeonFloorModel>(path);
          floorModels.Add(model);
        }

        var sortedFloorModels = floorModels
          .OrderBy(model => model.Index).ToArray();

        // Floor -1 must be added last
        dungeonModel.Floors.AddRange(sortedFloorModels.Where(model => model.Index > -1));
        dungeonModel.Floors.AddRange(sortedFloorModels.Where(model => model.Index <= -1));

        dungeons.SetDungeon(index, dungeonModel);
      }
    }

    protected async override Task SaveModels(IModTarget target)
    {
      var rom = (IRtdxRom) target;

      // Models can only be automatically applied to the first mod
      var mod = Mods?.FirstOrDefault();
      if (mod != null)
      {
        var tasks = new List<Task>();

        // TODO: create derived RtdxMod class and move this stuff there
        if (rom.ActorsModified)
        {
          tasks.Add(mod.SaveModel(rom.GetActors(), "actors.yaml"));
        }

        if (rom.StartersModified)
        {
          tasks.Add(mod.SaveModel(rom.GetStarters(), "starters.yaml"));
        }

        if (rom.DungeonsModified)
        {
          var dungeons = rom.GetDungeons();
          foreach (var dungeon in dungeons.LoadedDungeons.Where(dungeon => dungeons.IsDungeonDirty(dungeon.Key)))
          {
            string dungeonFolder = Path.Combine("dungeons", dungeon.Key.ToString());
            string mainDataPath = Path.Combine(dungeonFolder, "dungeon.yaml");
            tasks.Add(mod.SaveModel(dungeon.Value, mainDataPath));

            for (int i = 0; i < dungeon.Value.ItemSets.Count; i++)
            {
              var itemSet = dungeon.Value.ItemSets[i];
              string path = Path.Combine(dungeonFolder, "itemsets", $"{i:D2}.yaml");
              tasks.Add(mod.SaveModel(itemSet, path));
            }
            for (int i = 0; i < dungeon.Value.Floors.Count; i++)
            {
              var floor = dungeon.Value.Floors[i];
              string path = Path.Combine(dungeonFolder, "floors", $"{floor.Index:D2}.yaml");
              tasks.Add(mod.SaveModel(floor, path));
            }
          }
        }

        if (rom.DungeonMapsModified)
        {
          tasks.Add(mod.SaveModel(rom.GetDungeonMaps(), "dungeon_maps.yaml"));
        }

        if (rom.DungeonMusicModified)
        {
          tasks.Add(mod.SaveModel(rom.GetDungeonMusic(), "dungeon_music.yaml"));
        }

        await Task.WhenAll(tasks);
      }
    }
  }
}
