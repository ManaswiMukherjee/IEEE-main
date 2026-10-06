import { useMemo } from "react";
import * as THREE from "three";
import type { WaterBody } from "../Types/City";

interface WaterBodiesProps {
    waterBodies: WaterBody[];
}

/**
 * Renders water bodies as flat blue planes just above terrain (Y = 0.05).
 * Uses polygonOffset to avoid z-fighting with the base terrain.
 */
export function WaterBodies({ waterBodies }: WaterBodiesProps) {
    const meshes = useMemo(() => {
        return waterBodies.map((wb) => {
            const [ox, oz, sw, sd] = wb.bounds;
            return {
                id: wb.id,
                cx: ox + sw / 2,
                cz: oz + sd / 2,
                w: sw,
                d: sd,
            };
        });
    }, [waterBodies]);

    if (meshes.length === 0) return null;

    return (
        <group>
            {meshes.map((m) => (
                <mesh
                    key={m.id}
                    position={[m.cx, 0.05, m.cz]}
                    rotation={[-Math.PI / 2, 0, 0]}
                >
                    <planeGeometry args={[m.w, m.d]} />
                    <meshStandardMaterial
                        color="#3a6b8a"
                        roughness={0.1}
                        metalness={0.2}
                        transparent
                        opacity={0.85}
                        polygonOffset
                        polygonOffsetFactor={-2}
                        polygonOffsetUnits={-2}
                        side={THREE.DoubleSide}
                    />
                </mesh>
            ))}
        </group>
    );
}
