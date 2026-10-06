import type { CityPlan } from "../Types/City";
import { Buildings } from "./Buildings";
import { Roads } from "./Roads";
import { Terrain } from "./Terrain";

interface CityRendererProps {
    plan: CityPlan;
}

export function CityRenderer({ plan }: CityRendererProps) {
    return (
        <>
            <Terrain plan={plan} />
            <Roads nodes={plan.road_graph.nodes} edges={plan.road_graph.edges} />
            <Buildings sectors={plan.sectors} />
        </>
    );
}
