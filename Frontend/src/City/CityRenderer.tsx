import type { CityPlan } from "../Types/City";
import { Terrain } from "./Terrain";
import { Roads } from "./Roads";
import { Buildings } from "./Buildings";
import { Vegetation } from "./Vegetation";
import { WaterBodies } from "./WaterBodies";

interface CityRendererProps {
    plan: CityPlan;
}

export function CityRenderer({ plan }: CityRendererProps) {
    return (
        <group>
            {/* Terrain must be first so depth buffer is correct */}
            <Terrain plan={plan} />

            {/* Water bodies sit just above terrain */}
            <WaterBodies waterBodies={plan.environment.water_bodies ?? []} />

            {/* Roads at Y=0.15 to avoid z-fighting with terrain */}
            <Roads nodes={plan.road_graph.nodes} edges={plan.road_graph.edges} />

            {/* Buildings stand on terrain */}
            <Buildings sectors={plan.sectors} />

            {/* Vegetation in green zones */}
            <Vegetation sectors={plan.sectors} seed={plan.city.seed} />
        </group>
    );
}
