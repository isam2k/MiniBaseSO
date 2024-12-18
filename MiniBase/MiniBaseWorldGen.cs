using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Delaunay.Geo;
using HarmonyLib;
using Klei;
using MiniBase.Model;
using MiniBase.Model.Profiles;
using ProcGen;
using ProcGenGame;
using TemplateClasses;
using UnityEngine;
using VoronoiTree;
using static MiniBase.MiniBaseConfig;

namespace MiniBase
{
    public class MiniBaseWorldGen
    {
        public static void CreateWorld(WorldGen gen, BinaryWriter writer, ref Sim.Cell[] cells,
            ref Sim.DiseaseCell[] dc, int baseId, ref List<WorldTrait> placedStoryTraits, bool isStartingWorld)
        {
            var moonletData = new MoonletData(gen);
            if (DlcManager.IsExpansion1Active())
            {
                CreateSOWorld(gen, writer, ref cells, ref dc, baseId, moonletData);
            }
            else
            {
                CreateVanillaWorld(gen, writer, ref cells, ref dc, baseId, moonletData);
            }
        }
        
        private static void CreateVanillaWorld(WorldGen gen, BinaryWriter writer, ref Sim.Cell[] cells,
            ref Sim.DiseaseCell[] dc, int baseId, MoonletData moonletData)
        {
            var options = MiniBaseOptions.Instance;

            // Convenience variables, including private fields/properties
            var worldGen = Traverse.Create(gen);
            var data = gen.data;
            var myRandom = worldGen.Field("myRandom").GetValue<SeededRandom>();
            var updateProgressFn = worldGen.Field("successCallbackFn").GetValue<WorldGen.OfflineCallbackFunction>();
            var errorCallback = worldGen.Field("errorCallback").GetValue<Action<OfflineWorldGen.ErrorInfo>>();
            var running = worldGen.Field("running");
            running.SetValue(true);
            _random = new System.Random(data.globalTerrainSeed);

            // Initialize noise maps
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0f, WorldGenProgressStages.Stages.NoiseMapBuilder);
            var noiseMap = GenerateNoiseMap(_random, moonletData.WorldSize.x, moonletData.WorldSize.y);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0.9f, WorldGenProgressStages.Stages.NoiseMapBuilder);

            // Set biomes
            SetBiomes(data.overworldCells, moonletData);

            // Rewrite of WorldGen.RenderToMap function, which calls the default terrain and border generation, places features, and spawns flora and fauna
            cells = new Sim.Cell[Grid.CellCount];
            var bgTemp = new float[Grid.CellCount];
            dc = new Sim.DiseaseCell[Grid.CellCount];

            // Initialize terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 0f, WorldGenProgressStages.Stages.ClearingLevel);
            ClearTerrain(cells, bgTemp, dc);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 1f, WorldGenProgressStages.Stages.ClearingLevel);

            // Draw custom terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0f, WorldGenProgressStages.Stages.Processing);
            ISet<Vector2I> biomeCells, coreCells, borderCells;
            biomeCells = DrawCustomTerrain(moonletData, data, cells, bgTemp, dc, noiseMap, out coreCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0.9f, WorldGenProgressStages.Stages.Processing);

            // Printing pod
            var startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1, moonletData.Bottom() + (moonletData.Height() / 2) + 2);
            data.gameSpawnData.baseStartPos = startPos;
            var templateSpawnTargets = new List<TemplateSpawning.TemplateSpawner>();
            var reservedCells = new HashSet<Vector2I>();
            var startingBaseTemplate = TemplateCache.GetTemplate(gen.Settings.world.startingBaseTemplate);

            startingBaseTemplate.pickupables.Clear(); // Remove stray hatch
            var itemPos = new Vector2I(3, 1);
            foreach (var entry in moonletData.Biome.StartingItems) // Add custom defined starting items
            {
                startingBaseTemplate.pickupables.Add(new Prefab(entry.Key, Prefab.Type.Pickupable, itemPos.x, itemPos.y, (SimHashes) 0, _units: entry.Value));
            }
            foreach (var cell in startingBaseTemplate.cells)
            {
                if (cell.element == SimHashes.SandStone || cell.element == SimHashes.Algae)
                {
                    cell.element = moonletData.Biome.DefaultMaterial;
                }
                reservedCells.Add(new Vector2I(cell.location_x, cell.location_y) + startPos);
            }
            var terrainCell = data.overworldCells.Find((Predicate<TerrainCell>)(tc => tc.node.tags.Contains(WorldGenTags.StartLocation)));
            startingBaseTemplate.cells.RemoveAll((c) => (c.location_x == -8) || (c.location_x == 9)); // Trim the starting base area
            templateSpawnTargets.Add(new TemplateSpawning.TemplateSpawner(startPos,startingBaseTemplate.GetTemplateBounds(), startingBaseTemplate, terrainCell));

            // Geysers
            int GeyserMinX = moonletData.Left() + CornerSize + 2;
            int GeyserMaxX = moonletData.Right() - CornerSize - 4;
            int GeyserMinY = moonletData.Bottom() + CornerSize + 2;
            int GeyserMaxY = moonletData.Top() - CornerSize - 4;
            var coverElement = moonletData.Biome.DefaultElement();
            PlaceGeyser(data, cells, options.FeatureWest, new Vector2I(moonletData.Left() + 2, _random.Next(GeyserMinY, GeyserMaxY + 1)), coverElement);
            PlaceGeyser(data, cells, options.FeatureEast, new Vector2I(moonletData.Right() - 4, _random.Next(GeyserMinY, GeyserMaxY + 1)), coverElement);
            if (options.HasCore())
            {
                coverElement = moonletData.CoreBiome.DefaultElement();
            }
            PlaceGeyser(data, cells, options.FeatureSouth, new Vector2I(_random.Next(GeyserMinX, GeyserMaxX + 1), moonletData.Bottom()), coverElement);

            // Change geysers to be made of abyssalite so they don't melt in magma
            var geyserPrefabs = Assets.GetPrefabsWithComponent<Geyser>();
            geyserPrefabs.Add(Assets.GetPrefab(GameTags.OilWell));
            foreach (var prefab in geyserPrefabs)
            {
                prefab.GetComponent<PrimaryElement>().SetElement(SimHashes.Katairite);
            }

            // Draw borders
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 0f, WorldGenProgressStages.Stages.DrawWorldBorder);
            borderCells = DrawCustomWorldBorders(moonletData, cells);
            biomeCells.ExceptWith(borderCells);
            coreCells.ExceptWith(borderCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 1f, WorldGenProgressStages.Stages.DrawWorldBorder);

            // Settle simulation
            // This writes the cells to the world, then performs a couple of game frames of simulation, then saves the game
            running.SetValue(WorldGenSimUtil.DoSettleSim(gen.Settings, writer, ref cells, ref bgTemp, ref dc, updateProgressFn, data, templateSpawnTargets, errorCallback, baseId));

            // Place templates, pretty much just the printing pod
            var claimedCells = new Dictionary<int, int>();
            foreach (var templateSpawner in templateSpawnTargets)
            {
                data.gameSpawnData.AddTemplate(templateSpawner.container, templateSpawner.position, ref claimedCells);
            }

            // Add plants, critters, and items
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0f, WorldGenProgressStages.Stages.PlacingCreatures);
            PlaceSpawnables(cells, data.gameSpawnData.pickupables, moonletData.Biome, biomeCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 50f, WorldGenProgressStages.Stages.PlacingCreatures);
            PlaceSpawnables(cells, data.gameSpawnData.pickupables, moonletData.CoreBiome, coreCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 100f, WorldGenProgressStages.Stages.PlacingCreatures);

            // Finish and save
            updateProgressFn(global::STRINGS.UI.WORLDGEN.COMPLETE.key, 1f, WorldGenProgressStages.Stages.Complete);
            running.SetValue(false);
        }
        
        private static void CreateSOWorld(WorldGen gen, BinaryWriter writer, ref Sim.Cell[] cells,
            ref Sim.DiseaseCell[] dc, int baseId, MoonletData moonletData)
        {
            var options = MiniBaseOptions.Instance;
            
            // Convenience variables, including private fields/properties
            var worldGen = Traverse.Create(gen);
            var myRandom = worldGen.Field("myRandom").GetValue<SeededRandom>();
            var updateProgressFn = worldGen.Field("successCallbackFn").GetValue<WorldGen.OfflineCallbackFunction>();
            var errorCallback = worldGen.Field("errorCallback").GetValue<Action<OfflineWorldGen.ErrorInfo>>();
            var running = worldGen.Field("running");
            running.SetValue(true);
            _random = new System.Random(gen.data.globalTerrainSeed);

            // Initialize noise maps
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0f, WorldGenProgressStages.Stages.NoiseMapBuilder);
            var noiseMap = GenerateNoiseMap(_random, moonletData.WorldSize.x, moonletData.WorldSize.y);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0.9f, WorldGenProgressStages.Stages.NoiseMapBuilder);

            // Set biomes
            SetBiomes(gen.data.overworldCells, moonletData);

            // Rewrite of WorldGen.RenderToMap function, which calls the default terrain and border generation, places features, and spawns flora and fauna
            cells = new Sim.Cell[Grid.CellCount];
            var bgTemp = new float[Grid.CellCount];
            dc = new Sim.DiseaseCell[Grid.CellCount];

            // Initialize terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 0f, WorldGenProgressStages.Stages.ClearingLevel);
            ClearTerrain(cells, bgTemp, dc);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.CLEARINGLEVEL.key, 1f, WorldGenProgressStages.Stages.ClearingLevel);

            // Draw custom terrain
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0f, WorldGenProgressStages.Stages.Processing);
            var biomeCells = DrawCustomTerrain(moonletData, gen.data, cells, bgTemp, dc, noiseMap, out var coreCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0.9f, WorldGenProgressStages.Stages.Processing);
            
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
                case MoonletData.Moonlet.Start:
                    // Printing pod
                    startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1, moonletData.Bottom() + (moonletData.Height() / 2) + 2);
                    gen.data.gameSpawnData.baseStartPos = startPos;
                    var startingBaseTemplate = TemplateCache.GetTemplate(gen.Settings.world.startingBaseTemplate);
                    startingBaseTemplate.pickupables.Clear(); // Remove stray hatch
                    var itemPos = new Vector2I(3, 1);
                    foreach (var entry in moonletData.Biome.StartingItems) // Add custom defined starting items
                    {
                        startingBaseTemplate.pickupables.Add(new Prefab(entry.Key, Prefab.Type.Pickupable, itemPos.x, itemPos.y, (SimHashes)0, _units: entry.Value));
                    }
                    foreach (var cell in startingBaseTemplate.cells)
                    {
                        if (cell.element == SimHashes.SandStone || cell.element == SimHashes.Algae)
                        {
                            cell.element = moonletData.Biome.DefaultMaterial;
                        }
                        reservedCells.Add(new Vector2I(cell.location_x, cell.location_y) + startPos);
                    }
                    terrainCell = gen.data.overworldCells.Find((Predicate<TerrainCell>)(tc => tc.node.tags.Contains(WorldGenTags.StartLocation)));
                    startingBaseTemplate.cells.RemoveAll((c) => (c.location_x == -8) || (c.location_x == 9)); // Trim the starting base area
                    templateSpawnTargets.Add(new TemplateSpawning.TemplateSpawner(startPos,startingBaseTemplate.GetTemplateBounds(), startingBaseTemplate, terrainCell));
                    
                    // warp portal if oil moonlet is also active
                    if (MiniBaseOptions.Instance.OilMoonlet)
                    {
                        var portal = new Prefab("WarpPortal", Prefab.Type.Other, startPos.x - 5, startPos.y, SimHashes.Katairite);
                        gen.data.gameSpawnData.otherEntities.Add(portal);
                    }
                    
                    // Geysers
                    geyserMinX = moonletData.Left() + CornerSize + 2;
                    geyserMaxX = moonletData.Right() - CornerSize - 4;
                    geyserMinY = moonletData.Bottom() + CornerSize + 2;
                    geyserMaxY = moonletData.Top() - CornerSize - 4;
                    coverElement = moonletData.Biome.DefaultElement();
                    PlaceGeyser(gen.data, cells, options.FeatureWest, new Vector2I(moonletData.Left() + 2, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
                    PlaceGeyser(gen.data, cells, options.FeatureEast, new Vector2I(moonletData.Right() - 4, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
                    if (moonletData.HasCore)
                    {
                        coverElement = moonletData.CoreBiome.DefaultElement();
                    }

                    var bottomGeyserPos = new Vector2I(_random.Next(geyserMinX, geyserMaxX + 1), moonletData.Bottom());
                    PlaceGeyser(gen.data, cells, options.FeatureSouth, bottomGeyserPos, coverElement);
                    
                    if(moonletData.HasCore && moonletData.CoreBiome == MiniBaseCoreBiomeProfiles.RadioactiveCoreProfile)
                    {
                        // Add a single viable beeta in the radioactive core
                        bottomGeyserPos.x += bottomGeyserPos.x > moonletData.Left() + moonletData.Width() / 2 ? -12 : 12;

                        bottomGeyserPos.y += 1;
                        coverElement = ElementLoader.FindElementByHash(SimHashes.Wolframite);

                        cells[Grid.XYToCell(bottomGeyserPos.x, bottomGeyserPos.y)].SetValues(coverElement, GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature), ElementLoader.elements);
                        cells[Grid.XYToCell(bottomGeyserPos.x+1, bottomGeyserPos.y)].SetValues(coverElement, GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature), ElementLoader.elements);

                        coverElement = ElementLoader.FindElementByHash(SimHashes.CarbonDioxide);

                        physicsData = GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature);
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 3; y++)
                            {
                                cells[Grid.XYToCell(bottomGeyserPos.x + x, bottomGeyserPos.y + 1 + y)].SetValues(coverElement, physicsData, ElementLoader.elements);
                            }
                        }

                        gen.data.gameSpawnData.pickupables.Add(new Prefab("BeeHive", Prefab.Type.Pickupable, bottomGeyserPos.x, bottomGeyserPos.y+1, (SimHashes)0));
                    }
                    break;
                case MoonletData.Moonlet.Second:
                    startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1, moonletData.Bottom() + (moonletData.Height() / 2) + 2);
                    
                    // spawn warp receiver
                    var receiver = new Prefab("WarpReceiver", Prefab.Type.Other, startPos.x - 5, startPos.y, SimHashes.Katairite);
                    gen.data.gameSpawnData.otherEntities.Add(receiver);
                    
                    geyserMinX = moonletData.Left() + CornerSize + 2;
                    geyserMaxX = moonletData.Right() - CornerSize - 4;
                    geyserMinY = moonletData.Bottom() + CornerSize + 2;
                    geyserMaxY = moonletData.Top() - moonletData.Height() / 2 - CornerSize - 4;

                    coverElement = moonletData.Biome.DefaultElement();
                    PlaceGeyser(gen.data, cells, MiniBaseOptions.FeatureType.OilReservoir, new Vector2I(moonletData.Left() + 2, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement, false, false);
                    PlaceGeyser(gen.data, cells, MiniBaseOptions.FeatureType.OilReservoir, new Vector2I(moonletData.Right() - 4, _random.Next(geyserMinY, geyserMaxY + 1)), coverElement, false, false);
                    if (moonletData.HasCore)
                    {
                        coverElement = moonletData.CoreBiome.DefaultElement();
                    }
                    PlaceGeyser(gen.data, cells, MiniBaseOptions.FeatureType.Volcano, new Vector2I(_random.Next(geyserMinX, geyserMaxX + 1), moonletData.Bottom()), coverElement);
                    break;
                case MoonletData.Moonlet.Tree:
                    startPos = new Vector2I(moonletData.Left() + (2*moonletData.Width() / 3) - 1, moonletData.Bottom() + (moonletData.Height() / 2) + 2);
                    var template = TemplateCache.GetTemplate("expansion1::poi/sap_tree_room");
                    terrainCell = gen.data.overworldCells.Find((Predicate<TerrainCell>)(tc => tc.node.tags.Contains(WorldGenTags.AtSurface)));
                    templateSpawnTargets.Add(new TemplateSpawning.TemplateSpawner(startPos, template.GetTemplateBounds(), template, terrainCell));
                    break;
                case MoonletData.Moonlet.Niobium:
                    startPos = new Vector2I(moonletData.Left() + (moonletData.Width() / 2) - 1, moonletData.Bottom() + (moonletData.Height() / 4) + 2);
                    coverElement = ElementLoader.FindElementByHash(SimHashes.Niobium);
                    PlaceGeyser(gen.data, cells, MiniBaseOptions.FeatureType.Niobium, startPos, coverElement);

                    // Replace niobium near the surface with obsidian
                    coverElement = ElementLoader.FindElementByHash(SimHashes.Obsidian);
                    physicsData = GetPhysicsData(coverElement, 1f, moonletData.Biome.DefaultTemperature);
                    for (int x = moonletData.Left(); x <= moonletData.Right(); x++)
                    {
                        for(int y = moonletData.Top(); y >= (moonletData.Top() - (3*moonletData.Height()/4)); y--)
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
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 0f, WorldGenProgressStages.Stages.DrawWorldBorder);
            var borderCells = DrawCustomWorldBorders(moonletData, cells);
            biomeCells.ExceptWith(borderCells);
            coreCells.ExceptWith(borderCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.DRAWWORLDBORDER.key, 1f, WorldGenProgressStages.Stages.DrawWorldBorder);

            // Settle simulation
            // This writes the cells to the world, then performs a couple of game frames of simulation, then saves the game
            running.SetValue(WorldGenSimUtil.DoSettleSim(gen.Settings, writer, ref cells, ref bgTemp, ref dc, updateProgressFn, gen.data, templateSpawnTargets, errorCallback, baseId));

            // Place templates, pretty much just the printing pod
            var claimedCells = new Dictionary<int, int>();
            foreach (var templateSpawner in templateSpawnTargets)
            {
                gen.data.gameSpawnData.AddTemplate(templateSpawner.container, templateSpawner.position, ref claimedCells);
            }

            // Add plants, critters, and items
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0f, WorldGenProgressStages.Stages.PlacingCreatures);
            PlaceSpawnables(cells, gen.data.gameSpawnData.pickupables, moonletData.Biome, biomeCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0.5f, WorldGenProgressStages.Stages.PlacingCreatures);
            if (moonletData.HasCore)
            {
                PlaceSpawnables(cells, gen.data.gameSpawnData.pickupables, moonletData.CoreBiome, coreCells, reservedCells);
            }
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 1f, WorldGenProgressStages.Stages.PlacingCreatures);

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
        private static void SetBiomes(List<TerrainCell> overworldCells, MoonletData moonletData)
        {
            overworldCells.Clear();
            
            const string spaceBiome = "subworlds/space/Space";
            string backgroundBiome = moonletData.Biome.BackgroundSubworld;

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
            Vector2I bottomLeftSe = moonletData.BottomLeft() + new Vector2I(CornerSize, 0),
                bottomLeftNw = moonletData.BottomLeft() + new Vector2I(0, CornerSize),
                topLeftSw = moonletData.TopLeft() - new Vector2I(0, CornerSize) - new Vector2I(0, 1),
                topLeftNe = moonletData.TopLeft() + new Vector2I(CornerSize, 0),
                topRightNw = moonletData.TopRight() - new Vector2I(CornerSize, 0) - new Vector2I(1, 0),
                topRightSe = moonletData.TopRight() - new Vector2I(0, CornerSize) - new Vector2I(0, 1),
                bottomRightNe = moonletData.BottomRight() + new Vector2I(0, CornerSize),
                bottomRightSw = moonletData.BottomRight() - new Vector2I(CornerSize, 0);

            // Liveable cell
            var tags = new TagSet();

            if (moonletData.Type == MoonletData.Moonlet.Start)
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
            bounds = new Polygon(new Rect(0f, moonletData.Top(), moonletData.WorldSize.x, moonletData.WorldSize.y - moonletData.Top()));
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
            for (int i = 0; i < cells.Length; ++i)
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
        /// <param name="bgTemp"></param>
        /// <param name="dc"></param>
        /// <param name="noiseMap"></param>
        /// <param name="coreCells"></param>
        /// <returns></returns>
        private static ISet<Vector2I> DrawCustomTerrain(MoonletData moonletData, Data data, Sim.Cell[] cells,
            float[] bgTemp, Sim.DiseaseCell[] dc, float[,] noiseMap, out ISet<Vector2I> coreCells)
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
                var diseaseDict = new Dictionary<MiniBaseConfig.DiseaseID, byte>()
                {
                    { DiseaseID.None, byte.MaxValue},
                    { DiseaseID.Slimelung, diseaseDb.GetIndex(diseaseDb.SlimeGerms.id)},
                    { DiseaseID.FoodPoisoning, diseaseDb.GetIndex(diseaseDb.FoodGerms.id)},
                };

                foreach (var pos in positions)
                {
                    float e = noiseMap[pos.x, pos.y];
                    var bandInfo = biome.GetBand(e);
                    var element = bandInfo.GetElement();
                    var elementData = biome.GetPhysicsData(bandInfo);
                    int cell = Grid.PosToCell(pos);
                    cells[cell].SetValues(element, elementData, ElementLoader.elements);
                    dc[cell] = new Sim.DiseaseCell()
                    {
                        diseaseIdx = Db.Get().Diseases.GetIndex(diseaseDict[bandInfo.disease]),
                        elementCount = _random.Next(10000, 1000000),
                    };
                }
            });

            // Main biome
            int relativeLeft = moonletData.Left();
            int relativeRight = moonletData.Right();
            for (int x = relativeLeft; x < relativeRight; x++)
            {
                for (int y = moonletData.Bottom(); y < moonletData.Top(); y++)
                {
                    var pos = new Vector2I(x, y);
                    int extra = (int)(noiseMap[pos.x, pos.y] * 8f);
                    
                    if (moonletData.Type != MoonletData.Moonlet.Start &&
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
            int coreHeight = CoreMin + moonletData.Height() / 10;
            int[] heights = GetHorizontalWalk(moonletData.WorldSize.x, coreHeight, coreHeight + CoreDeviation);
            ISet<Vector2I> abyssaliteCells = new HashSet<Vector2I>();
            for (int x = relativeLeft; x < relativeRight; x++)
            {
                // Create abyssalite border of size CORE_BORDER
                for (int j = 0; j < CoreBorder; j++)
                {
                    abyssaliteCells.Add(new Vector2I(x, moonletData.Bottom() + heights[x] + j));
                }

                // Ensure border thickness at high slopes
                if (x > relativeLeft && x < relativeRight - 1)
                {
                    if ((heights[x - 1] - heights[x] > 1) || (heights[x + 1] - heights[x] > 1))
                    {
                        var top = new Vector2I(x, moonletData.Bottom() + heights[x] + CoreBorder - 1);
                        abyssaliteCells.Add(top + new Vector2I(-1, 0));
                        abyssaliteCells.Add(top + new Vector2I(1, 0));
                        abyssaliteCells.Add(top + new Vector2I(0, 1));
                    }
                }

                // Mark core biome cells
                for (int y = moonletData.Bottom(); y < moonletData.Bottom() + heights[x]; y++)
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
            var borderCells = new HashSet<Vector2I>();
           
            var borderMat = WorldGen.unobtaniumElement;

            var addBorderCell = new Action<int, int, Element>((x, y, e) =>
            {
                int cell = Grid.XYToCell(x, y);
                if (Grid.IsValidCell(cell))
                {
                    borderCells.Add(new Vector2I(x, y));
                    cells[cell].SetValues(e, ElementLoader.elements);
                }
            });
            
            // Top and bottom borders
            for (int x = 0; x < moonletData.WorldSize.x; x++)
            {
                // Top border
                for (int y = moonletData.Top(false); y < moonletData.Top(true); y++)
                {
                    if (moonletData.Type == MoonletData.Moonlet.Start || x < (moonletData.Left() + CornerSize) ||
                        x > (moonletData.Right() - CornerSize))
                    {
                        addBorderCell(x, y, borderMat);
                    }
                }
                
                // Bottom border
                for (int y = moonletData.Bottom(true); y < moonletData.Bottom(false); y++)
                {
                    addBorderCell(x, y, borderMat);
                }
            }
            
            // Side borders
            for (int y = moonletData.Bottom(true); y < moonletData.Top(true); y++)
            {
                // Left border
                for (int x = moonletData.Left(true); x < moonletData.Left(false); x++)
                {
                    addBorderCell(x, y, borderMat);
                }

                // Right border
                for (int x = moonletData.Right(false); x < moonletData.Right(true); x++)
                {
                    addBorderCell(x, y, borderMat);
                }
            }

            // Corner structures
            int leftCenterX = (moonletData.Left(true) + moonletData.Left(false)) / 2;
            int rightCenterX = (moonletData.Right(false) + moonletData.Right(true)) / 2;
            int adjustedCornerSize = CornerSize + (int)Math.Ceiling(BorderSize / 2f);
            for (int i = 0; i < adjustedCornerSize; i++)
            {
                for (int j = adjustedCornerSize; j > i; j--)
                {
                    int bottomY = moonletData.Bottom() + adjustedCornerSize - j;
                    int topY = moonletData.Top() - adjustedCornerSize + j - 1;

                    borderMat = j - i <= DiagonalBorderSize
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

            // Space access
            if (moonletData.Type == MoonletData.Moonlet.Start)
            {
                if (MiniBaseOptions.Instance.SpaceAccess == MiniBaseOptions.AccessType.Classic)
                {
                    borderMat = WorldGen.katairiteElement;

                    for (int y = moonletData.Top(); y < moonletData.Top(true); y++)
                    {
                        //Left cutout
                        for (int x = moonletData.Left() + CornerSize; x < Math.Min(moonletData.Left() + CornerSize + SpaceAccessSize, moonletData.Right() - CornerSize); x++)
                        {
                            addBorderCell(x, y, borderMat);
                        }
                        
                        //Right cutout
                        for (int x = Math.Max(moonletData.Right() - CornerSize - SpaceAccessSize, moonletData.Left() + CornerSize); x < moonletData.Right() - CornerSize; x++)
                        {
                            addBorderCell(x, y, borderMat);
                        }
                    }
                    
                    if (MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.BothSides ||
                        MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.LeftOnly ||
                        MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.RightOnly)
                    {
                        //Space Tunnels
                        for (int y = moonletData.Bottom(false) + CornerSize; y < moonletData.Bottom(false) + SideAccessSize + CornerSize; y++)
                        {
                            if (MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.LeftOnly ||
                                MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.BothSides)
                            {
                                //Far left tunnel
                                for (int x = moonletData.Left(true); x < moonletData.Left(false); x++)
                                {
                                    addBorderCell(x, y, borderMat);
                                }
                            }
                            if (MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.RightOnly ||
                                MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.BothSides)
                            {
                                //Far Right tunnel
                                for (int x = moonletData.Right(false); x < moonletData.Right(true); x++)
                                {
                                    addBorderCell(x, y, borderMat);
                                }
                            }
                        }
                    }
                }
                else if (MiniBaseOptions.Instance.SpaceAccess == MiniBaseOptions.AccessType.Full)
                {
                    borderMat = WorldGen.katairiteElement;
                    for (int y = moonletData.Top(); y < moonletData.Top(true); y++)
                    {
                        for (int x = moonletData.Left() + CornerSize; x < moonletData.Right() - CornerSize; x++)
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
        private static void PlaceGeyser(Data data, Sim.Cell[] cells, MiniBaseOptions.FeatureType type, Vector2I pos, Element coverMaterial, bool neutronium = true, bool cover = true)
        {
            string featureName;
            switch (type)
            {
                case MiniBaseOptions.FeatureType.None:
                    featureName = null;
                    break;
                case MiniBaseOptions.FeatureType.RandomAny:
                    var featureType = GeyserDictionary.Keys
                        .Where(x => x != MiniBaseOptions.FeatureType.Niobium)
                        .GetRandom();
                    featureName = GeyserDictionary[featureType];
                    break;
                case MiniBaseOptions.FeatureType.RandomWater:
                    featureName = GeyserDictionary[RandomWaterFeatures.GetRandom()];
                    break;
                case MiniBaseOptions.FeatureType.RandomUseful:
                    featureName = GeyserDictionary[RandomUsefulFeatures.GetRandom()];
                    break;
                case MiniBaseOptions.FeatureType.RandomVolcano:
                    featureName = GeyserDictionary[RandomVolcanoFeatures.GetRandom()];
                    break;
                case MiniBaseOptions.FeatureType.RandomMagmaVolcano:
                    featureName = GeyserDictionary[RandomMagmaVolcanoFeatures.GetRandom()];
                    break;
                case MiniBaseOptions.FeatureType.RandomMetalVolcano:
                    featureName = GeyserDictionary[RandomMetalVolcanoFeatures.GetRandom()];
                    break;
                default:
                    if (!GeyserDictionary.TryGetValue(type, out featureName))
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
                for (int x = pos.x - 1; x < pos.x + 3; x++)
                {
                    cells[Grid.XYToCell(x, pos.y - 1)].SetValues(WorldGen.unobtaniumElement, ElementLoader.elements);
                }
            }
            
            // Cover feature
            if (cover)
            {
                for (int x = pos.x; x < pos.x + 2; x++)
                {
                    for (int y = pos.y; y < pos.y + 3; y++)
                    {
                        if (!ElementLoader.elements[cells[Grid.XYToCell(x, y)].elementIdx].IsSolid)
                        {
                            cells[Grid.XYToCell(x, y)].SetValues(coverMaterial, GetPhysicsData(coverMaterial), ElementLoader.elements);
                        }
                    }
                }
            }
        }

        private static SpawnPoints GetSpawnPoints(Sim.Cell[] cells, IEnumerable<Vector2I> biomeCells)
        {
            var spawnPoints = new SpawnPoints()
            {
                onFloor = new HashSet<Vector2I>(),
                onCeil = new HashSet<Vector2I>(),
                inGround = new HashSet<Vector2I>(),
                inAir = new HashSet<Vector2I>(),
                inLiquid = new HashSet<Vector2I>(),
            };

            foreach (var pos in biomeCells)
            {
                int cell = Grid.PosToCell(pos);
                var element = ElementLoader.elements[cells[cell].elementIdx];
                if (element.IsSolid && element.id != SimHashes.Katairite && element.id != SimHashes.Unobtanium &&
                    cells[cell].temperature < 373f)
                {
                    spawnPoints.inGround.Add(pos);
                }
                else if (element.IsGas)
                {
                    var elementBelow = ElementLoader.elements[cells[Grid.CellBelow(cell)].elementIdx];
                    if (elementBelow.IsSolid)
                    {
                        spawnPoints.onFloor.Add(pos);
                    }
                    else
                    {
                        spawnPoints.inAir.Add(pos);
                    }
                }
                else if (element.IsLiquid)
                {
                    spawnPoints.inLiquid.Add(pos);
                }
            }

            return spawnPoints;
        }

        private static void PlaceSpawnables(Sim.Cell[] cells, List<Prefab> spawnList, MiniBaseBiomeProfile biome, ISet<Vector2I> biomeCells, ISet<Vector2I> reservedCells)
        {
            var spawnStruct = GetSpawnPoints(cells, biomeCells.Except(reservedCells));
            PlaceSpawnables(spawnList, biome.SpawnablesOnFloor, spawnStruct.onFloor, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesOnCeil, spawnStruct.onCeil, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesInGround, spawnStruct.inGround, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesInLiquid, spawnStruct.inLiquid, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.SpawnablesInAir, spawnStruct.inAir, Prefab.Type.Pickupable);
        }
        
        /// <summary>
        /// Add spawnables to the GameSpawnData list
        /// </summary>
        /// <param name="spawnList"></param>
        /// <param name="spawnables"></param>
        /// <param name="spawnPoints"></param>
        /// <param name="prefabType"></param>
        private static void PlaceSpawnables(List<Prefab> spawnList, Dictionary<string, float> spawnables, ISet<Vector2I> spawnPoints, Prefab.Type prefabType)
        {
            if (spawnables == null || spawnables.Count == 0 || spawnPoints.Count == 0)
            {
                return;
            }
            foreach (KeyValuePair<string, float> spawnable in spawnables)
            {
                int numSpawnables = (int)Math.Ceiling(spawnable.Value * spawnPoints.Count);
                for (int i = 0; i < numSpawnables && spawnPoints.Count > 0; i++)
                {
                    var pos = spawnPoints.ElementAt(_random.Next(0, spawnPoints.Count));
                    spawnPoints.Remove(pos);
                    spawnList.Add(new Prefab(spawnable.Key, prefabType, pos.x, pos.y, (SimHashes)0));
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
                mass = defaultData.mass * modifier * (element.IsSolid ? MiniBaseOptions.Instance.GetResourceModifier() : 1f),
                temperature = temperature == -1 ? defaultData.temperature : temperature,
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
            Octave oct1 = new Octave(1f, 10f);
            Octave oct2 = new Octave(oct1.amp / 2, oct1.freq * 2);
            Octave oct3 = new Octave(oct2.amp / 2, oct2.freq * 2);
            float maxAmp = oct1.amp + oct2.amp + oct3.amp;
            float absolutePeriod = 100f;
            float xStretch = 2.5f;
            float zStretch = 1.6f;
            Vector2f offset = new Vector2f((float)r.NextDouble(), (float)r.NextDouble());
            float[,] noiseMap = new float[width, height];

            float total = 0f;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector2f pos = new Vector2f(i / absolutePeriod + offset.x, j / absolutePeriod + offset.y);      // Find current x,y position for the noise function
                    double e =                                                                                      // Generate a value in [0, maxAmp] with average maxAmp / 2
                        oct1.amp * Mathf.PerlinNoise(oct1.freq * pos.x / xStretch, oct1.freq * pos.y) +
                        oct2.amp * Mathf.PerlinNoise(oct2.freq * pos.x / xStretch, oct2.freq * pos.y) +
                        oct3.amp * Mathf.PerlinNoise(oct3.freq * pos.x / xStretch, oct3.freq * pos.y);

                    e = e / maxAmp;                                                                                 // Normalize to [0, 1]
                    float f = Mathf.Clamp((float)e, 0f, 1f);
                    total += f;

                    noiseMap[i, j] = f;
                }
            }

            // Center the distribution at 0.5 and stretch it to fill out [0, 1]
            float average = total / noiseMap.Length;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float f = noiseMap[i, j];
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
            double WalkChance = 0.7;
            double DoubleWalkChance = 0.25;
            int[] walk = new int[width];
            int height = _random.Next(min, max + 1);
            for (int i = 0; i < walk.Length; i++)
            {
                if (_random.NextDouble() < WalkChance)
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

                    if (_random.NextDouble() < DoubleWalkChance)
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
            public float amp;
            public float freq;
            public Octave(float amplitude, float frequency)
            {
                amp = amplitude;
                freq = frequency;
            }
        }
        public struct SpawnPoints
        {
            public ISet<Vector2I> onFloor;
            public ISet<Vector2I> onCeil;
            public ISet<Vector2I> inGround;
            public ISet<Vector2I> inAir;
            public ISet<Vector2I> inLiquid;
        }
        #endregion
        
        #region Fields
        private static System.Random _random;
        #endregion
    }
}