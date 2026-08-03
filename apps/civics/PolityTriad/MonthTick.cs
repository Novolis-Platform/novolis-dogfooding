using Novolis.Civics.Core;
using Novolis.Civics.EconomyBridge;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Holdings;
using Novolis.Geopolitics.Core;
using Novolis.Geopolitics.Diplomacy;
using Novolis.Geopolitics.Trade;
using CivicsEngine = Novolis.Civics.Core.CivicEngine;
// TreatyEffects lives in Diplomacy
using CivicsGov = Novolis.Civics.Core.GovernmentType;
using CivicsGovRules = Novolis.Civics.Core.GovernmentRules;
using GeoCivic = Novolis.Geopolitics.Core.CivicEngine;

namespace PolityTriad;

/// <summary>
/// Composed month (intricate loop):
/// 1) fiscal agents tweak intent
/// 2) trade clearing → resource balances / shortages
/// 3) Economy periods for Alpha &amp; Beta (production, wages, tax, transfers)
/// 4) Civics from observed delivery + geo facts (control/wars/occupation)
/// 5) force growth from capability demand
/// 6) Gamma geo civic month
/// 7) conflict resolution if at war (battles / captures)
/// 8) history sample + phase label
/// </summary>
static class MonthTick
{
    public static void Advance(TriadWorld.Model model, Queue<string> log)
    {
        var world = model.World;
        var telemetry = model.Telemetry;
        model.BattlesThisMonth = 0;

        // --- 1. Policy agents (intent only) ---
        if (model.AgentsEnabled)
        {
            SyncNationTreasuryHint(model.AlphaNation, model.AlphaEconomy, TriadWorld.AlphaState);
            SyncNationTreasuryHint(model.BetaNation, model.BetaEconomy, TriadWorld.BetaState);
            var aTax0 = model.AlphaNation.Policy.HouseholdTaxRate;
            var bMil0 = model.BetaNation.Policy.MilitaryShare;
            model.FiscalAgent.AdjustPolicy(model.AlphaNation);
            model.FiscalAgent.AdjustPolicy(model.BetaNation);
            if (Math.Abs(model.AlphaNation.Policy.HouseholdTaxRate - aTax0) > 1e-9)
                Note(log, $"α agent tax {aTax0:0.00}→{model.AlphaNation.Policy.HouseholdTaxRate:0.00}");
            if (Math.Abs(model.BetaNation.Policy.MilitaryShare - bMil0) > 1e-9)
                Note(log, $"β agent mil {bMil0:0.00}→{model.BetaNation.Policy.MilitaryShare:0.00}");
        }

        // --- 2. Trade ---
        var tradeBefore = telemetry.CommonMarketVolume + telemetry.WorldMarketVolume;
        TradeClearing.RunMonth(world, telemetry);
        var tradeDelta = (telemetry.CommonMarketVolume + telemetry.WorldMarketVolume) - tradeBefore;

        // Push shortages from negative balances into next civic context via Balance (already set by trade)
        // Also starve Alpha ore slightly under war to show production bind.
        if (world.AreAtWar(new PolityId(0), new PolityId(1)))
        {
            model.AlphaEconomy = HoldingLedger.Upsert(
                model.AlphaEconomy, TriadWorld.AlphaFirm, TriadWorld.RegionA, TriadWorld.OreId,
                Math.Max(0m, HoldingLedger.GetQuantity(
                    model.AlphaEconomy, TriadWorld.AlphaFirm, TriadWorld.RegionA, TriadWorld.OreId) - 2m));
        }
        else
        {
            // Peacetime ore replenishment (extractive)
            model.AlphaEconomy = HoldingLedger.Upsert(
                model.AlphaEconomy, TriadWorld.AlphaFirm, TriadWorld.RegionA, TriadWorld.OreId,
                HoldingLedger.GetQuantity(
                    model.AlphaEconomy, TriadWorld.AlphaFirm, TriadWorld.RegionA, TriadWorld.OreId) + 5m);
            model.BetaEconomy = HoldingLedger.Upsert(
                model.BetaEconomy, TriadWorld.BetaFirm, TriadWorld.RegionB, TriadWorld.OreId,
                HoldingLedger.GetQuantity(
                    model.BetaEconomy, TriadWorld.BetaFirm, TriadWorld.RegionB, TriadWorld.OreId) + 4m);
        }

        // --- 3–5. Economy → Civics → force for Alpha & Beta ---
        model.AlphaEconomy = SettleNation(
            model, model.AlphaNation, model.AlphaEconomy,
            TriadWorld.AlphaState, world.Polity(new PolityId(0)), log, "α");
        model.BetaEconomy = SettleNation(
            model, model.BetaNation, model.BetaEconomy,
            TriadWorld.BetaState, world.Polity(new PolityId(1)), log, "β");

        // --- 6. Gamma geo-only + treaty effects ---
        var gamma = world.Polity(new PolityId(2));
        var gFacts = BuildGeoFacts(world, gamma.Id);
        GeoCivic.ApplyMonth(gamma, new GeoCivic.MonthContext
        {
            ControlRatio = gFacts.ControlRatio,
            ActiveWars = gFacts.ActiveWars,
            ResourceShortage = gFacts.ResourceShortage,
            OccupyingForeignLand = gFacts.OccupyingForeignLand,
            LostHomeProvinces = gFacts.LostHomeTerritory,
            ResearchMultiplier = world.TreatiesContaining(gamma.Id, TreatyKind.ResearchPartnership).Any() ? 1.15 : 1.0,
        });
        TreatyEffects.RunMonth(world, telemetry);

        // --- 7. Battles ---
        if (world.AreAtWar(new PolityId(0), new PolityId(1)))
        {
            foreach (var war in world.ActiveWars.Where(w =>
                         (w.Attacker.Value == 0 && w.Defender.Value == 1) ||
                         (w.Attacker.Value == 1 && w.Defender.Value == 0)).ToList())
            {
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    if (!model.Conflict.TryResolveFront(world, war))
                        continue;
                    model.BattlesThisMonth++;
                    model.CapturesTotal++;
                    telemetry.ProvincesCaptured++;
                    Note(log, $"BATTLE capture — {world.Polity(war.Attacker).Name} advances ({war.ProvincesTakenByAttacker} taken)");
                    break;
                }

                if (model.BattlesThisMonth == 0)
                    Note(log, "Front stalled — casualties only");
            }
        }

        // --- 8. Clock + history ---
        world.Day += WorldState.DaysPerMonth;
        var month = world.Day / WorldState.DaysPerMonth;
        var alpha = world.Polity(new PolityId(0));
        var beta = world.Polity(new PolityId(1));
        model.Phase = DescribePhase(world, alpha, beta);
        var aCash = (double)model.AlphaEconomy.Entities[TriadWorld.AlphaState].Cash.Amount;
        model.History.Record(
            alpha.Civic.Legitimacy, alpha.Civic.Approval, alpha.Civic.WarFatigue, aCash, alpha.Gdp,
            beta.Civic.Legitimacy, beta.Civic.WarFatigue, tradeDelta, model.BattlesThisMonth, model.Phase);

        Note(log,
            $"M{month} [{model.Phase}] tradeΔ {tradeDelta:0.#} · " +
            $"α L{alpha.Civic.Legitimacy:0.00}/A{alpha.Civic.Approval:0.00}/WF{alpha.Civic.WarFatigue:0.00}/HD{alpha.Civic.HumanDevelopment:0.00} · " +
            $"β WF{beta.Civic.WarFatigue:0.00} ctrl {Control(world, beta.Id):0.00} · γ L{gamma.Civic.Legitimacy:0.00}");
    }

    static EconomyState SettleNation(
        TriadWorld.Model model,
        NationState nation,
        EconomyState economy,
        LegalEntityId stateId,
        Polity polity,
        Queue<string> log,
        string tag)
    {
        economy = economy with
        {
            Policy = CivicEconomyBridge.ToEconomyStatePolicy(
                nation.Policy,
                transferPerHousehold: economy.Policy.TransferPerHousehold,
                wagePerLaborHour: economy.Policy.WagePerLaborHour,
                firmTaxRate: economy.Policy.FirmTaxRate),
        };

        var beforeWidgets = HoldingLedger.GetQuantity(
            economy,
            tag == "α" ? TriadWorld.AlphaFirm : TriadWorld.BetaFirm,
            tag == "α" ? TriadWorld.RegionA : TriadWorld.RegionB,
            TriadWorld.WidgetId);

        economy = model.Engine.Advance(economy);

        var afterWidgets = HoldingLedger.GetQuantity(
            economy,
            tag == "α" ? TriadWorld.AlphaFirm : TriadWorld.BetaFirm,
            tag == "α" ? TriadWorld.RegionA : TriadWorld.RegionB,
            TriadWorld.WidgetId);
        var produced = afterWidgets - beforeWidgets;

        var facts = BuildGeoFacts(model.World, polity.Id);
        // Research boost if Alpha–Gamma research treaty and this is Alpha
        if (tag == "α" && model.World.TreatiesContaining(polity.Id, TreatyKind.ResearchPartnership).Any())
        {
            facts = new PeriodContext
            {
                ControlRatio = facts.ControlRatio,
                ActiveWars = facts.ActiveWars,
                ResourceShortage = facts.ResourceShortage,
                OccupyingForeignLand = facts.OccupyingForeignLand,
                LostHomeTerritory = facts.LostHomeTerritory,
                ResearchMultiplier = 1.15,
            };
        }

        var period = CivicEconomyBridge.PeriodContextFromDelivery(
            (double)economy.Flows.TaxCollected.Amount,
            (double)economy.Flows.TransfersPaid.Amount,
            facts);

        var outcome = CivicsEngine.ApplyPeriod(nation, period);
        SyncPolityFromNation(polity, nation, economy, stateId);
        ApplyForceGrowth(polity, outcome.ForceCapabilityDemand);

        if (produced > 0 || economy.Flows.WagesAccrued.Amount > 0)
        {
            Note(log,
                $"{tag} eco: tax {economy.Flows.TaxCollected.Amount:0.#} xfer {economy.Flows.TransfersPaid.Amount:0.#} " +
                $"wages {economy.Flows.WagesAccrued.Amount:0.#} widgets {produced:0.#} force+{outcome.ForceCapabilityDemand:0.##}");
        }

        return economy;
    }

    public static void DeclareWar(TriadWorld.Model model, Queue<string> log)
    {
        var a = new PolityId(0);
        var b = new PolityId(1);
        if (model.World.AreAtWar(a, b))
        {
            Note(log, "Already at war.");
            return;
        }

        model.World.Relations.Set(a, b, -55);
        var war = DiplomaticInstruments.DeclareWar(model.World, model.Telemetry, a, b);
        Note(log, war is null ? "War refused." : "WAR — Alpha vs Beta. Ore imports collapse.");
        model.Phase = "war";
    }

    public static void OfferPeace(TriadWorld.Model model, Queue<string> log)
    {
        var a = new PolityId(0);
        var b = new PolityId(1);
        var war = model.World.ActiveWars.FirstOrDefault(w =>
            (w.Attacker == a && w.Defender == b) || (w.Attacker == b && w.Defender == a));
        if (war is null)
        {
            Note(log, "No active Alpha–Beta war.");
            return;
        }

        war.Active = false;
        DiplomaticInstruments.SignTreaty(model.World, model.Telemetry, TreatyKind.Peace, a, b, 360);
        model.World.Relations.Adjust(a, b, 15);
        Note(log, "PEACE signed — 360d grace.");
        model.Phase = "peace";
    }

    public static void SignCommonMarket(TriadWorld.Model model, Queue<string> log)
    {
        var a = new PolityId(0);
        var g = new PolityId(2);
        if (model.World.CountActiveTreatiesOfKind(TreatyKind.CommonMarket) > 0 &&
            model.World.HaveTreaty(a, g, TreatyKind.CommonMarket))
        {
            Note(log, "Alpha–Gamma Common Market already active.");
            return;
        }

        model.World.Relations.Set(a, g, Math.Max(50, model.World.Relations.Get(a, g)));
        var t = DiplomaticInstruments.SignTreaty(
            model.World, model.Telemetry, TreatyKind.CommonMarket, a, g, 2000);
        Note(log, t is null ? "CM refused." : "TREATY — Alpha–Gamma Common Market.");
    }

    public static void SignResearch(TriadWorld.Model model, Queue<string> log)
    {
        var a = new PolityId(0);
        var g = new PolityId(2);
        model.World.Relations.Set(a, g, Math.Max(55, model.World.Relations.Get(a, g)));
        var t = DiplomaticInstruments.SignTreaty(
            model.World, model.Telemetry, TreatyKind.ResearchPartnership, a, g, 1500);
        Note(log, t is null ? "Research treaty refused." : "TREATY — Research Partnership (α tech ×1.15).");
    }

    public static void RunScriptedArc(TriadWorld.Model model, Queue<string> log, int monthIndex)
    {
        // Scripted beats so headless demos show the full stack without key presses.
        switch (monthIndex)
        {
            case 2:
                SignCommonMarket(model, log);
                break;
            case 4:
                SignResearch(model, log);
                break;
            case 8:
                DeclareWar(model, log);
                // Buff Alpha for eventual capture drama
                model.World.Polity(new PolityId(0)).Military.Land += 400;
                model.World.Polity(new PolityId(0)).Military.Air += 120;
                break;
            case 18 when model.World.AreAtWar(new PolityId(0), new PolityId(1)):
                OfferPeace(model, log);
                break;
        }
    }

    static PeriodContext BuildGeoFacts(WorldState world, PolityId id)
    {
        var owned = world.CountOwnedProvinces(id);
        var home = world.Provinces.Count(p => p.HomePolityId == id);
        var polity = world.Polity(id);
        return new PeriodContext
        {
            ControlRatio = home == 0 ? 1.0 : owned / (double)home,
            ActiveWars = world.ActiveWars.Count(w => w.Attacker == id || w.Defender == id),
            ResourceShortage = ResourceKinds.All.Sum(k => polity.Balance[k] < 0 ? -polity.Balance[k] : 0),
            OccupyingForeignLand = world.Provinces.Any(p => p.OwnerId == id && p.HomePolityId != id),
            LostHomeTerritory = world.Provinces.Any(p => p.HomePolityId == id && p.OwnerId != id),
            ResearchMultiplier = 1.0,
        };
    }

    static double Control(WorldState world, PolityId id)
    {
        var home = world.Provinces.Count(p => p.HomePolityId == id);
        if (home == 0)
            return 1;
        return world.CountOwnedProvinces(id) / (double)home;
    }

    static string DescribePhase(WorldState world, Polity alpha, Polity beta)
    {
        if (world.AreAtWar(alpha.Id, beta.Id))
            return alpha.Civic.WarFatigue > 0.45 ? "war-fatigue" : "war";
        if (world.Provinces.Any(p => p.OwnerId != p.HomePolityId))
            return "occupation";
        if (world.CountActiveTreatiesOfKind(TreatyKind.CommonMarket) > 0)
            return "integration";
        return "peace";
    }

    static void SyncNationTreasuryHint(NationState nation, EconomyState economy, LegalEntityId stateId) =>
        nation.Treasury = (double)economy.Entities[stateId].Cash.Amount;

    static void SyncPolityFromNation(Polity polity, NationState nation, EconomyState economy, LegalEntityId stateId)
    {
        polity.Gdp = nation.Gdp;
        polity.Treasury = (double)economy.Entities[stateId].Cash.Amount;
        polity.Stability = nation.Stability;
        polity.TechLevel = nation.TechnologyStock;
        polity.TechProgress = nation.TechnologyProgress;
        polity.Civic.Legitimacy = nation.Civic.Legitimacy;
        polity.Civic.Approval = nation.Civic.Approval;
        polity.Civic.Corruption = nation.Civic.Corruption;
        polity.Civic.HumanDevelopment = nation.Civic.HumanDevelopment;
        polity.Civic.WarFatigue = nation.Civic.WarFatigue;
        polity.Civic.LastTaxCollected = nation.Civic.LastTaxCollected;
        polity.Civic.LastTransfersPaid = nation.Civic.LastTransfersPaid;
        polity.TaxRate = Math.Clamp(nation.Policy.HouseholdTaxRate, 0, 0.6);
        polity.MilitaryBudgetShare = Math.Clamp(nation.Policy.MilitaryShare, 0, 0.7);
        polity.Policy.HouseholdTaxRate = nation.Policy.HouseholdTaxRate;
        polity.Policy.TransferShare = nation.Policy.TransferShare;
        polity.Policy.InfrastructureShare = nation.Policy.InfrastructureShare;
        polity.Policy.PropagandaShare = nation.Policy.PropagandaShare;
        polity.Policy.MilitaryShare = nation.Policy.MilitaryShare;
    }

    static void ApplyForceGrowth(Polity polity, double demand)
    {
        var upkeep = CivicsGovRules.MilitaryUpkeepFactor((CivicsGov)(int)polity.Government);
        polity.Military.Land += demand * 0.55;
        polity.Military.Air += demand * 0.25;
        polity.Military.Naval += demand * 0.20;
        polity.Military.Scale(1.0 - 0.008 * upkeep);
    }

    static void Note(Queue<string> log, string line)
    {
        while (log.Count > 18)
            log.Dequeue();
        log.Enqueue(line);
    }
}
