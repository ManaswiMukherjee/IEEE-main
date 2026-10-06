export type Vec2 = [number, number];

export interface Sector {
    id: string;
    type: string;
    /** bounds = [x, z, width, depth] — origin + size format */
    bounds: [number, number, number, number];
    density?: number;
    building_height?: [number, number];
    population_target?: number;
}

export interface RoadNode {
    id: string;
    /** position = [x, z] in city space */
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

export interface WaterBody {
    id: string;
    /** bounds = [x, z, width, depth] */
    bounds: [number, number, number, number];
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
        water_bodies?: WaterBody[];
    };
}
