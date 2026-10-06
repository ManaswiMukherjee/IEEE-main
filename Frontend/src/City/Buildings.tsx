import { useMemo } from "react";
import * as THREE from "three";
import type { Sector } from "../Types/City";

interface BuildingsProps {
    sectors: Sector[];
}

/**
 * Renders all buildings as a single InstancedMesh.
 *
 * Sector bounds format: [x, z, width, depth]  ← origin + size
 * Building Y placement: bottom of building at Y=0 (terrain surface)
 *                       position.y = height / 2  (centre of box)
 */
export function Buildings({ sectors }: BuildingsProps) {
    const mesh = useMemo(() => {
        // Collect all building instances first.
        type BuildingData = {
            x: number;
            z: number;
            w: number;
            d: number;
            h: number;
        };

        const instances: BuildingData[] = [];

        for (const sector of sectors) {
            if (
                sector.type === "green" ||
                sector.type === "energy" ||
                sector.type === "transport"
            ) {
                continue;
            }

            // bounds = [originX, originZ, width, depth]
            const [ox, oz, sw, sd] = sector.bounds;

            const density = sector.density ?? 0.6;
            const minH = sector.building_height?.[0] ?? 5;
            const maxH = sector.building_height?.[1] ?? 15;

            // Grid spacing based on density (denser → tighter grid)
            const spacing = sector.type === "commercial" ? 50 : 60;
            const countX = Math.max(1, Math.floor((sw * density) / spacing));
            const countZ = Math.max(1, Math.floor((sd * density) / spacing));

            for (let xi = 0; xi < countX; xi++) {
                for (let zi = 0; zi < countZ; zi++) {
                    const r = seededRandom(`${sector.id}-${xi}-${zi}`);

                    // Skip some to avoid perfectly regular grid
                    if (r < 0.15) continue;

                    const px = ox + ((xi + 0.5) / countX) * sw + (seededRandom(`${sector.id}-jx-${xi}-${zi}`) - 0.5) * (sw / countX) * 0.3;
                    const pz = oz + ((zi + 0.5) / countZ) * sd + (seededRandom(`${sector.id}-jz-${xi}-${zi}`) - 0.5) * (sd / countZ) * 0.3;

                    const h = minH + seededRandom(`${sector.id}-h-${xi}-${zi}`) * (maxH - minH);
                    const bw = (sw / countX) * 0.5 + seededRandom(`${sector.id}-bw-${xi}-${zi}`) * (sw / countX) * 0.2;
                    const bd = (sd / countZ) * 0.5 + seededRandom(`${sector.id}-bd-${xi}-${zi}`) * (sd / countZ) * 0.2;

                    instances.push({ x: px, z: pz, w: bw, d: bd, h });
                }
            }
        }

        if (instances.length === 0) return null;

        const geo = new THREE.BoxGeometry(1, 1, 1);
        const mat = new THREE.MeshStandardMaterial({
            color: "#b8a880",
            roughness: 0.85,
            metalness: 0.05,
        });
        const imesh = new THREE.InstancedMesh(geo, mat, instances.length);
        imesh.matrixAutoUpdate = false;
        imesh.castShadow = false;
        imesh.receiveShadow = false;

        const dummy = new THREE.Object3D();
        for (let i = 0; i < instances.length; i++) {
            const b = instances[i];
            // position.y = h/2 so bottom of building sits at Y=0
            dummy.position.set(b.x, b.h / 2, b.z);
            dummy.rotation.set(0, 0, 0);
            dummy.scale.set(b.w, b.h, b.d);
            dummy.updateMatrix();
            imesh.setMatrixAt(i, dummy.matrix);
        }
        imesh.instanceMatrix.needsUpdate = true;

        return imesh;
    }, [sectors]);

    if (!mesh) return null;

    return <primitive object={mesh} />;
}

/**
 * Deterministic hash-based pseudo-random number 0..1.
 * Uses a simple string hash → sin trick.
 */
function seededRandom(seed: string): number {
    let h = 0;
    for (let i = 0; i < seed.length; i++) {
        h = (Math.imul(31, h) + seed.charCodeAt(i)) | 0;
    }
    const x = Math.sin(h * 0.000001) * 43758.5453;
    return x - Math.floor(x);
}
