import { useRef, useEffect } from "react";
import { Canvas, useThree } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import type { OrbitControls as OrbitControlsImpl } from "three-stdlib";
import * as THREE from "three";
import type { CityPlan } from "../Types/City";
import { CityRenderer } from "./CityRenderer";

interface CitySceneProps {
    plan: CityPlan;
}

/**
 * CameraRig — lives *inside* the Canvas so it has access to the R3F camera.
 *
 * On first mount it:
 *  1. Computes a tight bounding box from the city plan dimensions.
 *  2. Positions the camera at an isometric-ish elevated angle.
 *  3. Sets OrbitControls target to the city centre.
 *
 * No React state is ever set during camera movement — everything goes through
 * Three.js object mutations so the renderer stays fully decoupled from React.
 */
interface CameraRigProps {
    plan: CityPlan;
    controlsRef: React.RefObject<OrbitControlsImpl | null>;
}

function CameraRig({ plan, controlsRef }: CameraRigProps) {
    const { camera, invalidate } = useThree();
    const initialised = useRef(false);

    useEffect(() => {
        if (initialised.current) return;
        initialised.current = true;

        const [cityW, cityD] = plan.city.dimensions;

        // Build a bounding box from the actual city footprint.
        // City occupies X:[0, cityW], Y:[0, maxBuildingHeight], Z:[0, cityD].
        // We compute max building height from sectors for a tighter fit.
        let maxH = 25; // safe fallback
        for (const sector of plan.sectors) {
            if (sector.building_height) {
                maxH = Math.max(maxH, sector.building_height[1]);
            }
        }

        const box = new THREE.Box3(
            new THREE.Vector3(0, 0, 0),
            new THREE.Vector3(cityW, maxH, cityD)
        );

        const center = new THREE.Vector3();
        box.getCenter(center);

        const size = new THREE.Vector3();
        box.getSize(size);

        // Diagonal of the base footprint — good proxy for "how big is the city".
        const footprintDiag = Math.sqrt(cityW * cityW + cityD * cityD);

        // Desired vertical field of view in radians.
        const fovRad = ((camera as THREE.PerspectiveCamera).fov * Math.PI) / 180;

        // Distance needed so the full footprint diagonal fills ~70% of the FOV.
        const fitDist = (footprintDiag / 2 / Math.tan(fovRad / 2)) * 1.4;

        // Isometric-ish: 45° elevation angle, offset along -Z and +Y.
        const elevationAngle = Math.PI / 4; // 45 degrees
        const horizontalDist = fitDist * Math.cos(elevationAngle);
        const verticalDist = fitDist * Math.sin(elevationAngle);

        // Position camera offset from city centre (south-east perspective).
        camera.position.set(
            center.x + horizontalDist * 0.7,
            center.y + verticalDist,
            center.z + horizontalDist * 0.7
        );

        camera.lookAt(center.x, 0, center.z);
        camera.updateProjectionMatrix();

        // Update OrbitControls target to city centre at ground level.
        if (controlsRef.current) {
            controlsRef.current.target.set(center.x, 0, center.z);
            controlsRef.current.update();
        }

        // Request one render after setup.
        invalidate();
    }, [plan, camera, controlsRef, invalidate]);

    return null;
}

export function CityScene({ plan }: CitySceneProps) {
    const [cityW, cityD] = plan.city.dimensions;
    const footprintDiag = Math.sqrt(cityW * cityW + cityD * cityD);

    // Derive a reasonable far plane from the city diagonal.
    const farPlane = footprintDiag * 4;
    const minDist = footprintDiag * 0.01;
    const maxDist = footprintDiag * 3;

    // Ref to OrbitControls instance — shared with CameraRig.
    // Using RefObject so TypeScript is happy; value is set by <OrbitControls ref=...>
    const controlsRef = useRef<OrbitControlsImpl | null>(null);

    return (
        <Canvas
            /**
             * frameloop="demand" → Three.js only renders when something changes.
             * OrbitControls calls `invalidate()` automatically when the user
             * interacts, so we get smooth interaction without spinning the render
             * loop at 60 fps when the scene is idle.
             */
            frameloop="demand"
            camera={{
                fov: 45,
                near: 1,
                far: farPlane,
                // Initial position is overridden by CameraRig once it mounts,
                // but we set a sensible fallback so the first frame is not blank.
                position: [cityW * 0.6, cityD * 0.4, cityD * 0.9],
            }}
            dpr={[1, 1.5]}
            gl={{
                antialias: true,
                powerPreference: "high-performance",
            }}
            style={{
                width: "100%",
                height: "100%",
                display: "block",
                background: "#101312",
            }}
        >
            {/* Sky / background colour */}
            <color attach="background" args={["#101312"]} />

            {/* ─── Lights ──────────────────────────────────────────────────── */}
            {/* Ambient fill — keeps shadows from going pitch black */}
            <ambientLight intensity={1.2} />

            {/* Main sun — northeast, elevated */}
            <directionalLight
                position={[cityW * 0.5 + cityW * 0.4, cityD * 0.6, cityD * 0.5 - cityD * 0.3]}
                intensity={2.5}
                castShadow={false}
            />

            {/* Soft fill from the west */}
            <directionalLight
                position={[cityW * 0.5 - cityW * 0.3, cityD * 0.3, cityD * 0.5 + cityD * 0.4]}
                intensity={0.6}
            />

            {/* ─── City geometry ───────────────────────────────────────────── */}
            <CityRenderer plan={plan} />

            {/* ─── Camera rig (positions camera from city bounds) ──────────── */}
            <CameraRig plan={plan} controlsRef={controlsRef} />

            {/* ─── Orbit controls ──────────────────────────────────────────── */}
            {/*
             * makeDefault → registers these as the default controls so R3F's
             *               invalidate() calls from useFrame hooks work correctly.
             *
             * We deliberately do NOT listen to onChange/onUpdate with React
             * callbacks to avoid triggering re-renders every animation frame.
             * OrbitControls calls invalidate() internally when damping is active.
             */}
            <OrbitControls
                ref={controlsRef}
                makeDefault
                enableDamping
                dampingFactor={0.05}
                enablePan
                enableZoom
                enableRotate
                minDistance={minDist}
                maxDistance={maxDist}
                /**
                 * Prevent camera from flipping underneath the terrain.
                 * Math.PI / 2 is directly overhead; we stop just before that
                 * so the camera never goes below the ground plane.
                 */
                maxPolarAngle={Math.PI / 2 - 0.05}
                zoomSpeed={1.0}
                panSpeed={0.8}
                rotateSpeed={0.6}
                screenSpacePanning={false}
            />
        </Canvas>
    );
}
