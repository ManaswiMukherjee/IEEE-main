import { useMemo } from "react";
import * as THREE from "three";
import type { RoadNode, RoadEdge } from "../Types/City";

interface RoadsProps {
    nodes: RoadNode[];
    edges: RoadEdge[];
}

/**
 * Renders all roads as instanced box geometry.
 *
 * Road nodes have position = [x, z] in city space.
 * Roads are placed at Y = 0.15 to sit clearly above the terrain (Y = 0).
 */
export function Roads({ nodes, edges }: RoadsProps) {
    // Group edges by type so we can use one InstancedMesh per road type.
    const groups = useMemo(() => {
        const nodeMap = new Map<string, [number, number]>(
            nodes.map((n) => [n.id, n.position])
        );

        type RoadGroup = {
            color: string;
            height: number;
            segments: { cx: number; cz: number; length: number; angle: number; width: number }[];
        };

        const map = new Map<string, RoadGroup>();

        for (const edge of edges) {
            const from = nodeMap.get(edge.from);
            const to = nodeMap.get(edge.to);
            if (!from || !to) continue;

            const dx = to[0] - from[0];
            const dz = to[1] - from[1];
            const length = Math.sqrt(dx * dx + dz * dz);
            if (length < 0.001) continue;

            const angle = Math.atan2(dx, dz); // rotation around Y axis

            const cx = (from[0] + to[0]) / 2;
            const cz = (from[1] + to[1]) / 2;

            const color = getRoadColor(edge.type);
            const h = getRoadHeight(edge.type);

            if (!map.has(edge.type)) {
                map.set(edge.type, { color, height: h, segments: [] });
            }
            map.get(edge.type)!.segments.push({ cx, cz, length, angle, width: edge.width });
        }

        return Array.from(map.values());
    }, [nodes, edges]);

    return (
        <group>
            {groups.map((g, gi) => (
                <RoadLayer key={gi} group={g} />
            ))}
        </group>
    );
}

interface RoadGroupData {
    color: string;
    height: number;
    segments: { cx: number; cz: number; length: number; angle: number; width: number }[];
}

function RoadLayer({ group }: { group: RoadGroupData }) {
    const { segments, color, height } = group;
    const count = segments.length;

    const meshRef = useMemo(() => {
        const geo = new THREE.BoxGeometry(1, 1, 1);
        const mat = new THREE.MeshStandardMaterial({ color, roughness: 0.9, metalness: 0 });
        const mesh = new THREE.InstancedMesh(geo, mat, count);
        mesh.matrixAutoUpdate = false;

        const dummy = new THREE.Object3D();
        for (let i = 0; i < count; i++) {
            const s = segments[i];
            dummy.position.set(s.cx, height / 2, s.cz);
            dummy.rotation.set(0, s.angle, 0);
            dummy.scale.set(s.width, height, s.length);
            dummy.updateMatrix();
            mesh.setMatrixAt(i, dummy.matrix);
        }
        mesh.instanceMatrix.needsUpdate = true;
        return mesh;
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return <primitive object={meshRef} />;
}

function getRoadColor(type: string): string {
    switch (type) {
        case "highway":    return "#242424";
        case "arterial":   return "#333333";
        case "collector":  return "#3d3d3d";
        case "local":      return "#474747";
        case "pedestrian": return "#7a7260";
        case "cycle":      return "#4e6448";
        default:           return "#3a3a3a";
    }
}

function getRoadHeight(type: string): number {
    // All roads sit at Y = 0 → top surface at Y = height.
    // We want the top surface at Y = 0.15 above terrain, so height = 0.15 + small slab.
    switch (type) {
        case "highway":    return 0.25;
        case "arterial":   return 0.22;
        case "collector":  return 0.20;
        case "local":      return 0.18;
        case "pedestrian": return 0.15;
        case "cycle":      return 0.15;
        default:           return 0.18;
    }
}
