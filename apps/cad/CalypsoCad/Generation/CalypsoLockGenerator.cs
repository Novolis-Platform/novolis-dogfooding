using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CalypsoCad.Models;
using CalypsoCad.Services;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Structure;

namespace CalypsoCad.Generation;

/// <summary>
/// Rev H lock-driven Calypso freighter from CAL-INT-GA-001.json (LOA 69 mid-stretch).
/// Emits spaces/walls/openings, vacuum-assisted D3 hatches, L-airlocks, 5 cabins, structure BOM/mass.
/// </summary>
internal static class CalypsoLockGenerator
{
    public const float DeckSpacing = 4f;
    public const float WallThickness = 0.15f;
    public const float OuterSkinAreaM2 = 3796.055f;

    private static readonly Guid ShapeHullExt = Guid.Parse("e0000000-0000-4000-8000-000000000001");
    private static readonly Guid ShapeHullInt = Guid.Parse("e0000000-0000-4000-8000-000000000002");
    private static readonly Guid ShapeCorridor = Guid.Parse("e0000000-0000-4000-8000-000000000003");
    private static readonly Guid ShapeCargo = Guid.Parse("e0000000-0000-4000-8000-000000000004");
    private static readonly Guid ShapeHab = Guid.Parse("e0000000-0000-4000-8000-000000000005");
    private static readonly Guid ShapeEng = Guid.Parse("e0000000-0000-4000-8000-000000000006");
    private static readonly Guid ShapeUtil = Guid.Parse("e0000000-0000-4000-8000-000000000007");
    private static readonly Guid ShapeBridge = Guid.Parse("e0000000-0000-4000-8000-000000000008");
    private static readonly Guid ShapeLining = Guid.Parse("e0000000-0000-4000-8000-000000000009");

    private static readonly Guid LayerHull = Guid.Parse("d1000000-0000-4000-8000-000000000001");
    private static readonly Guid LayerWall = Guid.Parse("d1000000-0000-4000-8000-000000000002");
    private static readonly Guid LayerDoor = Guid.Parse("d1000000-0000-4000-8000-000000000003");
    private static readonly Guid LayerCorr = Guid.Parse("d1000000-0000-4000-8000-000000000004");
    private static readonly Guid LayerCargo = Guid.Parse("d1000000-0000-4000-8000-000000000005");
    private static readonly Guid LayerHab = Guid.Parse("d1000000-0000-4000-8000-000000000006");
    private static readonly Guid LayerEng = Guid.Parse("d1000000-0000-4000-8000-000000000007");
    private static readonly Guid LayerUtil = Guid.Parse("d1000000-0000-4000-8000-000000000008");
    private static readonly Guid LayerBridge = Guid.Parse("d1000000-0000-4000-8000-000000000009");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string DefaultOutputDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Novolis", "CalypsoCad", "generated");

    public static string Generate(string? outputDirectory = null)
    {
        var dir = outputDirectory ?? DefaultOutputDirectory;
        var stamp = DateTime.UtcNow.ToString("o");
        List<CadEntity>? preservedExterior = null;
        var existingCad = Path.Combine(dir, "calypso.cadjson");
        if (File.Exists(existingCad))
        {
            try
            {
                var prev = JsonSerializer.Deserialize<CadDocument>(File.ReadAllText(existingCad), CadJson.Options);
                preservedExterior = prev?.Entities
                    .Where(IsHandAuthoredExterior)
                    .Select(CloneEntity)
                    .ToList();
            }
            catch
            {
                // Corrupt prior — regenerate.
            }
        }

        var lockDoc = LoadLock();
        var layers = BuildLayers(stamp);
        var shapes = BuildShapes(stamp);
        var cad = BuildCad(stamp, lockDoc);
        if (preservedExterior is { Count: > 0 })
            cad.Entities.AddRange(preservedExterior);
        CadDocumentStore.WriteAll(dir, layers, shapes, cad);
        return dir;
    }

    /// <summary>
    /// Keep only hand-authored exterior (e.g. acceptance / Draft Studio saves).
    /// Generator-owned hull/nacelle/C40 must be rebuilt each regenerate — otherwise the old OML blob sticks.
    /// </summary>
    private static bool IsHandAuthoredExterior(CadEntity entity)
    {
        if (!Novolis.Avalonia.Cad.Ship.Services.CadShipExterior.IsPreservedExterior(entity))
            return false;
        var name = entity.Name ?? "";
        if (name.StartsWith("ext-acceptance", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("ext-user-", StringComparison.OrdinalIgnoreCase))
            return true;
        if (entity.Properties is not null
            && entity.Properties.TryGetValue("handAuthored", out var el)
            && el.ValueKind is JsonValueKind.True)
            return true;
        // Draft Studio meshes with exterior=true but not generator names.
        if (string.Equals(entity.Kind, "mesh", StringComparison.OrdinalIgnoreCase)
            && !name.StartsWith("ext-oml", StringComparison.OrdinalIgnoreCase)
            && !name.StartsWith("ext-hull", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static CalypsoLockDocument LoadLock()
    {
        foreach (var path in CandidateLockPaths())
        {
            if (!File.Exists(path))
                continue;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CalypsoLockDocument>(json, JsonOpts)
                   ?? throw new InvalidOperationException($"Failed to parse lock JSON: {path}");
        }

        throw new FileNotFoundException(
            "CAL-INT-GA-001.json not found. Expected under docs/internals or output lock/.");
    }

    private static IEnumerable<string> CandidateLockPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "lock", "CAL-INT-GA-001.json");
        var proj = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        yield return Path.Combine(proj, "docs", "internals", "CAL-INT-GA-001.json");
        yield return Path.Combine(
            @"d:\novolis\novolis-dogfooding\apps\cad\CalypsoCad\docs\internals",
            "CAL-INT-GA-001.json");
    }

    private static CadEntity CloneEntity(CadEntity src)
    {
        var json = JsonSerializer.Serialize(src, CadJson.Options);
        return JsonSerializer.Deserialize<CadEntity>(json, CadJson.Options)
               ?? throw new InvalidOperationException("Failed to clone preserved exterior entity");
    }

    private static CadLayersDocument BuildLayers(string stamp) => new()
    {
        Name = "Calypso ship layers",
        Standard = "custom",
        StandardVersion = "1",
        CreatedAt = stamp,
        ModifiedAt = stamp,
        Layers =
        [
            Cat(LayerHull, "S-HULL", "S", "HULL", "Outer / inner hull shell", [0.42f, 0.48f, 0.55f]),
            Cat(LayerWall, "A-WALL", "A", "WALL", "Interior partitions", [0.45f, 0.55f, 0.7f]),
            Cat(LayerDoor, "A-DOOR", "A", "DOOR", "Doors and hatches", [0.7f, 0.55f, 0.3f]),
            Cat(LayerCorr, "A-CORR", "A", "CORR", "Corridors", [0.35f, 0.55f, 0.4f]),
            Cat(LayerCargo, "A-ZONE-CARGO", "A", "ZONE", "Cargo", [0.3f, 0.4f, 0.32f], ["CARG"]),
            Cat(LayerHab, "A-ZONE-HAB", "A", "ZONE", "Habitability", [0.45f, 0.52f, 0.58f], ["HAB"]),
            Cat(LayerEng, "A-ZONE-ENG", "A", "ZONE", "Engineering", [0.55f, 0.38f, 0.35f], ["ENG"]),
            Cat(LayerUtil, "A-ZONE-UTIL", "A", "ZONE", "Utilities", [0.42f, 0.44f, 0.55f], ["UTIL"]),
            Cat(LayerBridge, "A-ZONE-BRIDGE", "A", "ZONE", "Bridge", [0.4f, 0.42f, 0.48f], ["BRDG"]),
        ],
    };

    private static CadCatalogLayer Cat(Guid id, string name, string disc, string major, string desc, float[] color, List<string>? minor = null) =>
        new()
        {
            Id = id,
            Name = name,
            Discipline = disc,
            Major = major,
            Minor = minor,
            Description = desc,
            DefaultColor = color,
            DefaultLineWeightMm = 0.35f,
        };

    private static CadShapesDocument BuildShapes(string stamp) => new()
    {
        Name = "Calypso Rev H lock shapes",
        CreatedAt = stamp,
        ModifiedAt = stamp,
        BaseDocument = "calypso.cadjson",
        Shapes =
        [
            Shape(ShapeHullExt, "hull-exterior", [0.42f, 0.48f, 0.55f], "hull-steel", 0.4f, 0.65f),
            Shape(ShapeHullInt, "hull-interior", [0.62f, 0.64f, 0.66f], "hull-lining", 0.72f, 0.08f),
            Shape(ShapeCorridor, "corridor-deck", [0.28f, 0.30f, 0.32f], "corridor-plate", 0.78f, 0.12f),
            Shape(ShapeCargo, "cargo-plate", [0.30f, 0.33f, 0.30f], "cargo-deck-plate", 0.62f, 0.2f),
            Shape(ShapeHab, "hab-fill", [0.48f, 0.52f, 0.56f], "hab-finish", 0.7f, 0.05f),
            Shape(ShapeEng, "eng-fill", [0.42f, 0.34f, 0.32f], "eng-deck", 0.55f, 0.18f),
            Shape(ShapeUtil, "util-fill", [0.38f, 0.40f, 0.46f], "util-deck", 0.65f, 0.1f),
            Shape(ShapeBridge, "bridge-fill", [0.36f, 0.38f, 0.42f], "bridge-console-deck", 0.48f, 0.22f),
            Shape(ShapeLining, "interior-lining", [0.70f, 0.72f, 0.74f], "cabin-lining", 0.82f, 0.04f),
        ],
    };

    private static CadShape Shape(Guid id, string name, float[] color, string preset, float roughness, float metalness) =>
        new()
        {
            Id = id,
            Name = name,
            Extensions = new CadShapeExtensions
            {
                Appearance = new CadAppearanceExtension
                {
                    Fill = new CadFill { Enabled = true, Color = color },
                    Stroke = new CadStroke { Color = color, LineWeightMm = 0.25f },
                },
                Material = new CadMaterialExtension
                {
                    Preset = preset,
                    Albedo = color,
                    Roughness = roughness,
                    Metalness = metalness,
                },
            },
        };

    private static CadDocument BuildCad(string stamp, CalypsoLockDocument lockDoc)
    {
        var env = lockDoc.Envelope ?? throw new InvalidOperationException("Lock missing envelope");
        var loa = (float)env.Loa;
        var beam = (float)env.Beam;
        var oah = (float)env.Oah;
        var entities = new List<CadEntity>();
        var spaceByKey = new Dictionary<string, CadEntity>(StringComparer.OrdinalIgnoreCase);
        var openingById = new Dictionary<string, CadEntity>(StringComparer.OrdinalIgnoreCase);

        // Faceted OML from manufacturer pepakura (not a solid blob).
        if (TryBuildManufacturerHullMesh(loa, beam, oah, out var hullMesh))
            entities.Add(hullMesh);
        else
            entities.AddRange(BuildSteppedHullBoxes(loa, beam, oah));

        AddExteriorDetails(entities, lockDoc, loa, beam, oah);

        foreach (var comp in lockDoc.Compartments ?? [])
        {
            foreach (var (deck, up0, up1) in ExpandDecks(comp, lockDoc))
            {
                var key = SpaceKey(comp.Id!, deck);
                var (layer, shape) = Classify(comp.Id!);
                var space = MakeSpace(comp.Id!, deck, up0, up1, comp, loa, layer, shape);
                entities.Add(space);
                spaceByKey[key] = space;
                AddRoomWalls(entities, space, deck, up0, up1 - up0, loa);
            }
        }

        foreach (var al in lockDoc.Airlocks ?? [])
        {
            var deck = 0;
            var up0 = (float)al.Up0;
            var up1 = (float)al.Up1;
            var fake = new LockCompartment
            {
                Id = al.Id,
                Z0 = al.Z0,
                Z1 = al.Z1,
                Y0 = al.Y0,
                Y1 = al.Y1,
                Up0 = al.Up0,
                Up1 = al.Up1,
            };
            var space = MakeSpace(al.Id!, deck, up0, up1, fake, loa, LayerWall, ShapeLining);
            entities.Add(space);
            spaceByKey[SpaceKey(al.Id!, deck)] = space;
            AddRoomWalls(entities, space, deck, up0, up1 - up0, loa);
        }

        foreach (var hatch in lockDoc.Hatches ?? [])
        {
            var deck = hatch.Deck;
            var c = LockToWorld((float)hatch.Y, (float)hatch.Up, (float)hatch.Z, loa);
            var clearW = (float)hatch.ClearW;
            var clearH = (float)hatch.ClearH;
            var wall = MakeHatchWall(hatch.Id!, deck, c, clearW, clearH, hatch.Normal);
            entities.Add(wall);

            var halfW = clearW * 0.5f;
            var halfD = WallThickness;
            List<float[]> fp =
            [
                SvgCoords.ToArray(c + new Vector3(-halfW, 0f, -halfD)),
                SvgCoords.ToArray(c + new Vector3(halfW, 0f, -halfD)),
                SvgCoords.ToArray(c + new Vector3(halfW, 0f, halfD)),
                SvgCoords.ToArray(c + new Vector3(-halfW, 0f, halfD)),
            ];

            var opening = new CadEntity
            {
                Kind = "opening",
                Name = hatch.Id,
                OpeningType = "hatch",
                LayerId = LayerDoor,
                Deck = deck,
                Height = clearH,
                HostWallId = wall.Id,
                Footprint = fp,
                ConnectsSides = ["A", "B"],
            };

            var isOuterVacuum = string.Equals(hatch.To, "SPACE", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(hatch.Faces, "outer hull", StringComparison.OrdinalIgnoreCase)
                               || hatch.Id!.StartsWith("D3-", StringComparison.OrdinalIgnoreCase);
            if (isOuterVacuum)
                ShipCad.TagVacuumAssistedHatch(opening, clearW, clearH, leafState: ShipLeafState.Closed);
            else
                ShipCad.TagOpeningPressure(opening, ShipPressureClass.Habitable, clearW, clearH);

            entities.Add(opening);
            openingById[hatch.Id!] = opening;
        }

        // Port / starboard L-airlocks: vestibule = chamber A, outer = D3, inner = D1
        foreach (var side in new[] { "port", "stbd" })
        {
            var letter = side[0].ToString().ToUpperInvariant();
            var vestibuleKey = SpaceKey($"AIRLOCK_A_{side}", 0);
            if (!spaceByKey.TryGetValue(vestibuleKey, out var vestibule))
                continue;
            if (!openingById.TryGetValue($"D3-{letter}", out var outer))
                continue;
            if (!openingById.TryGetValue($"D1-{letter}", out var inner))
                continue;
            entities.Add(ShipCad.CreateAirlock($"L-Airlock {side}", vestibule.Id, outer.Id, inner.Id));
        }

        // Hab pressure volume: all non-hold, non-airlock spaces on decks −1/0/+1
        var habMembers = spaceByKey.Values
            .Where(s => s.Name is not null
                        && !s.Name.StartsWith("AIRLOCK", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(s.Name, "HOLD", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Id)
            .ToList();
        entities.Add(ShipCad.CreatePressureVolume("Habitable", habMembers, "habitable", 101.3f));

        if (spaceByKey.TryGetValue(SpaceKey("HOLD", 0), out var holdSpace)
            || (holdSpace = spaceByKey.Values.FirstOrDefault(s => s.Name == "HOLD")) is not null)
        {
            entities.Add(ShipCad.CreatePressureVolume("Hold", [holdSpace.Id], "cargo", 101.3f));
        }

        OpeningDerivation.Apply(entities);

        var cad = new CadDocument
        {
            Name = "Calypso — Rev H (lock LOA 69)",
            Generator = new CadGenerator { Name = "CalypsoCad.Lock", Version = "2026.1.0" },
            CreatedAt = stamp,
            ModifiedAt = stamp,
            LayersDocument = "calypso.cadlayers.json",
            ShapesDocument = "calypso.cadshapejson",
            Layers =
            [
                new() { Id = LayerHull, Name = "S-HULL", Color = [0.42f, 0.48f, 0.55f] },
                new() { Id = LayerWall, Name = "A-WALL", Color = [0.45f, 0.55f, 0.7f] },
                new() { Id = LayerDoor, Name = "A-DOOR", Color = [0.7f, 0.55f, 0.3f] },
                new() { Id = LayerCorr, Name = "A-CORR", Color = [0.35f, 0.55f, 0.4f] },
                new() { Id = LayerCargo, Name = "A-ZONE-CARGO", Color = [0.3f, 0.4f, 0.32f] },
                new() { Id = LayerHab, Name = "A-ZONE-HAB", Color = [0.45f, 0.52f, 0.58f] },
                new() { Id = LayerEng, Name = "A-ZONE-ENG", Color = [0.55f, 0.38f, 0.35f] },
                new() { Id = LayerUtil, Name = "A-ZONE-UTIL", Color = [0.42f, 0.44f, 0.55f] },
                new() { Id = LayerBridge, Name = "A-ZONE-BRIDGE", Color = [0.4f, 0.42f, 0.48f] },
            ],
            Entities = entities,
            Camera = new CadCamera { Distance = 100f, Target = [0f, 6f, 0f], Yaw = 0.85f, Pitch = 0.38f },
        };

        ShipDocumentMetrics.SetShipEnvelope(cad, loa, beam, oah, DeckSpacing);
        cad.Properties!["canon"] = JsonSerializer.SerializeToElement("CAL-INT-GA-001 lock Rev H");
        cad.Properties["source"] = JsonSerializer.SerializeToElement("docs/internals/CAL-INT-GA-001.json");
        cad.Properties["crewCabinCount"] = JsonSerializer.SerializeToElement(
            lockDoc.Stations?.CrewCabinCount ?? 5);

        var spec = PlateMaterialSpec.Aisi316L_8mm;
        var mass = SkinMassRollup.FromFacetAreas(OuterSkinAreaM2, spec);
        var bom = new ShipBom
        {
            Drawing = "CAL-HULL-CAD-001",
            Rev = "B",
            Material = spec,
            SkinMass = mass,
            Lines = [SkinMassRollup.ToBomLine(mass)],
        };
        ShipStructureDocument.Attach(cad, spec, bom, mass);
        return cad;
    }

    private static string SpaceKey(string id, int deck) => $"{id}@{deck}";

    /// <summary>Lock (y starboard, up keel, z from stem) → Cad (+X stbd, +Y up, +Z bow).</summary>
    private static Vector3 LockToWorld(float y, float up, float zFromStem, float loa) =>
        new(y, up, loa * 0.5f - zFromStem);

    private static IEnumerable<(int Deck, float Up0, float Up1)> ExpandDecks(LockCompartment comp, CalypsoLockDocument lockDoc)
    {
        var decks = lockDoc.Decks;
        var roomH = decks is null ? 3.2f : (float)decks.RoomH;
        var raw = comp.Deck;
        if (raw is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number)
            {
                var d = je.GetInt32();
                yield return (d, (float)comp.Up0, (float)comp.Up1);
                yield break;
            }

            var s = je.GetString() ?? "";
            if (string.Equals(s, "atrium", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "cargo", StringComparison.OrdinalIgnoreCase))
            {
                yield return (0, (float)comp.Up0, (float)comp.Up1);
                yield break;
            }

            if (string.Equals(s, "all", StringComparison.OrdinalIgnoreCase))
            {
                // Replicate clear height on −1 / 0 / +1 using lock deck floors.
                var floors = new (int d, float y)[]
                {
                    (-1, decks is null ? 0.5f : (float)decks.M1),
                    (0, decks is null ? 4f : (float)decks.D0),
                    (1, decks is null ? 8f : (float)decks.D1),
                };
                foreach (var (d, y) in floors)
                    yield return (d, y, y + roomH);
                yield break;
            }
        }

        yield return (0, (float)comp.Up0, (float)comp.Up1);
    }

    private static (Guid Layer, Guid Shape) Classify(string id)
    {
        if (id.StartsWith("CORR", StringComparison.OrdinalIgnoreCase)
            || id is "CROSSING" or "ACCESS" or "STAIRS_P" or "ELEV_S")
            return (LayerCorr, ShapeCorridor);
        if (id is "BRIDGE")
            return (LayerBridge, ShapeBridge);
        if (id is "ENG")
            return (LayerEng, ShapeEng);
        if (id is "HOLD")
            return (LayerCargo, ShapeCargo);
        if (id.StartsWith("CABIN", StringComparison.OrdinalIgnoreCase)
            || id is "CREW" or "INFIRMARY" or "GALLEY" or "LOUNGE" or "STORE_P1")
            return (LayerHab, ShapeHab);
        if (id is "FUEL" or "UTILITY_M1")
            return (LayerUtil, ShapeUtil);
        return (LayerWall, ShapeLining);
    }

    private static CadEntity MakeSpace(
        string id,
        int deck,
        float up0,
        float up1,
        LockCompartment comp,
        float loa,
        Guid layer,
        Guid shape)
    {
        var y0 = (float)comp.Y0;
        var y1 = (float)comp.Y1;
        var z0 = LockToWorld(0, 0, (float)comp.Z0, loa).Z;
        var z1 = LockToWorld(0, 0, (float)comp.Z1, loa).Z;
        // z0 (stem) → larger world Z; ensure CCW footprint
        var zBow = Math.Max(z0, z1);
        var zAft = Math.Min(z0, z1);
        return new CadEntity
        {
            Kind = "space",
            Name = id,
            LayerId = layer,
            ShapeId = shape,
            Deck = deck,
            Height = up1 - up0,
            Points =
            [
                [y0, up0, zAft],
                [y1, up0, zAft],
                [y1, up0, zBow],
                [y0, up0, zBow],
            ],
            Hooks =
            [
                new CadHook
                {
                    Id = Guid.NewGuid(),
                    Tag = id,
                    Position = [(y0 + y1) * 0.5f, (up0 + up1) * 0.5f, (zBow + zAft) * 0.5f],
                },
            ],
        };
    }

    private static void AddRoomWalls(List<CadEntity> entities, CadEntity space, int deck, float floorY, float height, float loa)
    {
        if (space.Points is not { Count: >= 4 })
            return;
        for (var i = 0; i < space.Points.Count; i++)
        {
            var a = space.Points[i];
            var b = space.Points[(i + 1) % space.Points.Count];
            // Snap wall Y to floor
            a = [a[0], floorY, a[2]];
            b = [b[0], floorY, b[2]];
            entities.Add(new CadEntity
            {
                Kind = "wall",
                Name = $"w-{space.Name}-{i}",
                LayerId = LayerWall,
                Deck = deck,
                A = a,
                B = b,
                Thickness = WallThickness,
                Height = height,
                Sides = new CadWallSides
                {
                    A = new CadWallSide { ShapeId = ShapeLining },
                    B = new CadWallSide { ShapeId = ShapeHullInt },
                },
            });
        }
    }

    private static CadEntity MakeHatchWall(string id, int deck, Vector3 center, float clearW, float clearH, string? normal)
    {
        Vector3 a, b;
        var half = clearW * 0.5f;
        // Align wall along the coaming; normal hints which axis the leaf faces.
        if (normal is not null && (normal.Contains('Y', StringComparison.OrdinalIgnoreCase)))
        {
            // Wall along Z (opening faces ±Y / athwartships)
            a = center + new Vector3(0f, 0f, -half);
            b = center + new Vector3(0f, 0f, half);
        }
        else
        {
            // Wall along X (opening faces ±Z / fore-aft)
            a = center + new Vector3(-half, 0f, 0f);
            b = center + new Vector3(half, 0f, 0f);
        }

        a = new Vector3(a.X, center.Y - clearH * 0.5f, a.Z);
        b = new Vector3(b.X, center.Y - clearH * 0.5f, b.Z);
        return new CadEntity
        {
            Kind = "wall",
            Name = $"hatch-host-{id}",
            LayerId = LayerWall,
            Deck = deck,
            A = SvgCoords.ToArray(a),
            B = SvgCoords.ToArray(b),
            Thickness = WallThickness,
            Height = clearH,
            Sides = new CadWallSides
            {
                A = new CadWallSide { ShapeId = ShapeLining },
                B = new CadWallSide { ShapeId = ShapeHullExt },
            },
        };
    }

    /// <summary>
    /// Manufacturer CAD JSON uses ventral-aft-port AABB (X aft→stem, Y port→stbd, Z keel→crown with PAD).
    /// Map into Cad (+X stbd, +Y up from keel, +Z bow).
    /// </summary>
    private static bool TryBuildManufacturerHullMesh(float loa, float beam, float oah, out CadEntity mesh)
    {
        mesh = null!;
        string? path = null;
        foreach (var p in CandidateHullCadPaths())
        {
            if (File.Exists(p))
            {
                path = p;
                break;
            }
        }

        if (path is null)
            return false;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        if (!root.TryGetProperty("points", out var pointsEl) || !root.TryGetProperty("faces", out var facesEl))
            return false;

        const float pad = 0.5f;
        var byId = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        foreach (var p in pointsEl.EnumerateArray())
        {
            var id = p.GetProperty("id").GetString();
            if (id is null || id == "O")
                continue;
            var mx = p.GetProperty("x").GetSingle();
            var my = p.GetProperty("y").GetSingle();
            var mz = p.GetProperty("z").GetSingle();
            // Manufacturer → Cad
            var x = my - pad - beam * 0.5f;
            var y = mz - pad;
            var z = (mx - pad) - loa * 0.5f;
            byId[id] = new Vector3(x, y, z);
        }

        var verts = new List<float[]>();
        var inds = new List<int>();
        var indexOf = new Dictionary<(int, int, int), int>();

        int AddVert(Vector3 v)
        {
            var key = (
                (int)MathF.Round(v.X * 1000f),
                (int)MathF.Round(v.Y * 1000f),
                (int)MathF.Round(v.Z * 1000f));
            if (indexOf.TryGetValue(key, out var existing))
                return existing;
            var i = verts.Count;
            indexOf[key] = i;
            verts.Add([v.X, v.Y, v.Z]);
            return i;
        }

        foreach (var face in facesEl.EnumerateArray())
        {
            if (!face.TryGetProperty("shell", out var shell) ||
                !string.Equals(shell.GetString(), "OML", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!face.TryGetProperty("verts", out var vids))
                continue;
            var ids = vids.EnumerateArray().Select(e => e.GetString()).Where(s => s is not null).Cast<string>().ToList();
            if (ids.Count < 3)
                continue;
            var poly = new List<Vector3>();
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var v))
                {
                    poly.Clear();
                    break;
                }

                poly.Add(v);
            }

            if (poly.Count < 3)
                continue;

            // Fan triangulate CCW as stored
            var i0 = AddVert(poly[0]);
            for (var i = 1; i + 1 < poly.Count; i++)
            {
                inds.Add(i0);
                inds.Add(AddVert(poly[i]));
                inds.Add(AddVert(poly[i + 1]));
            }
        }

        if (inds.Count < 3)
            return false;

        mesh = new CadEntity
        {
            Kind = "mesh",
            Name = "ext-oml-hull",
            LayerId = LayerHull,
            ShapeId = ShapeHullExt,
            Color = [0.42f, 0.50f, 0.58f],
            MeshVertices = verts,
            MeshIndices = inds,
            Properties = new Dictionary<string, JsonElement>
            {
                [ShipPropertyKeys.Exterior] = JsonSerializer.SerializeToElement(true),
                ["source"] = JsonSerializer.SerializeToElement("CAL-HULL-CAD-001.json OML faces"),
                ["triangleCount"] = JsonSerializer.SerializeToElement(inds.Count / 3),
            },
        };
        return true;
    }

    private static IEnumerable<string> CandidateHullCadPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "lock", "CAL-HULL-CAD-001.json");
        var proj = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        yield return Path.Combine(proj, "docs", "manufacturer", "CAL-HULL-CAD-001.json");
        yield return Path.Combine(
            @"d:\novolis\novolis-dogfooding\apps\cad\CalypsoCad\docs\manufacturer",
            "CAL-HULL-CAD-001.json");
    }

    /// <summary>Fallback stepped AABB silhouette when manufacturer JSON is missing.</summary>
    private static List<CadEntity> BuildSteppedHullBoxes(float loa, float beam, float oah)
    {
        // Fore stations from lock (z from stem → half extents).
        (float z0, float z1, float hb, float hh)[] bays =
        [
            (0f, 3.25f, 1.75f, 2f),
            (3.25f, 10f, 5f, 3.75f),
            (10f, 17f, 8.5f, 5.25f),
            (17f, loa - 4f, beam * 0.5f, oah * 0.5f),
            (loa - 4f, loa, beam * 0.5f, oah * 0.5f),
        ];
        var list = new List<CadEntity>();
        for (var i = 0; i < bays.Length; i++)
        {
            var (z0, z1, hb, hh) = bays[i];
            var zMid = (LockToWorld(0, 0, z0, loa).Z + LockToWorld(0, 0, z1, loa).Z) * 0.5f;
            var depth = MathF.Abs(LockToWorld(0, 0, z0, loa).Z - LockToWorld(0, 0, z1, loa).Z) * 0.5f;
            list.Add(new CadEntity
            {
                Kind = "box",
                Name = $"ext-hull-bay-{i}",
                LayerId = LayerHull,
                ShapeId = ShapeHullExt,
                Color = [0.42f, 0.50f, 0.58f],
                Points =
                [
                    [0f, hh, zMid],
                    [hb, hh, depth],
                ],
                Properties = new Dictionary<string, JsonElement>
                {
                    [ShipPropertyKeys.Exterior] = JsonSerializer.SerializeToElement(true),
                },
            });
        }

        return list;
    }

    private static void AddExteriorDetails(List<CadEntity> entities, CalypsoLockDocument lockDoc, float loa, float beam, float oah)
    {
        // Port / stbd nacelle pods (orbit silhouette).
        foreach (var side in new[] { -1f, 1f })
        {
            entities.Add(new CadEntity
            {
                Kind = "cylinder",
                Name = side < 0 ? "nacelle-port" : "nacelle-stbd",
                LayerId = LayerHull,
                ShapeId = ShapeHullExt,
                Color = [0.38f, 0.42f, 0.48f],
                Center = [side * (beam * 0.5f + 1.2f), oah * 0.35f, -loa * 0.12f],
                Radius = 1.35f,
                Height = 9f,
                Properties = new Dictionary<string, JsonElement>
                {
                    [ShipPropertyKeys.Exterior] = JsonSerializer.SerializeToElement(true),
                },
            });
        }

        // Aft cargo door coaming (recess cue on stern face).
        var hold = lockDoc.Hold;
        var doorW = hold?.DoorW is > 0 ? (float)hold.DoorW : 14f;
        var doorH = hold?.DoorH is > 0 ? (float)hold.DoorH : 8.5f;
        var sill = hold?.Sill is >= 0 ? (float)hold.Sill : 0.25f;
        entities.Add(new CadEntity
        {
            Kind = "box",
            Name = "ext-aft-cargo-door",
            LayerId = LayerCargo,
            ShapeId = ShapeCargo,
            Color = [0.22f, 0.24f, 0.26f],
            Points =
            [
                [0f, sill + doorH * 0.5f, -loa * 0.5f + 0.15f],
                [doorW * 0.5f, doorH * 0.5f, 0.2f],
            ],
            Properties = new Dictionary<string, JsonElement>
            {
                [ShipPropertyKeys.Exterior] = JsonSerializer.SerializeToElement(true),
            },
        });

        // HILS-C40 peek (5×1×3) in hold — visible in sealed exterior pass by name.
        var c40L = 12.192f;
        var c40W = 2.438f;
        var c40H = 2.591f;
        var cell = 0.2f;
        var gridW = 5 * c40W + 4 * cell;
        var c40Fore = hold?.C40Fore is > 0 ? (float)hold.C40Fore : loa - 1f - c40L;
        var zMid = LockToWorld(0, 0, c40Fore + c40L * 0.5f, loa).Z;
        var left = -gridW * 0.5f;
        for (var col = 0; col < 5; col++)
        {
            var x = left + col * (c40W + cell) + c40W * 0.5f;
            for (var tier = 0; tier < 3; tier++)
            {
                var y = 1f + c40H * (tier + 0.5f);
                entities.Add(new CadEntity
                {
                    Kind = "box",
                    Name = $"C40-c{col}-t{tier}",
                    LayerId = LayerCargo,
                    ShapeId = ShapeCargo,
                    Color = [0.66f, 0.50f, 0.24f],
                    Points =
                    [
                        [x, y, zMid],
                        [c40W * 0.5f, c40H * 0.5f, c40L * 0.5f],
                    ],
                    Height = c40H,
                    Thickness = c40W,
                });
            }
        }

        // Exterior airlock blister boxes at shell (D3 stations).
        foreach (var side in new[] { -1f, 1f })
        {
            var z = LockToWorld(0, 0, 24.25f, loa).Z;
            entities.Add(new CadEntity
            {
                Kind = "box",
                Name = side < 0 ? "ext-airlock-blister-port" : "ext-airlock-blister-stbd",
                LayerId = LayerHull,
                ShapeId = ShapeHullExt,
                Color = [0.55f, 0.58f, 0.62f],
                Points =
                [
                    [side * (beam * 0.5f - 0.4f), 4f + 1.05f, z],
                    [0.8f, 1.05f, 1.25f],
                ],
                Properties = new Dictionary<string, JsonElement>
                {
                    [ShipPropertyKeys.Exterior] = JsonSerializer.SerializeToElement(true),
                },
            });
        }
    }

    private sealed class CalypsoLockDocument
    {
        public LockEnvelope? Envelope { get; set; }
        public LockDecks? Decks { get; set; }
        public LockHold? Hold { get; set; }
        public List<LockCompartment>? Compartments { get; set; }
        public List<LockAirlock>? Airlocks { get; set; }
        public List<LockHatch>? Hatches { get; set; }
        public LockStations? Stations { get; set; }
    }

    private sealed class LockHold
    {
        [JsonPropertyName("DOOR_W")]
        public double DoorW { get; set; }
        [JsonPropertyName("DOOR_H")]
        public double DoorH { get; set; }
        [JsonPropertyName("SILL")]
        public double Sill { get; set; }
        [JsonPropertyName("C40_FORE")]
        public double C40Fore { get; set; }
    }

    private sealed class LockEnvelope
    {
        [JsonPropertyName("LOA")]
        public double Loa { get; set; }
        [JsonPropertyName("BEAM")]
        public double Beam { get; set; }
        [JsonPropertyName("OAH")]
        public double Oah { get; set; }
    }

    private sealed class LockDecks
    {
        [JsonPropertyName("m1")]
        public double M1 { get; set; }
        [JsonPropertyName("d0")]
        public double D0 { get; set; }
        [JsonPropertyName("d1")]
        public double D1 { get; set; }
        [JsonPropertyName("roomH")]
        public double RoomH { get; set; }
    }

    private sealed class LockCompartment
    {
        public string? Id { get; set; }
        public JsonElement Deck { get; set; }
        public double Z0 { get; set; }
        public double Z1 { get; set; }
        public double Y0 { get; set; }
        public double Y1 { get; set; }
        public double Up0 { get; set; }
        public double Up1 { get; set; }
    }

    private sealed class LockAirlock
    {
        public string? Id { get; set; }
        public double Z0 { get; set; }
        public double Z1 { get; set; }
        public double Y0 { get; set; }
        public double Y1 { get; set; }
        public double Up0 { get; set; }
        public double Up1 { get; set; }
    }

    private sealed class LockHatch
    {
        public string? Id { get; set; }
        public int Deck { get; set; }
        public double ClearW { get; set; }
        public double ClearH { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Up { get; set; }
        public string? Normal { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Faces { get; set; }
    }

    private sealed class LockStations
    {
        [JsonPropertyName("CREW_CABIN_COUNT")]
        public int CrewCabinCount { get; set; }
    }
}
