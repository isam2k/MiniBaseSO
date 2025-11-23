using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Delaunay.Geo;
using HarmonyLib;
using Klei;
using Klei.CustomSettings;
using MiniBase.Model;
using MiniBase.Model.Enums;
using MiniBase.Model.Profiles;
using ProcGen;
using ProcGenGame;
using TemplateClasses;
using UnityEngine;
using VoronoiTree;

//using static MiniBase.MiniBaseOptions;

namespace MiniBase
{
    public static class MiniBaseWorldGen
    {
        public static void CreateWorld(
            WorldGen gen,
            BinaryWriter writer,
            uint simSeed,
            int baseId,
            out Sim.Cell[] cells,
            out Sim.DiseaseCell[] dc)
        {
            var moonletData = new MoonletData(gen);
            if (DlcManager.IsExpansion1Active())
            {
                CreateSoWorld(gen, writer, simSeed, baseId, moonletData, out cells, out dc);
            }
            else
            {
                CreateVanillaWorld(gen, writer, simSeed, baseId, moonletData, out cells, out dc);
            }
        }

        private static void CreateVanillaWorld(WorldGen gen, BinaryWriter writer, uint simSeed, int baseId, MoonletData moonletData,
            out Sim.Cell[] cells,
            out Sim.DiseaseCell[] dc)
        {
            var options = MiniBaseOptions.Instance;

            // Convenience variables, including private fields/properties
            var worldGen = Traverse.Create(gen);
            var data = gen.data;
            var updateProgressFn = worldGen.Field("successCallbackFn").GetValue<WorldGen.OfflineCallbackFunction>();
            var errorCallback = worldGen.Field("errorCallback").GetValue<Action<OfflineWorldGen.ErrorInfo>>();
            var running = worldGen.Field("running");
            running.SetValue(true);
            _random = new System.Random(data.globalTerrainSeed);

            // Initialize noise maps
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0f,
                WorldGenProgressStages.Stages.NoiseMapBuilder);
            var noiseMap = GenerateNoiseMap(_random, moonletData.WorldSize.x, moonletData.WorldSize.y);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0.9f,
                WorldGenProgressStages.Stages.NoiseMapBuilder);

            // Set biomes
            SetBiomes(data.overworldCells, moonletData);

            // Rewrite of WorldGen.RenderToMap function, which calls the default terrain and border generation, places features, and spawns flora and fauna
            cells = new Sim.Cell[Grid.CellCount];
            var bgTemp = new float[Grid.CellCount];
            dc = new Sim.DiseaseCell[Grid.CellCount];

            // Initialize terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 0f,
                WorldGenProgressStages.Stages.ClearingLevel);
            ClearTerrain(cells, bgTemp, dc);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 1f,
                WorldGenProgressStages.Stages.ClearingLevel);

            // Draw custom terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0f, WorldGenProgressStages.Stages.Processing);
            var biomeCells = DrawCustomTerrain(moonletData, data, cells, dc, noiseMap, out var coreCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0.9f,
                WorldGenProgressStages.Stages.Processing);

            // Printing pod
            var startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1,
                moonletData.Bottom() + (moonletData.Height() / 2) + 2);
            data.gameSpawnData.baseStartPos = startPos;
            var templateSpawnTargets = new List<TemplateSpawning.TemplateSpawner>();
            var reservedCells = new HashSet<Vector2I>();
            var startingBaseTemplate = TemplateCache.GetTemplate(gen.Settings.world.startingBaseTemplate);

            startingBaseTemplate.pickupables.Clear(); // Remove stray hatch
            var itemPos = new Vector2I(3, 1);
            foreach (var entry in moonletData.Biome.StartingItems) // Add custom defined starting items
            {
                startingBaseTemplate.pickupables.Add(new Prefab(entry.Key, Prefab.Type.Pickupable, itemPos.x, itemPos.y,
                    0, _units: entry.Value));
            }

            foreach (var cell in startingBaseTemplate.cells)
            {
                if (cell.element == SimHashes.SandStone || cell.element == SimHashes.Algae)
                {
                    cell.element = moonletData.Biome.DefaultMaterial;
                }

                reservedCells.Add(new Vector2I(cell.location_x, cell.location_y) + startPos);
            }

            var terrainCell =
                data.overworldCells.Find(tc =>
                    tc.node.tags.Contains(WorldGenTags.StartLocation));
            startingBaseTemplate.cells.RemoveAll((c) =>
                (c.location_x == -8) || (c.location_x == 9)); // Trim the starting base area
            templateSpawnTargets.Add(new TemplateSpawning.TemplateSpawner(startPos,
                startingBaseTemplate.GetTemplateBounds(), startingBaseTemplate, terrainCell));

            // Geysers
            var geyserMinX = moonletData.Left() + MiniBaseOptions.CornerSize + 2;
            var geyserMaxX = moonletData.Right() - MiniBaseOptions.CornerSize - 4;
            var geyserMinY = moonletData.Bottom() + MiniBaseOptions.CornerSize + 2;
            var geyserMaxY = moonletData.Top() - MiniBaseOptions.CornerSize - 4;
            var coverElement = moonletData.Biome.DefaultElement();
            PlaceGeyser(data, cells, options.FeatureWest,
                new Vector2I(moonletData.Left() + 2, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
            PlaceGeyser(data, cells, options.FeatureEast,
                new Vector2I(moonletData.Right() - 4, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
            if (options.HasCore())
            {
                coverElement = moonletData.CoreBiome.DefaultElement();
            }

            PlaceGeyser(data, cells, options.FeatureSouth,
                new Vector2I(_random.Next(geyserMinX, geyserMaxX + 1), moonletData.Bottom()), coverElement);

            // Change geysers to be made of abyssalite so they don't melt in magma
            var geyserPrefabs = Assets.GetPrefabsWithComponent<Geyser>();
            geyserPrefabs.Add(Assets.GetPrefab(GameTags.OilWell));
            foreach (var prefab in geyserPrefabs)
            {
                prefab.GetComponent<PrimaryElement>().SetElement(SimHashes.Katairite);
            }

            // Draw borders
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 0f,
                WorldGenProgressStages.Stages.DrawWorldBorder);
            var borderCells = DrawCustomWorldBorders(moonletData, cells);
            biomeCells.ExceptWith(borderCells);
            coreCells.ExceptWith(borderCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 1f,
                WorldGenProgressStages.Stages.DrawWorldBorder);

            // Settle simulation
            // This writes the cells to the world, then performs a couple of game frames of simulation, then saves the game
            running.SetValue(WorldGenSimUtil.DoSettleSim(
                gen.Settings,
                writer,
                simSeed,
                ref cells,
                ref bgTemp,
                ref dc,
                updateProgressFn,
                data,
                templateSpawnTargets,
                errorCallback,
                baseId));

            // Place templates, pretty much just the printing pod
            var claimedCells = new Dictionary<int, int>();
            foreach (var templateSpawner in templateSpawnTargets)
            {
                data.gameSpawnData.AddTemplate(templateSpawner.container, templateSpawner.position, ref claimedCells);
            }

            // Add plants, critters, and items
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0f,
                WorldGenProgressStages.Stages.PlacingCreatures);
            PlaceSpawnables(cells, data.gameSpawnData.pickupables, moonletData.Biome, biomeCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 50f,
                WorldGenProgressStages.Stages.PlacingCreatures);
            PlaceSpawnables(cells, data.gameSpawnData.pickupables, moonletData.CoreBiome, coreCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 100f,
                WorldGenProgressStages.Stages.PlacingCreatures);

            // Finish and save
            updateProgressFn(global::STRINGS.UI.WORLDGEN.COMPLETE.key, 1f, WorldGenProgressStages.Stages.Complete);
            running.SetValue(false);
        }

        private static void CreateSoWorld(WorldGen gen, BinaryWriter writer, uint simSeed, int baseId, MoonletData moonletData,
            out Sim.Cell[] cells,
            out Sim.DiseaseCell[] dc)
        {
            var options = MiniBaseOptions.Instance;

            // Convenience variables, including private fields/properties
            var worldGen = Traverse.Create(gen);
            var updateProgressFn = worldGen.Field("successCallbackFn").GetValue<WorldGen.OfflineCallbackFunction>();
            var errorCallback = worldGen.Field("errorCallback").GetValue<Action<OfflineWorldGen.ErrorInfo>>();
            var running = worldGen.Field("running");
            running.SetValue(true);
            _random = new System.Random(gen.data.globalTerrainSeed);

            // Initialize noise maps
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0f,
                WorldGenProgressStages.Stages.NoiseMapBuilder);
            var noiseMap = GenerateNoiseMap(_random, moonletData.WorldSize.x, moonletData.WorldSize.y);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0.9f,
                WorldGenProgressStages.Stages.NoiseMapBuilder);

            // Set biomes
            SetBiomes(gen.data.overworldCells, moonletData);

            // Rewrite of WorldGen.RenderToMap function, which calls the default terrain and border generation, places features, and spawns flora and fauna
            cells = new Sim.Cell[Grid.CellCount];
            var bgTemp = new float[Grid.CellCount];
            dc = new Sim.DiseaseCell[Grid.CellCount];

            // Initialize terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 0f,
                WorldGenProgressStages.Stages.ClearingLevel);
            ClearTerrain(cells, bgTemp, dc);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 1f,
                WorldGenProgressStages.Stages.ClearingLevel);

            // Draw custom terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0f, WorldGenProgressStages.Stages.Processing);
            var biomeCells = DrawCustomTerrain(moonletData, gen.data, cells, dc, noiseMap, out var coreCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0.9f,
                WorldGenProgressStages.Stages.Processing);

            // Change geysers to be made of abyssalite so they don't melt in magma
            var geyserPrefabs = Assets.GetPrefabsWithComponent<Geyser>();
            geyserPrefabs.Add(Assets.GetPrefab(GameTags.OilWell));
            foreach (var prefab in geyserPrefabs)
            {
                prefab.GetComponent<PrimaryElement>().SetElement(SimHashes.Katairite);
            }

            var templateSpawnTargets = new List<TemplateSpawning.TemplateSpawner>();
            var reservedCells = new HashSet<Vector2I>();
            Vector2I startPos;
            int geyserMinX, geyserMaxX, geyserMinY, geyserMaxY;
            Sim.PhysicsData physicsData;
            Element coverElement;
            TerrainCell terrainCell;
            switch (moonletData.Type)
            {
                case Moonlet.Start:
                    // Printing pod
                    startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1,
                        moonletData.Bottom() + (moonletData.Height() / 2) + 2);
                    gen.data.gameSpawnData.baseStartPos = startPos;
                    var startingBaseTemplate = TemplateCache.GetTemplate(gen.Settings.world.startingBaseTemplate);
                    startingBaseTemplate.pickupables.Clear(); // Remove stray hatch
                    var itemPos = new Vector2I(3, 1);
                    foreach (var entry in moonletData.Biome.StartingItems) // Add custom defined starting items
                    {
                        startingBaseTemplate.pickupables.Add(new Prefab(entry.Key, Prefab.Type.Pickupable, itemPos.x,
                            itemPos.y, 0, _units: entry.Value));
                    }

                    foreach (var cell in startingBaseTemplate.cells)
                    {
                        if (cell.element == SimHashes.SandStone || cell.element == SimHashes.Algae)
                        {
                            cell.element = moonletData.Biome.DefaultMaterial;
                        }

                        reservedCells.Add(new Vector2I(cell.location_x, cell.location_y) + startPos);
                    }

                    terrainCell =
                        gen.data.overworldCells.Find(tc =>
                            tc.node.tags.Contains(WorldGenTags.StartLocation));
                    startingBaseTemplate.cells.RemoveAll((c) =>
                        (c.location_x == -8) || (c.location_x == 9)); // Trim the starting base area
                    templateSpawnTargets.Add(new TemplateSpawning.TemplateSpawner(startPos,
                        startingBaseTemplate.GetTemplateBounds(), startingBaseTemplate, terrainCell));

                    // Geysers
                    geyserMinX = moonletData.Left() + MiniBaseOptions.CornerSize + 2;
                    geyserMaxX = moonletData.Right() - MiniBaseOptions.CornerSize - 4;
                    geyserMinY = moonletData.Bottom() + MiniBaseOptions.CornerSize + 2;
                    geyserMaxY = moonletData.Top() - MiniBaseOptions.CornerSize - 4;
                    coverElement = moonletData.Biome.DefaultElement();
                    PlaceGeyser(gen.data, cells, options.FeatureWest,
                        new Vector2I(moonletData.Left() + 2, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
                    PlaceGeyser(gen.data, cells, options.FeatureEast,
                        new Vector2I(moonletData.Right() - 4, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
                    if (moonletData.HasCore)
                    {
                        coverElement = moonletData.CoreBiome.DefaultElement();
                    }

                    var bottomGeyserPos = new Vector2I(_random.Next(geyserMinX, geyserMaxX + 1), moonletData.Bottom());
                    PlaceGeyser(gen.data, cells, options.FeatureSouth, bottomGeyserPos, coverElement);

                    // teleporters
                    reservedCells.UnionWith(PlaceWarpPortals(gen.data, cells, moonletData));

                    if (moonletData.HasCore &&
                        moonletData.CoreBiome == MiniBaseCoreBiomeProfiles.RadioactiveCoreProfile)
                    {
                        // Add a single viable beeta in the radioactive core
                        bottomGeyserPos.x +=
                            bottomGeyserPos.x > moonletData.Left() + moonletData.Width() / 2 ? -12 : 12;

                        bottomGeyserPos.y += 1;
                        coverElement = ElementLoader.FindElementByHash(SimHashes.Wolframite);

                        cells[Grid.XYToCell(bottomGeyserPos.x, bottomGeyserPos.y)].SetValues(coverElement,
                            GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature),
                            ElementLoader.elements);
                        cells[Grid.XYToCell(bottomGeyserPos.x + 1, bottomGeyserPos.y)].SetValues(coverElement,
                            GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature),
                            ElementLoader.elements);

                        coverElement = ElementLoader.FindElementByHash(SimHashes.CarbonDioxide);

                        physicsData = GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature);
                        for (var x = 0; x < 2; x++)
                        {
                            for (var y = 0; y < 3; y++)
                            {
                                cells[Grid.XYToCell(bottomGeyserPos.x + x, bottomGeyserPos.y + 1 + y)]
                                    .SetValues(coverElement, physicsData, ElementLoader.elements);
                            }
                        }

                        gen.data.gameSpawnData.pickupables.Add(new Prefab("BeeHive", Prefab.Type.Pickupable,
                            bottomGeyserPos.x, bottomGeyserPos.y + 1, 0));
                    }

                    break;
                case Moonlet.Second:
                    // teleporters
                    reservedCells.UnionWith(PlaceWarpPortals(gen.data, cells, moonletData));

                    // geysers
                    geyserMinX = moonletData.Left() + MiniBaseOptions.CornerSize + 2;
                    geyserMaxX = moonletData.Right() - MiniBaseOptions.CornerSize - 4;
                    geyserMinY = moonletData.Bottom() + MiniBaseOptions.CornerSize + 2;
                    geyserMaxY = moonletData.Top() - moonletData.Height() / 2 - MiniBaseOptions.CornerSize - 4;
                    coverElement = moonletData.Biome.DefaultElement();
                    PlaceGeyser(gen.data, cells, FeatureType.OilReservoir,
                        new Vector2I(moonletData.Left() + 2, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement,
                        false, false);
                    PlaceGeyser(gen.data, cells, FeatureType.OilReservoir,
                        new Vector2I(moonletData.Right() - 4, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement,
                        false, false);
                    if (moonletData.HasCore)
                    {
                        coverElement = moonletData.CoreBiome.DefaultElement();
                    }

                    PlaceGeyser(gen.data, cells, FeatureType.Volcano,
                        new Vector2I(_random.Next(geyserMinX, geyserMaxX + 1), moonletData.Bottom()), coverElement);
                    break;
                case Moonlet.Tree:
                    startPos = new Vector2I(moonletData.Left() + (2 * moonletData.Width() / 3) - 1,
                        moonletData.Bottom() + (moonletData.Height() / 2) + 2);
                    var template = TemplateCache.GetTemplate("expansion1::poi/sap_tree_room");
                    terrainCell =
                        gen.data.overworldCells.Find(tc =>
                            tc.node.tags.Contains(WorldGenTags.AtSurface));
                    templateSpawnTargets.Add(new TemplateSpawning.TemplateSpawner(startPos,
                        template.GetTemplateBounds(), template, terrainCell));
                    break;
                case Moonlet.Niobium:
                    startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1,
                        moonletData.Bottom() + (moonletData.Height() / 4) + 2);
                    coverElement = ElementLoader.FindElementByHash(SimHashes.Niobium);
                    PlaceGeyser(gen.data, cells, FeatureType.Niobium, startPos, coverElement);

                    // Replace niobium near the surface with obsidian
                    coverElement = ElementLoader.FindElementByHash(SimHashes.Obsidian);
                    physicsData = GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature);
                    for (var x = moonletData.Left(); x <= moonletData.Right(); x++)
                    {
                        for (var y = moonletData.Top(); y >= (moonletData.Top() - (3 * moonletData.Height() / 4)); y--)
                        {
                            if (ElementLoader.elements[cells[Grid.XYToCell(x, y)].elementIdx].id == SimHashes.Niobium)
                            {
                                cells[Grid.XYToCell(x, y)].SetValues(coverElement, physicsData, ElementLoader.elements);
                            }
                        }
                    }

                    break;
            }

            // Draw borders
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 0f,
                WorldGenProgressStages.Stages.DrawWorldBorder);
            var borderCells = DrawCustomWorldBorders(moonletData, cells);
            biomeCells.ExceptWith(borderCells);
            coreCells.ExceptWith(borderCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 1f,
                WorldGenProgressStages.Stages.DrawWorldBorder);

            // Settle simulation
            // This writes the cells to the world, then performs a couple of game frames of simulation, then saves the game
            running.SetValue(WorldGenSimUtil.DoSettleSim(
                gen.Settings,
                writer,
                simSeed,
                ref cells,
                ref bgTemp,
                ref dc,
                updateProgressFn,
                gen.data,
                templateSpawnTargets,
                errorCallback,
                baseId));

            // Place templates, pretty much just the printing pod
            var claimedCells = new Dictionary<int, int>();
            foreach (var templateSpawner in templateSpawnTargets)
            {
                gen.data.gameSpawnData.AddTemplate(templateSpawner.container, templateSpawner.position,
                    ref claimedCells);
            }

            // Add plants, critters, and items
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0f,
                WorldGenProgressStages.Stages.PlacingCreatures);
            PlaceSpawnables(cells, gen.data.gameSpawnData.pickupables, moonletData.Biome, biomeCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0.5f,
                WorldGenProgressStages.Stages.PlacingCreatures);
            if (moonletData.HasCore)
            {
                PlaceSpawnables(cells, gen.data.gameSpawnData.pickupables, moonletData.CoreBiome, coreCells,
                    reservedCells);
            }

            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 1f,
                WorldGenProgressStages.Stages.PlacingCreatures);

            // Finish
            updateProgressFn(global::STRINGS.UI.WORLDGEN.COMPLETE.key, 1f, WorldGenProgressStages.Stages.Complete);
            running.SetValue(false);
        }

        /// <summary>
        /// Set biome background for all cells
        /// Overworld cells are large polygons that divide the map into zones (biomes)
        /// To change a biome, create an appropriate overworld cell and add it to Data.overworldCells
        /// </summary>
        /// <param name="overworldCells"></param>
        /// <param name="moonletData"></param>
        private static void SetBiomes(List<TerrainCell> overworldCells, MoonletData moonletData)
        {
            overworldCells.Clear();

            const string spaceBiome = "subworlds/space/Space";
            var backgroundBiome = moonletData.Biome.BackgroundSubworld;

            uint cellId = 0;

            var createOverworldCell = new Action<string, Polygon, TagSet>((type, bs, ts) =>
            {
                var cell = new ProcGen.Map.Cell(); // biome
                cell.SetType(type);
                foreach (var tag in ts)
                {
                    cell.tags.Add(tag);
                }

                var site = new Diagram.Site();
                site.id = cellId++;
                site.poly = bs; // bounds of the overworld cell
                site.position = site.poly.Centroid();
                overworldCells.Add(new TerrainCellLogged(cell, site, new Dictionary<Tag, int>()));
            });

            // Vertices of the liveable area (octogon)
            var cornerSize = MiniBaseOptions.Instance.TeleporterPlacement == WarpPlacementType.Corners
                ? 0
                : MiniBaseOptions.CornerSize;
            Vector2I bottomLeftSe = moonletData.BottomLeft() + new Vector2I(cornerSize, 0),
                bottomLeftNw = moonletData.BottomLeft() + new Vector2I(0, cornerSize),
                topLeftSw = moonletData.TopLeft() - new Vector2I(0, cornerSize) - new Vector2I(0, 1),
                topLeftNe = moonletData.TopLeft() + new Vector2I(cornerSize, 0),
                topRightNw = moonletData.TopRight() - new Vector2I(cornerSize, 0) - new Vector2I(1, 0),
                topRightSe = moonletData.TopRight() - new Vector2I(0, cornerSize) - new Vector2I(0, 1),
                bottomRightNe = moonletData.BottomRight() + new Vector2I(0, cornerSize),
                bottomRightSw = moonletData.BottomRight() - new Vector2I(cornerSize, 0);

            // Liveable cell
            var tags = new TagSet();

            if (moonletData.Type == Moonlet.Start)
            {
                tags.Add(WorldGenTags.AtStart);
                tags.Add(WorldGenTags.StartWorld);
                tags.Add(WorldGenTags.StartLocation);
            }

            var vertices = new List<Vector2>()
            {
                bottomLeftSe,
                bottomLeftNw,
                topLeftSw,
                topLeftNe,
                topRightNw,
                topRightSe,
                bottomRightNe,
                bottomRightSw
            };
            var bounds = new Polygon(vertices);
            createOverworldCell(backgroundBiome, bounds, tags);

            // Top cell
            tags = new TagSet();
            bounds = new Polygon(new Rect(0f, moonletData.Top(), moonletData.WorldSize.x,
                moonletData.WorldSize.y - moonletData.Top()));
            createOverworldCell(spaceBiome, bounds, tags);

            // Bottom cell
            tags = new TagSet();
            bounds = new Polygon(new Rect(0f, 0, moonletData.WorldSize.x, moonletData.Bottom()));
            createOverworldCell(spaceBiome, bounds, tags);

            // Left side cell
            tags = new TagSet();
            vertices = new List<Vector2>()
            {
                new Vector2I(0, moonletData.Bottom()),
                new Vector2I(0, moonletData.Top()),
                topLeftNe,
                topLeftSw,
                bottomLeftNw,
                bottomLeftSe,
            };
            bounds = new Polygon(vertices);
            createOverworldCell(spaceBiome, bounds, tags);

            // Right side cell
            tags = new TagSet();
            vertices = new List<Vector2>()
            {
                bottomRightSw,
                bottomRightNe,
                topRightSe,
                topRightNw,
                new Vector2I(moonletData.WorldSize.x, moonletData.Top()),
                new Vector2I(moonletData.WorldSize.x, moonletData.Bottom()),
            };
            bounds = new Polygon(vertices);
            createOverworldCell(spaceBiome, bounds, tags);
        }

        /// <summary>
        /// From WorldGen.RenderToMap
        /// </summary>
        /// <param name="cells"></param>
        /// <param name="bgTemp"></param>
        /// <param name="dc"></param>
        private static void ClearTerrain(Sim.Cell[] cells, float[] bgTemp, Sim.DiseaseCell[] dc)
        {
            var vacuum = ElementLoader.FindElementByHash(SimHashes.Vacuum);
            for (var i = 0; i < cells.Length; ++i)
            {
                cells[i].SetValues(vacuum, ElementLoader.elements);
                bgTemp[i] = -1f;
                dc[i] = new Sim.DiseaseCell()
                {
                    diseaseIdx = byte.MaxValue
                };
            }
        }

        /// <summary>
        /// Rewrite of WorldGen.ProcessByTerrainCell
        /// </summary>
        /// <param name="moonletData"></param>
        /// <param name="data"></param>
        /// <param name="cells"></param>
        /// <param name="dc"></param>
        /// <param name="noiseMap"></param>
        /// <param name="coreCells"></param>
        /// <returns></returns>
        private static ISet<Vector2I> DrawCustomTerrain(MoonletData moonletData, Data data, Sim.Cell[] cells,
            Sim.DiseaseCell[] dc, float[,] noiseMap, out ISet<Vector2I> coreCells)
        {
            var biomeCells = new HashSet<Vector2I>();
            var sideCells = new HashSet<Vector2I>();
            var claimedCells = new HashSet<int>();
            coreCells = new HashSet<Vector2I>();
            foreach (var cell in data.terrainCells)
            {
                cell.InitializeCells(claimedCells);
            }

            if (MiniBaseOptions.Instance.SkipLiveableArea)
            {
                return biomeCells;
            }

            // Using a smooth noisemap, map the noise values to elements via the element band profile
            var setTerrain = new Action<MiniBaseBiomeProfile, ISet<Vector2I>>((biome, positions) =>
            {
                var diseaseDb = Db.Get().Diseases;
                var diseaseDict = new Dictionary<DiseaseID, byte>()
                {
                    { DiseaseID.None, byte.MaxValue },
                    { DiseaseID.Slimelung, diseaseDb.GetIndex(diseaseDb.SlimeGerms.id) },
                    { DiseaseID.FoodPoisoning, diseaseDb.GetIndex(diseaseDb.FoodGerms.id) },
                };

                foreach (var pos in positions)
                {
                    var e = noiseMap[pos.x, pos.y];
                    var bandInfo = biome.GetBand(e);
                    var element = bandInfo.GetElement();
                    var elementData = biome.GetPhysicsData(bandInfo);
                    var cell = Grid.PosToCell(pos);
                    cells[cell].SetValues(element, elementData, ElementLoader.elements);
                    dc[cell] = new Sim.DiseaseCell()
                    {
                        diseaseIdx = Db.Get().Diseases.GetIndex(diseaseDict[bandInfo.Disease]),
                        elementCount = _random.Next(10000, 1000000),
                    };
                }
            });

            // Main biome
            var relativeLeft = moonletData.Left();
            var relativeRight = moonletData.Right();
            for (var x = relativeLeft; x < relativeRight; x++)
            {
                for (var y = moonletData.Bottom(); y < moonletData.Top(); y++)
                {
                    var pos = new Vector2I(x, y);
                    var extra = (int)(noiseMap[pos.x, pos.y] * 8f);

                    if (moonletData.Type != Moonlet.Start &&
                        (moonletData.Type != Moonlet.Second || !MiniBaseOptions.Instance.TeleportersEnabled) &&
                        y > moonletData.Bottom() + extra + (2 * moonletData.Height() / 3))
                    {
                        continue;
                    }

                    if (moonletData.InLiveableArea(pos))
                    {
                        biomeCells.Add(pos);
                    }
                    else
                    {
                        sideCells.Add(pos);
                    }
                }
            }

            setTerrain(moonletData.Biome, biomeCells);
            setTerrain(moonletData.Biome, sideCells);

            if (!moonletData.HasCore)
            {
                return biomeCells;
            }

            // Core area
            var coreHeight = MiniBaseOptions.CoreMin + moonletData.Height() / 10;
            var heights = GetHorizontalWalk(moonletData.WorldSize.x, coreHeight,
                coreHeight + MiniBaseOptions.CoreDeviation);
            ISet<Vector2I> abyssaliteCells = new HashSet<Vector2I>();
            for (var x = relativeLeft; x < relativeRight; x++)
            {
                // Create abyssalite border of size CORE_BORDER
                for (var j = 0; j < MiniBaseOptions.CoreBorder; j++)
                {
                    abyssaliteCells.Add(new Vector2I(x, moonletData.Bottom() + heights[x] + j));
                }

                // Ensure border thickness at high slopes
                if (x > relativeLeft && x < relativeRight - 1)
                {
                    if ((heights[x - 1] - heights[x] > 1) || (heights[x + 1] - heights[x] > 1))
                    {
                        var top = new Vector2I(x, moonletData.Bottom() + heights[x] + MiniBaseOptions.CoreBorder - 1);
                        abyssaliteCells.Add(top + new Vector2I(-1, 0));
                        abyssaliteCells.Add(top + new Vector2I(1, 0));
                        abyssaliteCells.Add(top + new Vector2I(0, 1));
                    }
                }

                // Mark core biome cells
                for (var y = moonletData.Bottom(); y < moonletData.Bottom() + heights[x]; y++)
                {
                    coreCells.Add(new Vector2I(x, y));
                }
            }

            coreCells.ExceptWith(abyssaliteCells);
            setTerrain(moonletData.CoreBiome, coreCells);

            foreach (var abyssaliteCell in abyssaliteCells)
            {
                cells[Grid.PosToCell(abyssaliteCell)].SetValues(WorldGen.katairiteElement, ElementLoader.elements);
            }

            biomeCells.ExceptWith(coreCells);
            biomeCells.ExceptWith(abyssaliteCells);
            return biomeCells;
        }

        /// <summary>
        /// Rewrite of WorldGen.DrawWorldBorder
        /// </summary>
        /// <param name="moonletData"></param>
        /// <param name="cells"></param>
        /// <returns>Set of border cells.</returns>
        private static ISet<Vector2I> DrawCustomWorldBorders(MoonletData moonletData, Sim.Cell[] cells)
        {
            var options = MiniBaseOptions.Instance;

            var skipCorners =
                DlcManager.IsExpansion1Active() &&
                options.OilMoonlet &&
                options.TeleportersEnabled &&
                options.TeleporterPlacement == WarpPlacementType.Corners;

            var borderCells = new HashSet<Vector2I>();

            var borderMat = WorldGen.unobtaniumElement;

            var addBorderCell = new Action<int, int, Element>((x, y, e) =>
            {
                var cell = Grid.XYToCell(x, y);
                if (Grid.IsValidCell(cell))
                {
                    borderCells.Add(new Vector2I(x, y));
                    cells[cell].SetValues(e, ElementLoader.elements);
                }
            });

            // Top and bottom borders
            for (var x = 0; x < moonletData.WorldSize.x; x++)
            {
                // Top border
                for (var y = moonletData.Top(); y < moonletData.Top(true); y++)
                {
                    if (moonletData.Type == Moonlet.Start || x < (moonletData.Left() + MiniBaseOptions.CornerSize) ||
                        x > (moonletData.Right() - MiniBaseOptions.CornerSize))
                    {
                        addBorderCell(x, y, borderMat);
                    }
                }

                // Bottom border
                for (var y = moonletData.Bottom(true); y < moonletData.Bottom(); y++)
                {
                    addBorderCell(x, y, borderMat);
                }
            }

            // Side borders
            for (var y = moonletData.Bottom(true); y < moonletData.Top(true); y++)
            {
                // Left border
                for (var x = moonletData.Left(true); x < moonletData.Left(); x++)
                {
                    addBorderCell(x, y, borderMat);
                }

                // Right border
                for (var x = moonletData.Right(); x < moonletData.Right(true); x++)
                {
                    addBorderCell(x, y, borderMat);
                }
            }

            // Corner structures
            if (!skipCorners)
            {
                var leftCenterX = (moonletData.Left(true) + moonletData.Left()) / 2;
                var rightCenterX = (moonletData.Right() + moonletData.Right(true)) / 2;
                var adjustedCornerSize =
                    MiniBaseOptions.CornerSize + (int)Math.Ceiling(MiniBaseOptions.BorderSize / 2f);
                for (var i = 0; i < adjustedCornerSize; i++)
                {
                    for (var j = adjustedCornerSize; j > i; j--)
                    {
                        var bottomY = moonletData.Bottom() + adjustedCornerSize - j;
                        var topY = moonletData.Top() - adjustedCornerSize + j - 1;

                        borderMat = j - i <= MiniBaseOptions.DiagonalBorderSize
                            ? WorldGen.unobtaniumElement
                            : ElementLoader.FindElementByHash(SimHashes.Glass);

                        addBorderCell(leftCenterX + i, bottomY, borderMat);
                        addBorderCell(leftCenterX - i, bottomY, borderMat);
                        addBorderCell(rightCenterX + i, bottomY, borderMat);
                        addBorderCell(rightCenterX - i, bottomY, borderMat);

                        addBorderCell(leftCenterX + i, topY, borderMat);
                        addBorderCell(leftCenterX - i, topY, borderMat);
                        addBorderCell(rightCenterX + i, topY, borderMat);
                        addBorderCell(rightCenterX - i, topY, borderMat);
                    }
                }
            }

            // Space access
            Action<int, Element> generateAccess;
            switch (options.SpaceAccess)
            {
                case AccessType.Full:
                    generateAccess = (y, mat) =>
                    {
                        for (var x = moonletData.Left() + MiniBaseOptions.CornerSize;
                             x < moonletData.Right() - MiniBaseOptions.CornerSize;
                             x++)
                        {
                            addBorderCell(x, y, mat);
                        }
                    };
                    break;
                case AccessType.Classic:
                    generateAccess = (y, mat) =>
                    {
                        // Left cutout
                        for (var x = moonletData.Left() + MiniBaseOptions.CornerSize;
                             x < Math.Min(
                                 moonletData.Left() + MiniBaseOptions.CornerSize + MiniBaseOptions.SpaceAccessSize,
                                 moonletData.Right() - MiniBaseOptions.CornerSize);
                             x++)
                        {
                            addBorderCell(x, y, mat);
                        }

                        // Right cutout
                        for (var x = Math.Max(
                                 moonletData.Right() - MiniBaseOptions.CornerSize - MiniBaseOptions.SpaceAccessSize,
                                 moonletData.Left() + MiniBaseOptions.CornerSize);
                             x < moonletData.Right() - MiniBaseOptions.CornerSize;
                             x++)
                        {
                            addBorderCell(x, y, mat);
                        }
                    };
                    break;
                default:
                    generateAccess = (y, mat) => { };
                    break;
            }

            borderMat = WorldGen.katairiteElement;
            if (moonletData.Type == Moonlet.Start)
            {
                for (var y = moonletData.Top(); y < moonletData.Top(true); y++)
                {
                    generateAccess(y, borderMat);
                    
                    // generate access to side tunnels if desired
                    // Far left cutout
                    if ((options.TunnelAccess & TunnelAccessType.Left) > 0)
                    {
                        for (var x = MiniBaseOptions.BorderSize;
                             x < Math.Min(
                                 MiniBaseOptions.BorderSize + MiniBaseOptions.SpaceAccessSize,
                                 moonletData.WorldSize.x - MiniBaseOptions.BorderSize);
                             x++)
                        {
                            addBorderCell(x, y, borderMat);
                        }
                    }

                    // Far right cutout
                    if ((options.TunnelAccess & TunnelAccessType.Right) > 0)
                    {
                        for (var x = Math.Max(
                                 moonletData.WorldSize.x - MiniBaseOptions.BorderSize - MiniBaseOptions.SpaceAccessSize,
                                 MiniBaseOptions.BorderSize);
                             x < moonletData.WorldSize.x - MiniBaseOptions.BorderSize;
                             x++)
                        {
                            addBorderCell(x, y, borderMat);
                        }
                    }
                }
            }

            return borderCells;
        }

        /// <summary>
        /// Create and place a feature (geysers, volcanoes, vents, etc) given a feature type
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cells"></param>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="coverMaterial"></param>
        /// <param name="neutronium"></param>
        /// <param name="cover"></param>
        private static void PlaceGeyser(Data data, Sim.Cell[] cells, FeatureType type, Vector2I pos,
            Element coverMaterial, bool neutronium = true, bool cover = true)
        {
            string featureName;
            switch (type)
            {
                case FeatureType.None:
                    featureName = null;
                    break;
                case FeatureType.RandomAny:
                    var featureType = MiniBaseOptions.GeyserDictionary.Keys
                        .Where(x => x != FeatureType.Niobium)
                        .GetRandom();
                    featureName = MiniBaseOptions.GeyserDictionary[featureType];
                    break;
                case FeatureType.RandomWater:
                    featureName = MiniBaseOptions.GeyserDictionary[MiniBaseOptions.RandomWaterFeatures.GetRandom()];
                    break;
                case FeatureType.RandomUseful:
                    featureName = MiniBaseOptions.GeyserDictionary[MiniBaseOptions.RandomUsefulFeatures.GetRandom()];
                    break;
                case FeatureType.RandomVolcano:
                    featureName = MiniBaseOptions.GeyserDictionary[MiniBaseOptions.RandomVolcanoFeatures.GetRandom()];
                    break;
                case FeatureType.RandomMagmaVolcano:
                    featureName =
                        MiniBaseOptions.GeyserDictionary[MiniBaseOptions.RandomMagmaVolcanoFeatures.GetRandom()];
                    break;
                case FeatureType.RandomMetalVolcano:
                    featureName =
                        MiniBaseOptions.GeyserDictionary[MiniBaseOptions.RandomMetalVolcanoFeatures.GetRandom()];
                    break;
                default:
                    if (!MiniBaseOptions.GeyserDictionary.TryGetValue(type, out featureName))
                    {
                        return;
                    }

                    break;
            }

            if (featureName == null)
            {
                return;
            }

            var feature = new Prefab(featureName, Prefab.Type.Other, pos.x, pos.y, SimHashes.Katairite);
            data.gameSpawnData.otherEntities.Add(feature);

            // Base
            if (neutronium)
            {
                for (var x = pos.x - 1; x < pos.x + 3; x++)
                {
                    cells[Grid.XYToCell(x, pos.y - 1)].SetValues(WorldGen.unobtaniumElement, ElementLoader.elements);
                }
            }

            // Cover feature
            if (cover)
            {
                for (var x = pos.x; x < pos.x + 2; x++)
                {
                    for (var y = pos.y; y < pos.y + 3; y++)
                    {
                        if (!ElementLoader.elements[cells[Grid.XYToCell(x, y)].elementIdx].IsSolid)
                        {
                            cells[Grid.XYToCell(x, y)].SetValues(coverMaterial, GetPhysicsData(coverMaterial),
                                ElementLoader.elements);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Places teleporters and warp conduits if enabled by the player.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cells"></param>
        /// <param name="moonletData"></param>
        /// <returns>Set of reserved positions by the teleporters.</returns>
        private static ISet<Vector2I> PlaceWarpPortals(Data data, Sim.Cell[] cells, MoonletData moonletData)
        {
            var reserved = new HashSet<Vector2I>();

            // don't spawn the teleporter if the destination is not being used
            // or teleporters have been turned off
            if (!MiniBaseOptions.Instance.OilMoonlet ||
                CustomGameSettings.Instance.GetCurrentQualitySetting(CustomGameSettingConfigs.Teleporters).id !=
                "Enabled")
            {
                return reserved;
            }

            var inCorners = MiniBaseOptions.Instance.TeleporterPlacement == WarpPlacementType.Corners;

            var vacuum = ElementLoader.FindElementByHash(SimHashes.Vacuum);

            var clearCells = new Action<int, int, int, int>((x0, x1, y0, y1) =>
            {
                for (var x = x0; x < x1; x++)
                {
                    for (var y = y0; y < y1; y++)
                    {
                        cells[Grid.XYToCell(x, y)].SetValues(vacuum, ElementLoader.elements);
                        reserved.Add(new Vector2I(x, y));
                    }

                    data.gameSpawnData.buildings.Add(new Prefab("TilePOI", Prefab.Type.Building, x, y0 - 1,
                        SimHashes.SandStone));
                    reserved.Add(new Vector2I(x, y0 - 1));
                }
            });

            // teleporter positions
            var tpSenderPos = new Vector2I(
                inCorners ? moonletData.Left() + MiniBaseOptions.BorderSize + 2 : moonletData.Width() / 2 + 1,
                inCorners ? moonletData.Top() - MiniBaseOptions.BorderSize : moonletData.Height() / 2 + 1);
            var tpReceiverPos = new Vector2I(
                inCorners ? moonletData.Right() - MiniBaseOptions.BorderSize - 3 : moonletData.Width() / 2 + 4,
                inCorners ? moonletData.Top() - MiniBaseOptions.BorderSize : moonletData.Height() / 2 + 1);
            var wcSenderPos = new Vector2I(
                inCorners ? moonletData.Left() + MiniBaseOptions.BorderSize - 2 : moonletData.Width() / 2 - 3,
                inCorners ? moonletData.Top() - MiniBaseOptions.BorderSize : moonletData.Height() / 2 + 1);
            var wcReceiverPos = new Vector2I(
                inCorners ? moonletData.Right() - MiniBaseOptions.BorderSize : moonletData.Width() / 2 + 7,
                inCorners ? moonletData.Top() - MiniBaseOptions.BorderSize : moonletData.Height() / 2 + 1);

            Prefab portal;
            switch (moonletData.Type)
            {
                case Moonlet.Start:
                    // dupe teleporter sender
                    portal = new Prefab("WarpPortal", Prefab.Type.Other, tpSenderPos.x, tpSenderPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 2, portal.location_y, portal.location_y + 3);
                    // dupe teleporter receiver
                    portal = new Prefab("WarpReceiver", Prefab.Type.Other, tpReceiverPos.x, tpReceiverPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 2, portal.location_y, portal.location_y + 3);
                    // conduit sender
                    portal = new Prefab("WarpConduitSender", Prefab.Type.Other, wcSenderPos.x, wcSenderPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 3, portal.location_y, portal.location_y + 3);
                    // conduit receiver
                    portal = new Prefab("WarpConduitReceiver", Prefab.Type.Other, wcReceiverPos.x, wcReceiverPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 3, portal.location_y, portal.location_y + 3);
                    break;
                case Moonlet.Second:
                    // dupe teleporter sender
                    portal = new Prefab("WarpPortal", Prefab.Type.Other, tpSenderPos.x, tpSenderPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 2, portal.location_y, portal.location_y + 3);
                    // dupe teleporter receiver
                    portal = new Prefab("WarpReceiver", Prefab.Type.Other, tpReceiverPos.x, tpReceiverPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 2, portal.location_y, portal.location_y + 3);
                    // conduit sender
                    portal = new Prefab("WarpConduitSender", Prefab.Type.Other, wcSenderPos.x, wcSenderPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 3, portal.location_y, portal.location_y + 3);
                    // conduit receiver
                    portal = new Prefab("WarpConduitReceiver", Prefab.Type.Other, wcReceiverPos.x, wcReceiverPos.y,
                        SimHashes.Katairite);
                    data.gameSpawnData.otherEntities.Add(portal);
                    clearCells(portal.location_x - 1, portal.location_x + 3, portal.location_y, portal.location_y + 3);
                    break;
            }

            return reserved;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cells"></param>
        /// <param name="biomeCells"></param>
        /// <returns></returns>
        private static SpawnPoints GetSpawnPoints(Sim.Cell[] cells, IEnumerable<Vector2I> biomeCells)
        {
            var spawnPoints = new SpawnPoints()
            {
                OnFloor = new HashSet<Vector2I>(),
                OnCeil = new HashSet<Vector2I>(),
                InGround = new HashSet<Vector2I>(),
                InAir = new HashSet<Vector2I>(),
                InLiquid = new HashSet<Vector2I>(),
            };

            foreach (var pos in biomeCells)
            {
                var cell = Grid.PosToCell(pos);
                var element = ElementLoader.elements[cells[cell].elementIdx];
                if (element.IsSolid && element.id != SimHashes.Katairite && element.id != SimHashes.Unobtanium &&
                    cells[cell].temperature < 373f)
                {
                    spawnPoints.InGround.Add(pos);
                }
                else if (element.IsGas)
                {
                    var elementBelow = ElementLoader.elements[cells[Grid.CellBelow(cell)].elementIdx];
                    if (elementBelow.IsSolid)
                    {
                        spawnPoints.OnFloor.Add(pos);
                    }
                    else
                    {
                        spawnPoints.InAir.Add(pos);
                    }
                }
                else if (element.IsLiquid)
                {
                    spawnPoints.InLiquid.Add(pos);
                }
            }

            return spawnPoints;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cells"></param>
        /// <param name="spawnList"></param>
        /// <param name="biome"></param>
        /// <param name="biomeCells"></param>
        /// <param name="reservedCells"></param>
        private static void PlaceSpawnables(Sim.Cell[] cells, List<Prefab> spawnList, MiniBaseBiomeProfile biome,
            ISet<Vector2I> biomeCells, ISet<Vector2I> reservedCells)
        {
            var spawnStruct = GetSpawnPoints(cells, biomeCells.Except(reservedCells));
            PlaceSpawnables(spawnList, biome.SpawnablesOnFloor, spawnStruct.OnFloor, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesOnCeil, spawnStruct.OnCeil, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesInGround, spawnStruct.InGround, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesInLiquid, spawnStruct.InLiquid, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesInAir, spawnStruct.InAir, Prefab.Type.Pickupable);
        }

        /// <summary>
        /// Add spawnables to the GameSpawnData list
        /// </summary>
        /// <param name="spawnList"></param>
        /// <param name="spawnables"></param>
        /// <param name="spawnPoints"></param>
        /// <param name="prefabType"></param>
        private static void PlaceSpawnables(List<Prefab> spawnList, Dictionary<string, float> spawnables,
            ISet<Vector2I> spawnPoints, Prefab.Type prefabType)
        {
            if (spawnables == null || spawnables.Count == 0 || spawnPoints.Count == 0)
            {
                return;
            }

            foreach (var spawnable in spawnables)
            {
                var numSpawnables = (int)Math.Ceiling(spawnable.Value * spawnPoints.Count);
                for (var i = 0; i < numSpawnables && spawnPoints.Count > 0; i++)
                {
                    var pos = spawnPoints.ElementAt(_random.Next(0, spawnPoints.Count));
                    spawnPoints.Remove(pos);
                    spawnList.Add(new Prefab(spawnable.Key, prefabType, pos.x, pos.y, 0));
                }
            }
        }

        #region Util

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="modifier"></param>
        /// <param name="temperature"></param>
        /// <returns></returns>
        public static Sim.PhysicsData GetPhysicsData(Element element, float modifier = 1f, float temperature = -1f)
        {
            var defaultData = element.defaultValues;
            return new Sim.PhysicsData()
            {
                mass = defaultData.mass * modifier *
                       (element.IsSolid ? MiniBaseOptions.Instance.GetResourceModifier() : 1f),
                temperature = temperature == -1f ? defaultData.temperature : temperature,
                pressure = defaultData.pressure
            };
        }

        /// <summary>
        /// Returns a coherent noisemap normalized between [0.0, 1.0]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        /// <remarks>currently, the range is actually [yCorrection, 1.0] roughly centered around 0.5</remarks>
        private static float[,] GenerateNoiseMap(System.Random r, int width, int height)
        {
            var oct1 = new Octave(1f, 10f);
            var oct2 = new Octave(oct1.Amp / 2, oct1.Freq * 2);
            var oct3 = new Octave(oct2.Amp / 2, oct2.Freq * 2);
            var maxAmp = oct1.Amp + oct2.Amp + oct3.Amp;
            var absolutePeriod = 100f;
            var xStretch = 2.5f;
            var zStretch = 1.6f;
            var offset = new Vector2f((float)r.NextDouble(), (float)r.NextDouble());
            var noiseMap = new float[width, height];

            var total = 0f;
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var pos = new Vector2f(i / absolutePeriod + offset.x,
                        j / absolutePeriod + offset.y); // Find current x,y position for the noise function
                    double e = // Generate a value in [0, maxAmp] with average maxAmp / 2
                        oct1.Amp * Mathf.PerlinNoise(oct1.Freq * pos.x / xStretch, oct1.Freq * pos.y) +
                        oct2.Amp * Mathf.PerlinNoise(oct2.Freq * pos.x / xStretch, oct2.Freq * pos.y) +
                        oct3.Amp * Mathf.PerlinNoise(oct3.Freq * pos.x / xStretch, oct3.Freq * pos.y);

                    e = e / maxAmp; // Normalize to [0, 1]
                    var f = Mathf.Clamp((float)e, 0f, 1f);
                    total += f;

                    noiseMap[i, j] = f;
                }
            }

            // Center the distribution at 0.5 and stretch it to fill out [0, 1]
            var average = total / noiseMap.Length;
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var f = noiseMap[i, j];
                    f -= average;
                    f *= zStretch;
                    f += 0.5f;
                    noiseMap[i, j] = Mathf.Clamp(f, 0f, 1f);
                }
            }

            return noiseMap;
        }

        /// <summary>
        /// Return a description of a vertical movement over a horizontal axis
        /// </summary>
        /// <param name="width"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static int[] GetHorizontalWalk(int width, int min, int max)
        {
            const double walkChance = 0.7;
            const double doubleWalkChance = 0.25;
            var walk = new int[width];
            var height = _random.Next(min, max + 1);
            for (var i = 0; i < walk.Length; i++)
            {
                if (_random.NextDouble() < walkChance)
                {
                    int direction;
                    if (height >= max)
                    {
                        direction = -1;
                    }
                    else if (height <= min)
                    {
                        direction = 1;
                    }
                    else
                    {
                        direction = _random.NextDouble() < 0.5 ? 1 : -1;
                    }

                    if (_random.NextDouble() < doubleWalkChance)
                    {
                        direction *= 2;
                    }

                    height = Mathf.Clamp(height + direction, min, max);
                }

                walk[i] = height;
            }

            return walk;
        }

        #endregion

        #region Structs

        private struct Octave
        {
            public readonly float Amp;
            public readonly float Freq;

            public Octave(float amplitude, float frequency)
            {
                Amp = amplitude;
                Freq = frequency;
            }
        }

        public struct SpawnPoints
        {
            public ISet<Vector2I> OnFloor;
            public ISet<Vector2I> OnCeil;
            public ISet<Vector2I> InGround;
            public ISet<Vector2I> InAir;
            public ISet<Vector2I> InLiquid;
        }

        #endregion

        #region Fields

        private static System.Random _random;

        #endregion
    }
}