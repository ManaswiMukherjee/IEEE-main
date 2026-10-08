// CityPlanValidator.cs
// Performs semantic validation AFTER JSON deserialization.
// Does NOT duplicate JSON Schema structural checks - those are handled by the schema itself.

using System;
using System.Collections.Generic;
using UnityEngine;

public static class CityPlanValidator
{
    public struct ValidationResult
    {
        public bool   isValid;
        public string errors;   // newline-separated list
    }

    /// <summary>
    /// Validate a deserialized CityPlan. Returns a ValidationResult.
    /// Logs warnings for every error found so you can see them all at once.
    /// Throws InvalidOperationException if critical errors prevent generation.
    /// </summary>
    public static ValidationResult Validate(CityPlan plan)
    {
        var errors = new List<string>();

        ValidateCity(plan, errors);
        ValidateTerrain(plan, errors);
        ValidateSectors(plan, errors);
        ValidateRoadGraph(plan, errors);
        ValidateBlocks(plan, errors);
        ValidateBuildings(plan, errors);
        ValidateGeneration(plan, errors);
        ValidateSustainability(plan, errors);
        ValidateCrossReferences(plan, errors);

        if (errors.Count > 0)
        {
            string combined = string.Join("\n", errors);
            foreach (string e in errors)
                Debug.LogWarning("[CityPlan] Validation: " + e);

            return new ValidationResult { isValid = false, errors = combined };
        }

        Debug.Log("[CityPlan] Validation successful.");
        return new ValidationResult { isValid = true, errors = "" };
    }

    // ─── City ─────────────────────────────────────────────────────────────────

    private static void ValidateCity(CityPlan plan, List<string> errors)
    {
        if (plan.city == null)        { errors.Add("city: object is null");           return; }
        if (string.IsNullOrEmpty(plan.city.id))   errors.Add("city: id is empty");
        if (string.IsNullOrEmpty(plan.city.name)) errors.Add("city: name is empty");

        if (plan.city.population == null)
            errors.Add("city.population: object is null");
        else if (plan.city.population.target <= 0)
            errors.Add($"city.population.target: must be > 0, got {plan.city.population.target}");

        if (plan.city.dimensions == null)
        {
            errors.Add("city.dimensions: object is null");
        }
        else
        {
            if (plan.city.dimensions.width <= 0)
                errors.Add($"city.dimensions.width: must be > 0, got {plan.city.dimensions.width}");
            if (plan.city.dimensions.depth <= 0)
                errors.Add($"city.dimensions.depth: must be > 0, got {plan.city.dimensions.depth}");
        }

        if (plan.city.location != null)
        {
            if (plan.city.location.latitude < -90 || plan.city.location.latitude > 90)
                errors.Add($"city.location.latitude: must be -90..90, got {plan.city.location.latitude}");
            if (plan.city.location.longitude < -180 || plan.city.location.longitude > 180)
                errors.Add($"city.location.longitude: must be -180..180, got {plan.city.location.longitude}");
        }
    }

    // ─── Terrain ──────────────────────────────────────────────────────────────

    private static void ValidateTerrain(CityPlan plan, List<string> errors)
    {
        if (plan.terrain == null) { errors.Add("terrain: object is null"); return; }

        var e = plan.terrain.elevation;
        if (e != null && e.minimum > e.maximum)
            errors.Add($"terrain.elevation: minimum ({e.minimum}) > maximum ({e.maximum})");

        var s = plan.terrain.slope;
        if (s != null)
        {
            if (s.minimum > s.maximum)
                errors.Add($"terrain.slope: minimum ({s.minimum}) > maximum ({s.maximum})");
            if (s.preferred_building > s.maximum_building)
                errors.Add("terrain.slope: preferred_building > maximum_building");
        }

        var r = plan.terrain.roughness;
        if (r != null && (r.level < 0f || r.level > 1f))
            errors.Add($"terrain.roughness.level: must be 0..1, got {r.level}");

        var b = plan.terrain.buildability;
        if (b != null && (b.minimum_buildable_ratio < 0f || b.minimum_buildable_ratio > 1f))
            errors.Add($"terrain.buildability.minimum_buildable_ratio: must be 0..1, got {b.minimum_buildable_ratio}");
    }

    // ─── Sectors ──────────────────────────────────────────────────────────────

    private static void ValidateSectors(CityPlan plan, List<string> errors)
    {
        if (plan.sectors == null || plan.sectors.Count == 0)
        {
            errors.Add("sectors: list is null or empty"); return;
        }

        var seenIds = new HashSet<string>();
        foreach (var sector in plan.sectors)
        {
            string ctx = $"sector '{sector.id}'";

            if (string.IsNullOrEmpty(sector.id))
            {
                errors.Add("sector: id is empty"); continue;
            }
            if (!seenIds.Add(sector.id))
                errors.Add($"{ctx}: duplicate id");

            if (string.IsNullOrEmpty(sector.name))
                errors.Add($"{ctx}: name is empty");

            // geometry
            if (sector.geometry == null)
                errors.Add($"{ctx}: geometry is null");
            else if (sector.geometry.bounds == null || sector.geometry.bounds.Length < 4)
                errors.Add($"{ctx}: geometry.bounds must be [x, z, width, depth] (4 elements)");
            else
            {
                if (sector.geometry.bounds[2] <= 0)
                    errors.Add($"{ctx}: geometry.bounds width must be > 0");
                if (sector.geometry.bounds[3] <= 0)
                    errors.Add($"{ctx}: geometry.bounds depth must be > 0");
            }

            // population
            if (sector.population != null && sector.population.target < 0)
                errors.Add($"{ctx}: population.target must be >= 0");

            // density
            if (sector.density != null)
            {
                if (sector.density.building_coverage < 0 || sector.density.building_coverage > 1)
                    errors.Add($"{ctx}: density.building_coverage must be 0..1");
            }

            // building_rules
            if (sector.building_rules != null)
            {
                var br = sector.building_rules;
                if (br.height != null && br.height.minimum_floors > br.height.maximum_floors)
                    errors.Add($"{ctx}: building_rules.height minimum_floors > maximum_floors");
                if (br.footprint != null)
                {
                    if (br.footprint.minimum_width > br.footprint.maximum_width)
                        errors.Add($"{ctx}: building_rules.footprint minimum_width > maximum_width");
                    if (br.footprint.minimum_depth > br.footprint.maximum_depth)
                        errors.Add($"{ctx}: building_rules.footprint minimum_depth > maximum_depth");
                }
                if (br.coverage != null && br.coverage.maximum < 0f || br.coverage != null && br.coverage.maximum > 1f)
                    errors.Add($"{ctx}: building_rules.coverage.maximum must be 0..1");
            }

            // street_rules
            if (sector.street_rules != null)
            {
                var sr = sector.street_rules.blocks;
                if (sr != null && sr.minimum_width > sr.maximum_width)
                    errors.Add($"{ctx}: street_rules.blocks minimum_width > maximum_width");
                if (sr != null && sr.minimum_depth > sr.maximum_depth)
                    errors.Add($"{ctx}: street_rules.blocks minimum_depth > maximum_depth");
            }
        }
    }

    // ─── Road Graph ───────────────────────────────────────────────────────────

    private static void ValidateRoadGraph(CityPlan plan, List<string> errors)
    {
        if (plan.road_graph == null) { errors.Add("road_graph: object is null"); return; }

        if (plan.road_graph.nodes == null || plan.road_graph.nodes.Count == 0)
        {
            errors.Add("road_graph.nodes: list is null or empty"); return;
        }
        if (plan.road_graph.edges == null || plan.road_graph.edges.Count == 0)
        {
            errors.Add("road_graph.edges: list is null or empty"); return;
        }

        var nodeIds = new HashSet<string>();
        foreach (var node in plan.road_graph.nodes)
        {
            if (string.IsNullOrEmpty(node.id)) { errors.Add("road_graph.nodes: node with empty id"); continue; }
            if (!nodeIds.Add(node.id))
                errors.Add($"road_graph.nodes: duplicate node id '{node.id}'");
        }

        var edgeIds = new HashSet<string>();
        foreach (var edge in plan.road_graph.edges)
        {
            if (string.IsNullOrEmpty(edge.id)) { errors.Add("road_graph.edges: edge with empty id"); continue; }
            if (!edgeIds.Add(edge.id))
                errors.Add($"road_graph.edges: duplicate edge id '{edge.id}'");

            if (!nodeIds.Contains(edge.from))
                errors.Add($"road_graph.edges: edge '{edge.id}' references unknown from node '{edge.from}'");
            if (!nodeIds.Contains(edge.to))
                errors.Add($"road_graph.edges: edge '{edge.id}' references unknown to node '{edge.to}'");

            if (edge.width <= 0)
                errors.Add($"road_graph.edges: edge '{edge.id}' width must be > 0, got {edge.width}");
        }
    }

    // ─── Blocks ───────────────────────────────────────────────────────────────

    private static void ValidateBlocks(CityPlan plan, List<string> errors)
    {
        if (plan.blocks == null) return;

        var s = plan.blocks.size;
        if (s != null)
        {
            if (s.minimum_area > s.maximum_area)
                errors.Add("blocks.size: minimum_area > maximum_area");
            if (s.minimum_width > s.maximum_width)
                errors.Add("blocks.size: minimum_width > maximum_width");
            if (s.minimum_depth > s.maximum_depth)
                errors.Add("blocks.size: minimum_depth > maximum_depth");
            if (s.preferred_area > 0 && (s.preferred_area < s.minimum_area || s.preferred_area > s.maximum_area))
                errors.Add("blocks.size: preferred_area must be within [minimum_area, maximum_area]");
        }

        var sub = plan.blocks.subdivision;
        if (sub != null)
        {
            if (sub.minimum_plot_area > sub.maximum_plot_area)
                errors.Add("blocks.subdivision: minimum_plot_area > maximum_plot_area");
            if (sub.minimum_frontage > sub.maximum_frontage)
                errors.Add("blocks.subdivision: minimum_frontage > maximum_frontage");
            if (sub.minimum_depth > sub.maximum_depth)
                errors.Add("blocks.subdivision: minimum_depth > maximum_depth");
        }

        var dev = plan.blocks.development;
        if (dev != null)
        {
            if (dev.maximum_building_coverage < 0 || dev.maximum_building_coverage > 1)
                errors.Add("blocks.development: maximum_building_coverage must be 0..1");
            if (dev.preferred_building_coverage > dev.maximum_building_coverage)
                errors.Add("blocks.development: preferred_building_coverage > maximum_building_coverage");
        }
    }

    // ─── Buildings ────────────────────────────────────────────────────────────

    private static void ValidateBuildings(CityPlan plan, List<string> errors)
    {
        if (plan.buildings == null) { errors.Add("buildings: object is null"); return; }
        if (plan.buildings.archetypes == null || plan.buildings.archetypes.Count == 0)
        {
            errors.Add("buildings.archetypes: list is null or empty"); return;
        }

        var archetypeIds = new HashSet<string>();
        foreach (var arch in plan.buildings.archetypes)
        {
            if (string.IsNullOrEmpty(arch.id)) { errors.Add("buildings.archetypes: archetype with empty id"); continue; }
            if (!archetypeIds.Add(arch.id))
                errors.Add($"buildings.archetypes: duplicate archetype id '{arch.id}'");

            if (arch.height != null && arch.height.minimum_floors > arch.height.maximum_floors)
                errors.Add($"buildings.archetypes: archetype '{arch.id}' minimum_floors > maximum_floors");

            if (arch.footprint != null)
            {
                if (arch.footprint.minimum_width > arch.footprint.maximum_width)
                    errors.Add($"buildings.archetypes: archetype '{arch.id}' minimum_width > maximum_width");
                if (arch.footprint.minimum_depth > arch.footprint.maximum_depth)
                    errors.Add($"buildings.archetypes: archetype '{arch.id}' minimum_depth > maximum_depth");
            }
        }
    }

    // ─── Generation ───────────────────────────────────────────────────────────

    private static void ValidateGeneration(CityPlan plan, List<string> errors)
    {
        if (plan.generation == null) { errors.Add("generation: object is null"); return; }
        if (plan.generation.seed < 0)
            errors.Add($"generation.seed: must be >= 0, got {plan.generation.seed}");
        if (plan.generation.randomness < 0f || plan.generation.randomness > 1f)
            errors.Add($"generation.randomness: must be 0..1, got {plan.generation.randomness}");
    }

    // ─── Sustainability ───────────────────────────────────────────────────────

    private static void ValidateSustainability(CityPlan plan, List<string> errors)
    {
        if (plan.sustainability == null) return;

        ValidateRatio("sustainability.energy.renewable_share_target",
            plan.sustainability.energy?.renewable_share_target ?? 0f, errors);
        ValidateRatio("sustainability.energy.energy_efficiency_target",
            plan.sustainability.energy?.energy_efficiency_target ?? 0f, errors);
        ValidateRatio("sustainability.transport.sustainable_mode_share",
            plan.sustainability.transport?.sustainable_mode_share ?? 0f, errors);
        ValidateRatio("sustainability.water.water_reuse_target",
            plan.sustainability.water?.water_reuse_target ?? 0f, errors);
        ValidateRatio("sustainability.land.green_space_target",
            plan.sustainability.land?.green_space_target ?? 0f, errors);
    }

    // ─── Cross-references ─────────────────────────────────────────────────────

    private static void ValidateCrossReferences(CityPlan plan, List<string> errors)
    {
        if (plan.buildings == null || plan.sectors == null) return;

        var archetypeIds = new HashSet<string>();
        foreach (var arch in plan.buildings.archetypes)
            archetypeIds.Add(arch.id);

        foreach (var sector in plan.sectors)
        {
            if (sector.building_rules?.allowed_archetypes == null) continue;
            foreach (var archId in sector.building_rules.allowed_archetypes)
            {
                if (!string.IsNullOrEmpty(archId) && !archetypeIds.Contains(archId))
                    errors.Add(
                        $"sector '{sector.id}': building_rules.allowed_archetypes references " +
                        $"unknown archetype '{archId}'");
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void ValidateRatio(string name, float value, List<string> errors)
    {
        if (value < 0f || value > 1f)
            errors.Add($"{name}: must be 0..1, got {value}");
    }
}
