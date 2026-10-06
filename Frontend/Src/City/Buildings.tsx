import type { Sector } from "../Types/City";

interface BuildingsProps {
    sectors: Sector[];
}

export function Buildings({ sectors }: BuildingsProps) {
    return (
        <>
            {sectors
                .filter((sector) => !["green", "energy", "transport"].includes(sector.type))
                .map((sector) => (
                    <SectorBuildings key={sector.id} sector={sector} />
                ))}
        </>
    );
}

function SectorBuildings({ sector }: { sector: Sector }) {
    const [x1, z1, x2, z2] = sector.bounds;

    const width = x2 - x1;
    const depth = z2 - z1;
    const density = sector.density ?? 0.5;

    // Larger blocks = fewer, more visible buildings.
    const spacing = 35;
    const countX = Math.max(1, Math.floor((width / spacing) * density));
    const countZ = Math.max(1, Math.floor((depth / spacing) * density));
    const buildings = [];

    for (let x = 0; x < countX; x++) {
        for (let z = 0; z < countZ; z++) {
            const px = x1 + ((x + 0.5) / countX) * width;
            const pz = z1 + ((z + 0.5) / countZ) * depth;
            const minHeight = sector.building_height?.[0] ?? 4;
            const maxHeight = sector.building_height?.[1] ?? 10;
            const height = minHeight + Math.random() * (maxHeight - minHeight);
            const cellWidth = width / countX;
            const cellDepth = depth / countZ;
            const buildingWidth = cellWidth * 0.55;
            const buildingDepth = cellDepth * 0.55;

            buildings.push(
                <mesh key={`${sector.id}-${x}-${z}`} position={[px, height / 2, pz]}>
                    <boxGeometry args={[buildingWidth, height, buildingDepth]} />
                    <meshStandardMaterial color={getBuildingColor(sector.type)} />
                </mesh>,
            );
        }
    }

    return <>{buildings}</>;
}

function getBuildingColor(type: string) {
    switch (type) {
        case "residential":
            return "#c9b99a";
        case "commercial":
            return "#9da7ad";
        case "civic":
            return "#b59b7a";
        case "industrial":
            return "#777777";
        default:
            return "#aaa69b";
    }
}
