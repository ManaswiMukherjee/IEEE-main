import { useMemo } from "react";
import * as THREE from "three";
import type { Sector } from "../Types/City";

interface VegetationProps {
    sectors: Sector[];
    seed: number;
}

/**
 * Renders trees in green zones as two InstancedMeshes (trunk + foliage).
 *
 * Sector bounds format: [x, z, width, depth]  ← origin + size
 * Trees are placed at Y=0 (terrain surface).
 *
 * Uses primitive objects built in useMemo to avoid the ref-null InstancedMesh bug.
 */
export function Vegetation({ sectors, seed }: VegetationProps) {
    const { trunkMesh, foliageMesh } = useMemo(() => {
        type TreeData = {
            x: number;
            z: number;
            scale: number;
            rotation: number;
        };

        const trees: TreeData[] = [];

        for (const sector of sectors) {
            if (sector.type !== "green") continue;

            // bounds = [originX, originZ, width, depth]
            const [ox, oz, sw, sd] = sector.bounds;

            const area = sw * sd;
            // Keep count reasonable — max 300 per sector
            const count = Math.min(300, Math.max(20, Math.floor(area / 6000)));

            for (let i = 0; i < count; i++) {
                const rx = seededRandom(`${seed}-${sector.id}-rx-${i}`);
                const rz = seededRandom(`${seed}-${sector.id}-rz-${i}`);
                const rs = seededRandom(`${seed}-${sector.id}-rs-${i}`);
                const rr = seededRandom(`${seed}-${sector.id}-rr-${i}`);

                trees.push({
                    x: ox + rx * sw,
                    z: oz + rz * sd,
                    scale: 0.6 + rs * 0.8,
                    rotation: rr * Math.PI * 2,
                });
            }
        }

        if (trees.length === 0) {
            return { trunkMesh: null, foliageMesh: null };
        }

        // --- Trunk InstancedMesh ---
        const trunkGeo = new THREE.CylinderGeometry(0.35, 0.5, 5, 6);
        const trunkMat = new THREE.MeshStandardMaterial({ color: "#5a4432", roughness: 1 });
        const trunk = new THREE.InstancedMesh(trunkGeo, trunkMat, trees.length);
        trunk.matrixAutoUpdate = false;

        // --- Foliage InstancedMesh ---
        const foliageGeo = new THREE.SphereGeometry(3, 7, 5);
        const foliageMat = new THREE.MeshStandardMaterial({ color: "#4a7a44", roughness: 1 });
        const foliage = new THREE.InstancedMesh(foliageGeo, foliageMat, trees.length);
        foliage.matrixAutoUpdate = false;

        const dummy = new THREE.Object3D();
        const yAxis = new THREE.Vector3(0, 1, 0);

        for (let i = 0; i < trees.length; i++) {
            const t = trees[i];
            const s = t.scale;

            // Trunk — centre at Y = 2.5 * s (bottom at Y=0, top at Y=5*s)
            dummy.position.set(t.x, 2.5 * s, t.z);
            dummy.quaternion.setFromAxisAngle(yAxis, t.rotation);
            dummy.scale.setScalar(s);
            dummy.updateMatrix();
            trunk.setMatrixAt(i, dummy.matrix);

            // Foliage — sphere centre at Y = 5*s + 2*s = 7*s
            dummy.position.set(t.x, 7 * s, t.z);
            dummy.updateMatrix();
            foliage.setMatrixAt(i, dummy.matrix);
        }

        trunk.instanceMatrix.needsUpdate = true;
        foliage.instanceMatrix.needsUpdate = true;

        return { trunkMesh: trunk, foliageMesh: foliage };
    }, [sectors, seed]);

    return (
        <>
            {trunkMesh && <primitive object={trunkMesh} />}
            {foliageMesh && <primitive object={foliageMesh} />}
        </>
    );
}

function seededRandom(seed: string): number {
    let h = 0;
    for (let i = 0; i < seed.length; i++) {
        h = (Math.imul(31, h) + seed.charCodeAt(i)) | 0;
    }
    const x = Math.sin(h * 0.000001) * 43758.5453;
    return x - Math.floor(x);
}
