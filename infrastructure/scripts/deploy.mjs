#!/usr/bin/env node
// Deploys a Dokploy compose stack with specific image tags.
//
// Required env vars:
//   DOKPLOY_API_KEY     — API token from Dokploy dashboard → Settings → API
//   DOKPLOY_URL         — e.g. https://dokploy.yourvps.com
//   DOKPLOY_COMPOSE_ID  — compose ID visible in the Dokploy dashboard URL
//   IMAGE_TAG           — e.g. sha-abc1234

const { DOKPLOY_API_KEY, DOKPLOY_URL, DOKPLOY_COMPOSE_ID, IMAGE_TAG } = process.env;

if (!DOKPLOY_API_KEY) throw new Error("DOKPLOY_API_KEY is not set");
if (!DOKPLOY_URL) throw new Error("DOKPLOY_URL is not set");
if (!DOKPLOY_COMPOSE_ID) throw new Error("DOKPLOY_COMPOSE_ID is not set");
if (!IMAGE_TAG) throw new Error("IMAGE_TAG is not set");

const headers = { "x-api-key": DOKPLOY_API_KEY, "Content-Type": "application/json" };

async function post(path, body) {
    const res = await fetch(`${DOKPLOY_URL}/api/${path}`, {
        method: "POST",
        headers,
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(`POST ${path} failed: ${res.status} ${await res.text()}`);
    return res;
}

async function get(path, query) {
    const qs = new URLSearchParams(query);
    const res = await fetch(`${DOKPLOY_URL}/api/${path}?${qs}`, { headers });
    if (!res.ok) throw new Error(`GET ${path} failed: ${res.status} ${await res.text()}`);
    return res.json();
}

async function saveEnvironment() {
    const compose = await get("compose.one", { composeId: DOKPLOY_COMPOSE_ID });

    // Parse existing env, overwrite only the image vars, preserve everything else.
    const existing = Object.fromEntries(
        (compose.env ?? "")
            .split("\n")
            .filter(line => line.includes("="))
            .map(line => [line.slice(0, line.indexOf("=")), line.slice(line.indexOf("=") + 1)])
    );

    const updated = {
        ...existing,
        API_IMAGE: `ghcr.io/nielspilgaard/skoleoverblikket-api:${IMAGE_TAG}`,
        WEB_IMAGE: `ghcr.io/nielspilgaard/skoleoverblikket-web:${IMAGE_TAG}`,
        KEYCLOAK_IMAGE: `ghcr.io/nielspilgaard/skoleoverblikket-keycloak:${IMAGE_TAG}`,
    };

    const env = Object.entries(updated).map(([k, v]) => `${k}=${v}`).join("\n");

    await post("compose.saveEnvironment", { composeId: DOKPLOY_COMPOSE_ID, env });
    console.log(`Environment updated to ${IMAGE_TAG}`);
}

async function redeploy() {
    await post("compose.redeploy", {
        composeId: DOKPLOY_COMPOSE_ID,
        title: `Deploy ${IMAGE_TAG}`,
    });
    console.log("Redeploy triggered");
}

async function waitForDeployment() {
    console.log("Polling deployment status...");
    for (;;) {
        const deployments = await get("deployment.allByCompose", { composeId: DOKPLOY_COMPOSE_ID });
        const latest = deployments[0];
        console.log(`  status: ${latest.status}`);
        if (latest.status === "done") return;
        if (latest.status === "error") throw new Error(`Deployment failed: ${latest.errorMessage}`);
        if (latest.status === "cancelled") throw new Error("Deployment was cancelled");
        await new Promise(r => setTimeout(r, 5000));
    }
}

await saveEnvironment();
await redeploy();
await waitForDeployment();
console.log(`Deployed ${IMAGE_TAG} successfully`);
