import { useMemo } from "react";
import * as THREE from "three";
import type { CityPlan } from "../Types/City";

interface TerrainProps {
    plan: CityPlan;
}

/**
 * Flat terrain base covering the entire city footprint.
 *
 * Coordinate convention:
 *   X = east/west   (city space 0 → cityW)
 *   Z = north/south (city space 0 → cityD)
 *   Y = height      (terrain at Y = 0)
 *
 * Three.js PlaneGeometry is XY by default, so we rotate -90° around X to make it XZ.
 * The plane is centred at its local origin, so we translate it to the city centre.
 */
export function Terrain({ plan }: TerrainProps) {
    const [cityW, cityD] = plan.city.dimensions;

    // Derive a subtle grid of ground colours using the sector footprints.
    // We use a simple green base with coloured sector overlays rendered as
    // flat box slabs that sit exactly flush with the terrain (Y = 0) but
    // are rendered with polygonOffset so they don't z-fight.
    const sectors = plan.sectors;

    const sectorMeshes = useMemo(() => {
        return sectors.map((sector) => {
            const [sx, sz, sw, sd] = sector.bounds;
            const cx = sx + sw / 2;
            const cz = sz + sd / 2;
            const color = sectorColor(sector.type);
            return { id: sector.id, cx, cz, sw, sd, color };
        });
    }, [sectors]);

    return (
        <group>
            {/* Base ground plane — slightly below Y=0 to avoid z-fighting with sectors */}
            <mesh
                rotation={[-Math.PI / 2, 0, 0]}
                position={[cityW / 2, -0.1, cityD / 2]}
                receiveShadow={false}
            >
                <planeGeometry args={[cityW, cityD]} />
                <meshStandardMaterial color="#3a4d38" roughness={1} metalness={0} />
            </mesh>

            {/* Sector ground overlays — thin slabs just above base, with polygonOffset */}
            {sectorMeshes.map((s) => (
                <mesh
                    key={s.id}
                    position={[s.cx, 0, s.cz]}
                    rotation={[-Math.PI / 2, 0, 0]}
                >
                    <planeGeometry args={[s.sw, s.sd]} />
                    <meshStandardMaterial
                        color={s.color}
                        roughness={1}
                        metalness={0}
                        polygonOffset
                        polygonOffsetFactor={-1}
                        polygonOffsetUnits={-1}
                    />
                </mesh>
            ))}
        </group>
    );
}

function sectorColor(type: string): string {
    switch (type) {
        case "residential":  return "#5c7a55";
        case "commercial":   return "#6b7a8d";
        case "industrial":   return "#7a6b55";
        case "civic":        return "#8d7a6b";
        case "mixed":        return "#6b7a6b";
        case "green":        return "#4a7a44";
        case "energy":       return "#7a7a4a";
        case "transport":    return "#4a4a5c";
        default:             return "#5a5a5a";
    }
}
