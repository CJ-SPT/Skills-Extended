// Inspect actual prerendered components without starting SPT or a web server.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { pathToFileURL } = require("node:url");
const { chromium } = require(process.env.PLAYWRIGHT_MODULE_PATH || "playwright");

(async () => {
    const directory = path.resolve(process.argv[2] || path.join(__dirname, "bin/Release/net10.0/rendered"));
    const browser = await chromium.launch({
        headless: true,
        executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH || undefined,
    });
    let checks = 0;

    try {
        for (const width of [2560, 1440, 768, 390]) {
            const page = await browser.newPage({ viewport: { width, height: 1000 } });
            for (const file of fs.readdirSync(directory).filter(file => file.endsWith(".html"))) {
                await page.goto(pathToFileURL(path.join(directory, file)).href);
                await page.locator(".se-app").waitFor();
                assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${file}: viewport overflow at ${width}`);
                const centering = await page.evaluate(() => {
                    const main = document.querySelector(".se-content").getBoundingClientRect();
                    const content = document.querySelector(".se-content > .se-editor-fields").getBoundingClientRect();
                    return Math.abs((main.left + main.right) / 2 - (content.left + content.right) / 2);
                });
                assert.ok(centering <= 1, `${file}: centered content at ${width}`);
                assert.equal(await page.locator("h1").count(), 1, `${file}: one clear page title`);
                assert.ok(await page.getByRole("button", { name: "Save changes", exact: true }).isVisible());
                assert.ok(await page.getByRole("button", { name: "Discard changes", exact: true }).isVisible());
                const unlabeled = await page.locator("input:not([type=hidden]), select").evaluateAll(elements => elements.filter(element =>
                    !element.getAttribute("aria-label") && !element.getAttribute("aria-labelledby") && !element.labels?.length
                ).map(element => element.outerHTML));
                assert.deepEqual(unlabeled, [], `${file}: fields have accessible labels`);
                await page.keyboard.press("Tab");
                assert.equal(await page.locator(":focus").textContent(), "Skip to content", `${file}: keyboard skip link is first`);

                if (["overview.html", "endurance.html", "lock-picking.html"].includes(file)) {
                    await page.screenshot({ path: path.join(directory, `${file.slice(0, -5)}-${width}.png`), fullPage: false });
                }
                checks++;
            }

            // Show the real drawer in its open CSS state; event handling is verified separately in Blazor checks.
            if (width === 390) {
                await page.evaluate(() => document.querySelector(".se-sidebar").classList.add("se-open"));
                assert.ok(await page.getByRole("navigation", { name: "Skills Extended navigation" }).isVisible());
                await page.screenshot({ path: path.join(directory, "navigation-390.png") });
            }
            await page.close();
        }
        console.log(`${checks} rendered page/viewport checks passed (layout, field labels, save controls, keyboard entry).`);
    } finally {
        await browser.close();
    }
})().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
