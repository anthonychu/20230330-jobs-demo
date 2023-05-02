const fetch = require("node-fetch");
const apiBaseUrl = process.env.API_BASE_URL || "http://localhost:5024";
const customerName = process.env.CUSTOMER_NAME;

console.log(`apiBaseUrl: ${apiBaseUrl}`);
console.log(`customerName: ${customerName}`);

const steps = [
    {
        status: "pending",
        name: "Provision new environment"
    },
    {
        status: "pending",
        name: "Install services"
    },
    {
        status: "pending",
        name: "Deploy applications"
    },
    {
        status: "pending",
        name: "Import data"
    },
    {
        status: "pending",
        name: "Run diagnostics"
    },
];

async function main() {
    console.log("starting onboard.js");

    for (let i = 0; i < steps.length; i++) {
        const step = steps[i];
        console.log(`starting step ${step.name}`);
        step.status = "running";
        await updateStatus(steps);
        const randomWait = Math.floor(Math.random() * 15000);
        await sleep(randomWait);
        step.status = "success";
        console.log(`finished step ${step.name}`);
        await updateStatus(steps);
    }

    await sleep(1000);
    await updateStatus([
        {
            status: "success",
            name: "Provisioned"
        }
    ]);
    console.log("finished onboard.js");
}

main();

async function updateStatus(steps) {
    return await fetch(`${apiBaseUrl}/api/status`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            customerName,
            steps,
        })
    });
}

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}