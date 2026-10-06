import { Line } from "@react-three/drei";

interface RoadNode {
    id: string;
    position: [number, number];
}

interface RoadEdge {
    id: string;
    from: string;
    to: string;
    type: string;
    width: number;
}

interface RoadsProps {
    nodes: RoadNode[];
    edges: RoadEdge[];
}

export function Roads({ nodes, edges }: RoadsProps) {
    const nodeMap = new Map(nodes.map((node) => [node.id, node.position]));

    return (
        <>
            {edges.map((edge) => {
                const from = nodeMap.get(edge.from);
                const to = nodeMap.get(edge.to);

                if (!from || !to) return null;

                return (
                    <Line
                        key={edge.id}
                        points={[
                            [from[0], 0.5, from[1]],
                            [to[0], 0.5, to[1]],
                        ]}
                        color="#292929"
                        lineWidth={edge.width / 4}
                    />
                );
            })}
        </>
    );
}
