export type Vec2 = [number, number];

export interface Sector {
    id: string;
    type: string;
    bounds: [number, number, number, number];
    density?: number;
    building_height?: [number, number];
    population_target?: number;
}

export interface RoadNode {
    id: string;
    position: Vec2;
}

export interface RoadEdge {
    id: string;
    from: string;
    to: string;
    type: string;
    width: number;
}

export interface RoadGraph {
    nodes: RoadNode[];
    edges: RoadEdge[];
}

export interface CityPlan {
    city: {
        name: string;
        seed: number;
        dimensions: Vec2;
        population_target: number;
    };

    terrain: {
        type: string;
        height_scale: number;
    };

    sectors: Sector[];

    road_graph: RoadGraph;

    energy_zones: {
        id: string;
        type: string;
        sector: string;
        capacity: number;
        unit: string;
    }[];

    environment: {
        minimum_green_ratio: number;
    };
}
