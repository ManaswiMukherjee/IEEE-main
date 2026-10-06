import type { CityPlan } from "../Types/City";

interface TerrainProps {
    plan: CityPlan;
}

export function Terrain({ plan }: TerrainProps) {
    const [width, depth] = plan.city.dimensions;

    return (
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[width / 2, 0, depth / 2]}>
            <planeGeometry args={[width, depth]} />
            <meshStandardMaterial color="#52634a" />
        </mesh>
    );
}
