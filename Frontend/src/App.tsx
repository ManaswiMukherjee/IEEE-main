import { useEffect, useState } from "react";
import { CityScene } from "./City/CityScene";
import type { CityPlan } from "./Types/City";

function App() {
    const [plan, setPlan] = useState<CityPlan | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        fetch("/Plans/TestCity.json")
            .then((response) => {
                if (!response.ok) {
                    throw new Error(`Failed to load city plan: HTTP ${response.status}`);
                }
                return response.json();
            })
            .then((data: CityPlan) => {
                setPlan(data);
            })
            .catch((err: unknown) => {
                setError(err instanceof Error ? err.message : String(err));
            });
    }, []);

    if (error) {
        return (
            <div
                style={{
                    width: "100vw",
                    height: "100vh",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    background: "#101312",
                    color: "#ff6b6b",
                    fontFamily: "monospace",
                    fontSize: 16,
                    padding: 24,
                }}
            >
                ⚠ {error}
            </div>
        );
    }

    if (!plan) {
        return (
            <div
                style={{
                    width: "100vw",
                    height: "100vh",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    background: "#101312",
                    color: "#7a9e7e",
                    fontFamily: "monospace",
                    fontSize: 16,
                }}
            >
                Loading city plan…
            </div>
        );
    }

    return (
        <div style={{ width: "100vw", height: "100vh" }}>
            <CityScene plan={plan} />
        </div>
    );
}

export default App;
