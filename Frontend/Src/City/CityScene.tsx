import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { CityRenderer } from "./CityRenderer";
import type { CityPlan } from "../Types/City";

interface CitySceneProps {
    plan: CityPlan;
}

export function CityScene({ plan }: CitySceneProps) {
    return (
        <Canvas
            camera={{
                position: [500, 500, 500],
                fov: 45,
                near: 0.1,
                far: 10000,
            }}
        >
            <color attach="background" args={["#111111"]} />
            <ambientLight intensity={1.2} />
            <directionalLight position={[300, 500, 200]} intensity={2} />
            <CityRenderer plan={plan} />
            <gridHelper
                args={[Math.max(plan.city.dimensions[0], plan.city.dimensions[1]), 50]}
                position={[plan.city.dimensions[0] / 2, 0, plan.city.dimensions[1] / 2]}
            />
            <OrbitControls target={[plan.city.dimensions[0] / 2, 0, plan.city.dimensions[1] / 2]} />
        </Canvas>
    );
}
