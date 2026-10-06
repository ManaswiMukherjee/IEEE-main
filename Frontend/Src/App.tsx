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
                    throw new Error(`Failed to load city plan: ${response.status}`);
                }

                return response.json();
            })
            .then((data) => {
                setPlan(data);
            })
            .catch((err) => {
                setError(err.message);
            });
    }, []);

    if (error) {
        return <div>{error}</div>;
    }

    if (!plan) {
        return <div>Loading city...</div>;
    }

    return (
        <div
            style={{
                width: "100vw",
                height: "100vh",
            }}
        >
            <CityScene plan={plan} />
        </div>
    );
}

export default App;
