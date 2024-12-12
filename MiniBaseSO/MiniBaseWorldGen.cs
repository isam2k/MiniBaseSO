using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Klei;
using ProcGen;
using ProcGenGame;
using UnityEngine;
using Delaunay.Geo;
using VoronoiTree;
using TemplateClasses;
using static MiniBaseSO.MiniBaseConfig;
using static ProcGenGame.TemplateSpawning;
using MiniBaseSO.Profiles;

namespace MiniBaseSO
{
    public class MiniBaseWorldGen
    {
        private static System.Random random;

        private class MoonletData
        {
            public MiniBaseBiomeProfile Biome;
            public MiniBaseBiomeProfile CoreBiome;
            /// <summary>Total size of the map in tiles.</summary>
            public Vector2I WorldSize;
            /// <summary>Size of the map without borders.</summary>
            public Vector2I Size;
            public bool HasCore;
            public int ExtraTopMargin = 0;
            public enum Moonlet
            {
                Start,
                Second,
                Tree,
                Niobium
            };
            public Moonlet Type = Moonlet.Start;
        };

        private static MoonletData _moonletData = new MoonletData();

        /// <summary>
        /// Rewrite of WorldGen.RenderOffline
        /// </summary>
        /// <param name="gen"></param>
        /// <param name="writer"></param>
        /// <param name="cells"></param>
        /// <param name="dc"></param>
        /// <param name="baseId"></param>
        /// <param name="placedStoryTraits"></param>
        /// <returns></returns>
        public static bool CreateWorld(WorldGen gen, BinaryWriter writer, ref Sim.Cell[] cells, ref Sim.DiseaseCell[] dc, int baseId, ref List<WorldTrait> placedStoryTraits)
        {
            _moonletData = new MoonletData();
            MiniBaseOptions options = MiniBaseOptions.Instance;

            _moonletData.Biome = options.GetBiome();
            _moonletData.CoreBiome = options.GetCoreBiome();
            _moonletData.HasCore = options.HasCore();

            _moonletData.WorldSize = gen.WorldSize;
            
            switch (gen.Settings.world.filePath)
            {
                case "worlds/MiniBase":
                    _moonletData.Type = MoonletData.Moonlet.Start;
                    break;
                case "worlds/BabyOilyMoonlet":
                    _moonletData.Type = MoonletData.Moonlet.Second;
                    _moonletData.Biome = MiniBaseBiomeProfiles.OilMoonletProfile;
                    _moonletData.CoreBiome = MiniBaseCoreBiomeProfiles.MagmaCoreProfile;
                    _moonletData.HasCore = true;
                    break;
                case "worlds/BabyMarshyMoonlet":
                    _moonletData.Type = MoonletData.Moonlet.Tree;
                    _moonletData.Biome = MiniBaseBiomeProfiles.TreeMoonletProfile;
                    break;
                case "worlds/BabyNiobiumMoonlet":
                    _moonletData.Type = MoonletData.Moonlet.Niobium;
                    _moonletData.Biome = MiniBaseBiomeProfiles.NiobiumMoonletProfile;
                    break;
            }

            if (_moonletData.Type != MoonletData.Moonlet.Start)
            {
                _moonletData.ExtraTopMargin = ColonizableExtraMargin;
                _moonletData.HasCore = false;
            }
            _moonletData.Size = new Vector2I(_moonletData.WorldSize.x - 2 * BorderSize, _moonletData.WorldSize.y - 2 * BorderSize - TopMargin - _moonletData.ExtraTopMargin);
            
            // Convenience variables, including private fields/properties
            var worldGen = Traverse.Create(gen);
            var data = gen.data;
            var myRandom = worldGen.Field("myRandom").GetValue<SeededRandom>();
            var updateProgressFn = worldGen.Field("successCallbackFn").GetValue<WorldGen.OfflineCallbackFunction>();
            var errorCallback = worldGen.Field("errorCallback").GetValue<Action<OfflineWorldGen.ErrorInfo>>();
            var running = worldGen.Field("running");
            running.SetValue(true);
            random = new System.Random(data.globalTerrainSeed);

            // Initialize noise maps
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0f, WorldGenProgressStages.Stages.NoiseMapBuilder);
            var noiseMap = GenerateNoiseMap(random, _moonletData.WorldSize.x, _moonletData.WorldSize.y);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.GENERATENOISE.key, 0.9f, WorldGenProgressStages.Stages.NoiseMapBuilder);

            // Set biomes
            SetBiomes(data.overworldCells);

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
            var biomeCells = DrawCustomTerrain(data, cells, bgTemp, dc, noiseMap, out var coreCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PROCESSING.key, 0.9f, WorldGenProgressStages.Stages.Processing);

            // Printing pod
            var templateSpawnTargets = new List<TemplateSpawner>();
            var reservedCells = new HashSet<Vector2I>();
            Vector2I startPos;
            int geyserMinX, geyserMaxX, geyserMinY, geyserMaxY;
            Sim.PhysicsData physicsData;
            Element coverElement;
            TerrainCell terrainCell;
            switch (_moonletData.Type)
            {
                case MoonletData.Moonlet.Start:
                    startPos = Vec(Left() + (Width() / 2) - 1, Bottom() + (Height() / 2) + 2);
                    data.gameSpawnData.baseStartPos = startPos;
                    var startingBaseTemplate = TemplateCache.GetTemplate(gen.Settings.world.startingBaseTemplate);
                    startingBaseTemplate.pickupables.Clear(); // Remove stray hatch
                    var itemPos = new Vector2I(3, 1);
                    foreach (var entry in _moonletData.Biome.startingItems) // Add custom defined starting items
                    {
                        startingBaseTemplate.pickupables.Add(new Prefab(entry.Key, Prefab.Type.Pickupable, itemPos.x, itemPos.y, (SimHashes)0, _units: entry.Value));
                    }
                    foreach (var cell in startingBaseTemplate.cells)
                    {
                        if (cell.element == SimHashes.SandStone || cell.element == SimHashes.Algae)
                        {
                            cell.element = _moonletData.Biome.defaultMaterial;
                        }
                        reservedCells.Add(new Vector2I(cell.location_x, cell.location_y) + startPos);
                    }
                    terrainCell = data.overworldCells.Find((Predicate<TerrainCell>)(tc => tc.node.tags.Contains(WorldGenTags.StartLocation)));
                    startingBaseTemplate.cells.RemoveAll((c) => (c.location_x == -8) || (c.location_x == 9)); // Trim the starting base area
                    templateSpawnTargets.Add(new TemplateSpawner(startPos,startingBaseTemplate.GetTemplateBounds(), startingBaseTemplate, terrainCell));
                
                    // Geysers
                    geyserMinX = Left() + CornerSize + 2;
                    geyserMaxX = Right() - CornerSize - 4;
                    geyserMinY = Bottom() + CornerSize + 2;
                    geyserMaxY = Top() - CornerSize - 4;
                    coverElement = _moonletData.Biome.DefaultElement();
                    PlaceGeyser(data, cells, options.FeatureWest, Vec(Left() + 2, random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
                    PlaceGeyser(data, cells, options.FeatureEast, Vec(Right() - 4, random.Next(geyserMinY, geyserMaxY + 1)), coverElement);
                    if (_moonletData.HasCore)
                    {
                        coverElement = _moonletData.CoreBiome.DefaultElement();
                    }

                    var bottomGeyserPos = Vec(random.Next(geyserMinX, geyserMaxX + 1), Bottom());
                    PlaceGeyser(data, cells, options.FeatureSouth, bottomGeyserPos, coverElement);

                    // Change geysers to be made of abyssalite so they don't melt in magma
                    var geyserPrefabs = Assets.GetPrefabsWithComponent<Geyser>();
                    geyserPrefabs.Add(Assets.GetPrefab(GameTags.OilWell));
                    foreach (var prefab in geyserPrefabs)
                    {
                        prefab.GetComponent<PrimaryElement>().SetElement(SimHashes.Katairite);
                    }

                    if(_moonletData.HasCore && _moonletData.CoreBiome == MiniBaseCoreBiomeProfiles.RadioactiveCoreProfile)
                    {
                        // Add a single viable beeta in the radioactive core
                        bottomGeyserPos.x += bottomGeyserPos.x > Left() + Width() / 2 ? -12 : 12;

                        bottomGeyserPos.y += 1;
                        coverElement = ElementLoader.FindElementByHash(SimHashes.Wolframite);

                        cells[Grid.XYToCell(bottomGeyserPos.x, bottomGeyserPos.y)].SetValues(coverElement, GetPhysicsData(coverElement, 1f, _moonletData.Biome.defaultTemperature), ElementLoader.elements);
                        cells[Grid.XYToCell(bottomGeyserPos.x+1, bottomGeyserPos.y)].SetValues(coverElement, GetPhysicsData(coverElement, 1f, _moonletData.Biome.defaultTemperature), ElementLoader.elements);

                        coverElement = ElementLoader.FindElementByHash(SimHashes.CarbonDioxide);

                        physicsData = GetPhysicsData(coverElement, 1f, _moonletData.Biome.defaultTemperature);
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 3; y++)
                            {
                                cells[Grid.XYToCell(bottomGeyserPos.x + x, bottomGeyserPos.y + 1 + y)].SetValues(coverElement, physicsData, ElementLoader.elements);
                            }
                        }

                        data.gameSpawnData.pickupables.Add(new Prefab("BeeHive", Prefab.Type.Pickupable, bottomGeyserPos.x, bottomGeyserPos.y+1, (SimHashes)0));
                    }
                    break;
                case MoonletData.Moonlet.Second:
                    geyserMinX = Left() + CornerSize + 2;
                    geyserMaxX = Right() - CornerSize - 4;
                    geyserMinY = Bottom() + CornerSize + 2;
                    geyserMaxY = Top() - Height() / 2 - CornerSize - 4;

                    coverElement = _moonletData.Biome.DefaultElement();
                    PlaceGeyser(data, cells, MiniBaseOptions.FeatureType.OilReservoir, Vec(Left() + 2, random.Next(geyserMinY, geyserMaxY + 1)), coverElement, false, false);
                    PlaceGeyser(data, cells, MiniBaseOptions.FeatureType.OilReservoir, Vec(Right() - 4, random.Next(geyserMinY, geyserMaxY + 1)), coverElement, false, false);
                    if (_moonletData.HasCore)
                    {
                        coverElement = _moonletData.CoreBiome.DefaultElement();
                    }
                    PlaceGeyser(data, cells, MiniBaseOptions.FeatureType.Volcano, Vec(random.Next(geyserMinX, geyserMaxX + 1), Bottom()), coverElement);
                    break;
                case MoonletData.Moonlet.Tree:
                    startPos = Vec(Left() + (2*Width() / 3) - 1, Bottom() + (Height() / 2) + 2);
                    var template = TemplateCache.GetTemplate("expansion1::poi/sap_tree_room");
                    terrainCell = data.overworldCells.Find((Predicate<TerrainCell>)(tc => tc.node.tags.Contains(WorldGenTags.AtSurface)));
                    templateSpawnTargets.Add(new TemplateSpawner(startPos, template.GetTemplateBounds(), template, terrainCell));
                    break;
                case MoonletData.Moonlet.Niobium:
                    startPos = Vec(Left() + (Width() / 2) - 1, Bottom() + (Height() / 4) + 2);
                    coverElement = ElementLoader.FindElementByHash(SimHashes.Niobium);
                    PlaceGeyser(data, cells, MiniBaseOptions.FeatureType.Niobium, startPos, coverElement);

                    // Replace niobium near the surface with obsidian
                    coverElement = ElementLoader.FindElementByHash(SimHashes.Obsidian);
                    physicsData = GetPhysicsData(coverElement, 1f, _moonletData.Biome.defaultTemperature);
                    for (int x = Left(); x <= Right(); x++)
                    {
                        for(int y = Top(); y >= (Top() - (3*Height()/4)); y--)
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
            var borderCells = DrawCustomWorldBorders(cells);
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
            PlaceSpawnables(cells, data.gameSpawnData.pickupables, _moonletData.Biome, biomeCells, reservedCells);
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 0.5f, WorldGenProgressStages.Stages.PlacingCreatures);
            if (_moonletData.HasCore)
            {
                PlaceSpawnables(cells, data.gameSpawnData.pickupables, _moonletData.CoreBiome, coreCells, reservedCells);
            }
            updateProgressFn(global::STRINGS.UI.WORLDGEN.PLACINGCREATURES.key, 1f, WorldGenProgressStages.Stages.PlacingCreatures);

            // Finish
            updateProgressFn(global::STRINGS.UI.WORLDGEN.COMPLETE.key, 1f, WorldGenProgressStages.Stages.Complete);
            running.SetValue(false);
            return true;
        }

        /// <summary>
        /// Set biome background for all cells
        /// Overworld cells are large polygons that divide the map into zones (biomes)
        /// To change a biome, create an appropriate overworld cell and add it to Data.overworldCells
        /// </summary>
        /// <param name="overworldCells"></param>
        private static void SetBiomes(List<TerrainCell> overworldCells)
        {
            overworldCells.Clear();
            
            const string spaceBiome = "subworlds/space/Space";
            string backgroundBiome = _moonletData.Biome.backgroundSubworld;

            uint cellId = 0;
            void CreateOverworldCell(string type, Polygon bs, TagSet ts)
            {
                ProcGen.Map.Cell cell = new ProcGen.Map.Cell(); // biome
                cell.SetType(type);
                foreach (var tag in ts)
                {
                    cell.tags.Add(tag);
                }
                Diagram.Site site = new Diagram.Site();
                site.id = cellId++;
                site.poly = bs; // bounds of the overworld cell
                site.position = site.poly.Centroid();
                overworldCells.Add(new TerrainCellLogged(cell, site, new Dictionary<Tag, int>()));
            };

            // Vertices of the liveable area (octogon)
            Vector2I bottomLeftSe = BottomLeft() + Vec(CornerSize, 0),
                bottomLeftNw = BottomLeft() + Vec(0, CornerSize),
                topLeftSw = TopLeft() - Vec(0, CornerSize) - Vec(0, 1),
                topLeftNe = TopLeft() + Vec(CornerSize, 0),
                topRightNw = TopRight() - Vec(CornerSize, 0) - Vec(1, 0),
                topRightSe = TopRight() - Vec(0, CornerSize) - Vec(0, 1),
                bottomRightNe = BottomRight() + Vec(0, CornerSize),
                bottomRightSw = BottomRight() - Vec(CornerSize, 0);

            // Liveable cell
            var tags = new TagSet();

            if (_moonletData.Type ==MoonletData.Moonlet.Start)
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
            CreateOverworldCell(backgroundBiome, bounds, tags);

            // Top cell
            tags = new TagSet();
            bounds = new Polygon(new Rect(0f, Top(), _moonletData.WorldSize.x, _moonletData.WorldSize.y - Top()));
            CreateOverworldCell(spaceBiome, bounds, tags);

            // Bottom cell
            tags = new TagSet();
            bounds = new Polygon(new Rect(0f, 0, _moonletData.WorldSize.x, Bottom()));
            CreateOverworldCell(spaceBiome, bounds, tags);

            // Left side cell
            tags = new TagSet();
            vertices = new List<Vector2>()
            {
                Vec(0, Bottom()),
                Vec(0, Top()),
                topLeftNe,
                topLeftSw,
                bottomLeftNw,
                bottomLeftSe,
            };
            bounds = new Polygon(vertices);
            CreateOverworldCell(spaceBiome, bounds, tags);

            // Right side cell
            tags = new TagSet();
            vertices = new List<Vector2>()
            {
                bottomRightSw,
                bottomRightNe,
                topRightSe,
                topRightNw,
                Vec(_moonletData.WorldSize.x, Top()),
                Vec(_moonletData.WorldSize.x, Bottom()),
            };
            bounds = new Polygon(vertices);
            CreateOverworldCell(spaceBiome, bounds, tags);
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
        /// <param name="data"></param>
        /// <param name="cells"></param>
        /// <param name="bgTemp"></param>
        /// <param name="dc"></param>
        /// <param name="noiseMap"></param>
        /// <param name="coreCells"></param>
        /// <returns></returns>
        private static ISet<Vector2I> DrawCustomTerrain(Data data, Sim.Cell[] cells, float[] bgTemp, Sim.DiseaseCell[] dc, float[,] noiseMap, out ISet<Vector2I> coreCells)
        {
            var options = MiniBaseOptions.Instance;
            var biomeCells = new HashSet<Vector2I>();
            var sideCells = new HashSet<Vector2I>();
            var claimedCells = new HashSet<int>();
            coreCells = new HashSet<Vector2I>();
            for (int index = 0; index < data.terrainCells.Count; ++index)
                data.terrainCells[index].InitializeCells(claimedCells);
            if (MiniBaseOptions.Instance.SkipLiveableArea)
                return biomeCells;

            // Using a smooth noisemap, map the noise values to elements via the element band profile
            void SetTerrain(MiniBaseBiomeProfile biome, ISet<Vector2I> positions)
            {
                var DiseaseDb = Db.Get().Diseases;
                var DiseaseDict = new Dictionary<MiniBaseConfig.DiseaseID, byte>()
                {
                    { MiniBaseConfig.DiseaseID.None, byte.MaxValue},
                    { MiniBaseConfig.DiseaseID.Slimelung, DiseaseDb.GetIndex(DiseaseDb.SlimeGerms.id)},
                    { MiniBaseConfig.DiseaseID.FoodPoisoning, DiseaseDb.GetIndex(DiseaseDb.FoodGerms.id)},
                };

                foreach (var pos in positions)
                {
                    float e = noiseMap[pos.x, pos.y];
                    BandInfo bandInfo = biome.GetBand(e);
                    Element element = bandInfo.GetElement();
                    Sim.PhysicsData elementData = biome.GetPhysicsData(bandInfo);
                    int cell = Grid.PosToCell(pos);
                    cells[cell].SetValues(element, elementData, ElementLoader.elements);
                    dc[cell] = new Sim.DiseaseCell()
                    {
                        diseaseIdx = Db.Get().Diseases.GetIndex(DiseaseDict[bandInfo.disease]),
                        elementCount = random.Next(10000, 1000000),
                    };
                }
            }

            // Main biome
            int relativeLeft = Left();
            int relativeRight = Right();
            for (int x = relativeLeft; x < relativeRight; x++)
                for (int y = Bottom(); y < Top(); y++)
                {
                    var pos = Vec(x, y);
                    int extra = (int)(noiseMap[pos.x, pos.y] * 8f);
                    if (_moonletData.Type != MoonletData.Moonlet.Start && y > Bottom() + extra + (2 * Height() / 3)) continue; 

                    if (InLiveableArea(pos))
                        biomeCells.Add(pos);
                    else
                        sideCells.Add(pos);
                }
            SetTerrain(_moonletData.Biome, biomeCells);
            SetTerrain(_moonletData.Biome, sideCells);

            // Core area
            if (_moonletData.HasCore)
            {
                int coreHeight = CoreMin + Height() / 10;
                int[] heights = GetHorizontalWalk(_moonletData.WorldSize.x, coreHeight, coreHeight + CoreDeviation);
                ISet<Vector2I> abyssaliteCells = new HashSet<Vector2I>();
                for (int x = relativeLeft; x < relativeRight; x++)
                {
                    // Create abyssalite border of size CORE_BORDER
                    for (int j = 0; j < CoreBorder; j++)
                        abyssaliteCells.Add(Vec(x, Bottom() + heights[x] + j));

                    // Ensure border thickness at high slopes
                    if (x > relativeLeft && x < relativeRight - 1)
                        if ((heights[x - 1] - heights[x] > 1) || (heights[x + 1] - heights[x] > 1))
                        {
                            Vector2I top = Vec(x, Bottom() + heights[x] + CoreBorder - 1);
                            abyssaliteCells.Add(top + Vec(-1, 0));
                            abyssaliteCells.Add(top + Vec(1, 0));
                            abyssaliteCells.Add(top + Vec(0, 1));
                        }

                    // Mark core biome cells
                    for (int y = Bottom(); y < Bottom() + heights[x]; y++)
                        coreCells.Add(Vec(x, y));
                }
                coreCells.ExceptWith(abyssaliteCells);
                SetTerrain(_moonletData.CoreBiome, coreCells);
                foreach (Vector2I abyssaliteCell in abyssaliteCells)
                    cells[Grid.PosToCell(abyssaliteCell)].SetValues(WorldGen.katairiteElement, ElementLoader.elements);
                biomeCells.ExceptWith(coreCells);
                biomeCells.ExceptWith(abyssaliteCells);
            }
            return biomeCells;
        }

        /// <summary>
        /// Rewrite of WorldGen.DrawWorldBorder
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Set of border cells.</returns>
        private static ISet<Vector2I> DrawCustomWorldBorders(Sim.Cell[] cells)
        {
            ISet<Vector2I> borderCells = new HashSet<Vector2I>();
           
            Element borderMat = WorldGen.unobtaniumElement;
            
            // Top and bottom borders
            for (int x = 0; x < _moonletData.WorldSize.x; x++)
            {
                // Top border
                for (int y = Top(false); y < Top(true); y++)
                {
                    if (_moonletData.Type == MoonletData.Moonlet.Start || x < (Left() + CornerSize) ||
                        x > (Right() - CornerSize))
                    {
                        AddBorderCell(x, y, borderMat);
                    }
                }
                
                // Bottom border
                for (int y = Bottom(true); y < Bottom(false); y++)
                {
                    AddBorderCell(x, y, borderMat);
                }
            }
            
            // Side borders
            for (int y = Bottom(true); y < Top(true); y++)
            {
                // Left border
                for (int x = Left(true); x < Left(false); x++)
                {
                    AddBorderCell(x, y, borderMat);
                }

                // Right border
                for (int x = Right(false); x < Right(true); x++)
                {
                    AddBorderCell(x, y, borderMat);
                }
            }

            // Corner structures
            int leftCenterX = (Left(true) + Left(false)) / 2;
            int rightCenterX = (Right(false) + Right(true)) / 2;
            int adjustedCornerSize = CornerSize + (int)Math.Ceiling(BorderSize / 2f);
            for (int i = 0; i < adjustedCornerSize; i++)
            {
                for (int j = adjustedCornerSize; j > i; j--)
                {
                    int bottomY = Bottom() + adjustedCornerSize - j;
                    int topY = Top() - adjustedCornerSize + j - 1;

                    borderMat = j - i <= DiagonalBorderSize
                        ? WorldGen.unobtaniumElement
                        : ElementLoader.FindElementByHash(SimHashes.Glass);
                    
                    AddBorderCell(leftCenterX + i, bottomY, borderMat);
                    AddBorderCell(leftCenterX - i, bottomY, borderMat);
                    AddBorderCell(rightCenterX + i, bottomY, borderMat);
                    AddBorderCell(rightCenterX - i, bottomY, borderMat);
                    
                    AddBorderCell(leftCenterX + i, topY, borderMat);
                    AddBorderCell(leftCenterX - i, topY, borderMat);
                    AddBorderCell(rightCenterX + i, topY, borderMat);
                    AddBorderCell(rightCenterX - i, topY, borderMat);
                }
            }

            // Space access
            if (_moonletData.Type == MoonletData.Moonlet.Start)
            {
                if (MiniBaseOptions.Instance.SpaceAccess == MiniBaseOptions.AccessType.Classic)
                {
                    borderMat = WorldGen.katairiteElement;

                    for (int y = Top(); y < Top(true); y++)
                    {
                        //Left cutout
                        for (int x = Left() + CornerSize; x < Math.Min(Left() + CornerSize + SpaceAccessSize, Right() - CornerSize); x++)
                        {
                            AddBorderCell(x, y, borderMat);
                        }
                        
                        //Right cutout
                        for (int x = Math.Max(Right() - CornerSize - SpaceAccessSize, Left() + CornerSize); x < Right() - CornerSize; x++)
                        {
                            AddBorderCell(x, y, borderMat);
                        }
                    }
                    
                    if (MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.BothSides ||
                        MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.LeftOnly ||
                        MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.RightOnly)
                    {
                        //Space Tunnels
                        for (int y = Bottom(false) + CornerSize; y < Bottom(false) + SideAccessSize + CornerSize; y++)
                        {
                            if (MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.LeftOnly ||
                                MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.BothSides)
                            {
                                //Far left tunnel
                                for (int x = Left(true); x < Left(false); x++)
                                {
                                    AddBorderCell(x, y, borderMat);
                                }
                            }
                            if (MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.RightOnly ||
                                MiniBaseOptions.Instance.TunnelAccess == MiniBaseOptions.TunnelAccessType.BothSides)
                            {
                                //Far Right tunnel
                                for (int x = Right(false); x < Right(true); x++)
                                {
                                    AddBorderCell(x, y, borderMat);
                                }
                            }
                        }
                    }
                }
                else if (MiniBaseOptions.Instance.SpaceAccess == MiniBaseOptions.AccessType.Full)
                {
                    borderMat = WorldGen.katairiteElement;
                    for (int y = Top(); y < Top(true); y++)
                    {
                        for (int x = Left() + CornerSize; x < Right() - CornerSize; x++)
                        {
                            AddBorderCell(x, y, borderMat);
                        }
                    }
                }
            }
            return borderCells;
            
            void AddBorderCell(int x, int y, Element e)
            {
                int cell = Grid.XYToCell(x, y);
                if (Grid.IsValidCell(cell))
                {
                    borderCells.Add(Vec(x, y));
                    cells[cell].SetValues(e, ElementLoader.elements);
                }
            }
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
                    MiniBaseOptions.FeatureType feature_type;
                    do
                    {
                        feature_type = ChooseRandom(GeyserDictionary.Keys.ToArray());
                    } while (feature_type == MiniBaseOptions.FeatureType.Niobium);
                    featureName = GeyserDictionary[feature_type];
                    break;
                case MiniBaseOptions.FeatureType.RandomWater:
                    featureName = GeyserDictionary[ChooseRandom(RandomWaterFeatures)];
                    break;
                case MiniBaseOptions.FeatureType.RandomUseful:
                    featureName = GeyserDictionary[ChooseRandom(RandomUsefulFeatures)];
                    break;
                case MiniBaseOptions.FeatureType.RandomVolcano:
                    featureName = GeyserDictionary[ChooseRandom(RandomVolcanoFeatures)];
                    break;
                case MiniBaseOptions.FeatureType.RandomMagmaVolcano:
                    featureName = GeyserDictionary[ChooseRandom(RandomMagmaVolcanoFeatures)];
                    break;
                case MiniBaseOptions.FeatureType.RandomMetalVolcano:
                    featureName = GeyserDictionary[ChooseRandom(RandomMetalVolcanoFeatures)];
                    break;
                default:
                    if (GeyserDictionary.ContainsKey(type))
                        featureName = GeyserDictionary[type];
                    else
                        featureName = null;
                    break;
            }
            if (featureName == null)
                return;

            Prefab feature = new Prefab(featureName, Prefab.Type.Other, pos.x, pos.y, SimHashes.Katairite);
            data.gameSpawnData.otherEntities.Add(feature);

            // Base
            if (neutronium)
            {
                for (int x = pos.x - 1; x < pos.x + 3; x++)
                    cells[Grid.XYToCell(x, pos.y - 1)].SetValues(WorldGen.unobtaniumElement, ElementLoader.elements);
            }
            // Cover feature
            if (cover)
            {
                for (int x = pos.x; x < pos.x + 2; x++)
                    for (int y = pos.y; y < pos.y + 3; y++)
                        if (!ElementLoader.elements[cells[Grid.XYToCell(x, y)].elementIdx].IsSolid)
                            cells[Grid.XYToCell(x, y)].SetValues(coverMaterial, GetPhysicsData(coverMaterial), ElementLoader.elements);
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

            foreach (Vector2I pos in biomeCells)
            {
                int cell = Grid.PosToCell(pos);
                Element element = ElementLoader.elements[cells[cell].elementIdx];
                if (element.IsSolid && element.id != SimHashes.Katairite && element.id != SimHashes.Unobtanium && cells[cell].temperature < 373f)
                    spawnPoints.inGround.Add(pos);
                else if (element.IsGas)
                {
                    Element elementBelow = ElementLoader.elements[cells[Grid.CellBelow(cell)].elementIdx];
                    if (elementBelow.IsSolid)
                        spawnPoints.onFloor.Add(pos);
                    else
                        spawnPoints.inAir.Add(pos);
                }
                else if (element.IsLiquid)
                    spawnPoints.inLiquid.Add(pos);
            }

            return spawnPoints;
        }

        public struct SpawnPoints
        {
            public ISet<Vector2I> onFloor;
            public ISet<Vector2I> onCeil;
            public ISet<Vector2I> inGround;
            public ISet<Vector2I> inAir;
            public ISet<Vector2I> inLiquid;
        }

        private static void PlaceSpawnables(Sim.Cell[] cells, List<Prefab> spawnList, MiniBaseBiomeProfile biome, ISet<Vector2I> biomeCells, ISet<Vector2I> reservedCells)
        {
            var spawnStruct = GetSpawnPoints(cells, biomeCells.Except(reservedCells));
            PlaceSpawnables(spawnList, biome.spawnablesOnFloor, spawnStruct.onFloor, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.spawnablesOnCeil, spawnStruct.onCeil, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.spawnablesInGround, spawnStruct.inGround, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.spawnablesInLiquid, spawnStruct.inLiquid, Prefab.Type.Pickupable);
            PlaceSpawnables(spawnList, biome.spawnablesInAir, spawnStruct.inAir, Prefab.Type.Pickupable);
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
                return;
            foreach (KeyValuePair<string, float> spawnable in spawnables)
            {
                int numSpawnables = (int)Math.Ceiling(spawnable.Value * spawnPoints.Count);
                for (int i = 0; i < numSpawnables && spawnPoints.Count > 0; i++)
                {
                    var pos = spawnPoints.ElementAt(random.Next(0, spawnPoints.Count));
                    spawnPoints.Remove(pos);
                    spawnList.Add(new Prefab(spawnable.Key, prefabType, pos.x, pos.y, (SimHashes)0));
                }
            }
        }

        #region Util

        // The following utility methods all refer to the main liveable area
        // E.g., Width() returns the width of the liveable area, not the whole map
        private static int SideMargin() { return (_moonletData.WorldSize.x - _moonletData.Size.x - 2 * BorderSize) / 2; }
        /// <summary>
        /// The leftmost tile position that is considered inside the liveable area or its borders.
        /// </summary>
        /// <param name="withBorders"></param>
        /// <returns></returns>
        public static int Left(bool withBorders = false) { return SideMargin() + (withBorders ? 0 : BorderSize); }
        /// <summary>
        /// The rightmost tile position that is considered inside the liveable area or its borders.
        /// </summary>
        /// <param name="withBorders"></param>
        /// <returns></returns>
        public static int Right(bool withBorders = false) { return Left(withBorders) + _moonletData.Size.x + (withBorders ? BorderSize * 2 : 0); }
        public static int Top(bool withBorders = false) { return _moonletData.WorldSize.y - TopMargin - _moonletData.ExtraTopMargin - (withBorders ? 0 : BorderSize) + 1; }
        public static int Bottom(bool withBorders = false) { return Top(withBorders) - _moonletData.Size.y - (withBorders ? BorderSize * 2 : 0); }
        public static int Width(bool withBorders = false) { return Right(withBorders) - Left(withBorders); }
        public static int Height(bool withBorders = false) { return Top(withBorders) - Bottom(withBorders); }
        public static Vector2I TopLeft(bool withBorders = false) { return Vec(Left(withBorders), Top(withBorders)); }
        public static Vector2I TopRight(bool withBorders = false) { return Vec(Right(withBorders), Top(withBorders)); }
        public static Vector2I BottomLeft(bool withBorders = false) { return Vec(Left(withBorders), Bottom(withBorders)); }
        public static Vector2I BottomRight(bool withBorders = false) { return Vec(Right(withBorders), Bottom(withBorders)); }
        public static Vector2I Vec(int a, int b) { return new Vector2I(a, b); }
        public static bool InLiveableArea(Vector2I pos) { return pos.x >= Left() && pos.x < Right() && pos.y >= Bottom() && pos.y < Top(); }

        public static Sim.PhysicsData GetPhysicsData(Element element, float modifier = 1f, float temperature = -1f)
        {
            Sim.PhysicsData defaultData = element.defaultValues;
            return new Sim.PhysicsData()
            {
                mass = defaultData.mass * modifier * (element.IsSolid ? MiniBaseOptions.Instance.GetResourceModifier() : 1f),
                temperature = temperature == -1 ? defaultData.temperature : temperature,
                pressure = defaultData.pressure
            };
        }
        public static T ChooseRandom<T>(T[] tArray) { return tArray[random.Next(0, tArray.Length)]; }

        /// <summary>
        /// Returns a coherent noisemap normalized between [0.0, 1.0]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        /// <remarks>currently, the range is actually [yCorrection, 1.0] roughly centered around 0.5</remarks>
        public static float[,] GenerateNoiseMap(System.Random r, int width, int height)
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
                for (int j = 0; j < height; j++)
                {
                    float f = noiseMap[i, j];
                    f -= average;
                    f *= zStretch;
                    f += 0.5f;
                    noiseMap[i, j] = Mathf.Clamp(f, 0f, 1f);
                }

            return noiseMap;
        }

        public struct Octave
        {
            public float amp;
            public float freq;
            public Octave(float amplitude, float frequency)
            {
                amp = amplitude;
                freq = frequency;
            }
        }

        /// <summary>
        /// Return a description of a vertical movement over a horizontal axis
        /// </summary>
        /// <param name="width"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int[] GetHorizontalWalk(int width, int min, int max)
        {
            double WalkChance = 0.7;
            double DoubleWalkChance = 0.25;
            int[] walk = new int[width];
            int height = random.Next(min, max + 1);
            for (int i = 0; i < walk.Length; i++)
            {
                if (random.NextDouble() < WalkChance)
                {
                    int direction;
                    if (height >= max)
                        direction = -1;
                    else if (height <= min)
                        direction = 1;
                    else
                        direction = random.NextDouble() < 0.5 ? 1 : -1;
                    if (random.NextDouble() < DoubleWalkChance)
                        direction *= 2;
                    height = Mathf.Clamp(height + direction, min, max);
                }
                walk[i] = height;
            }
            return walk;
        }

        #endregion
    }
}