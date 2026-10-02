// Inspect actual prerendered components without starting SPT or a web server.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { pathToFileURL } = require("node:url");
const { chromium } = require(process.env.PLAYWRIGHT_MODULE_PATH || "playwright");

(async () => {
    const directory = path.resolve(process.argv[2] || path.join(__dirname, "bin/Release/net10.0/rendered"));
    const accessOnly = process.argv.includes("--client-editor-access");
    const browser = await chromium.launch({
        headless: true,
        executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH || undefined,
    });
    let checks = 0;

    try {
        for (const width of [2560, 1440, 768, 390]) {
            const page = await browser.newPage({ viewport: { width, height: 1000 } });
            for (const file of fs.readdirSync(directory).filter(file => file.endsWith(".html") && (!accessOnly || file === "client-editor-access.html"))) {
                await page.goto(pathToFileURL(path.join(directory, file)).href);
                await page.locator(".se-app").waitFor();
                assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${file}: viewport overflow at ${width}`);
                if (file === "client-editor-access.html") {
                    const access = page.getByRole("region", { name: "Fika client editor access" });
                    const geometry = await access.evaluate(element => {
                        const search = element.querySelector("input[type=search]").getBoundingClientRect();
                        const rows = [...element.querySelectorAll(".se-editor-access-profile")];
                        const first = rows[0].getBoundingClientRect();
                        return {
                            searchGap: first.top - search.bottom,
                            fits: rows.every(row => row.scrollWidth <= row.clientWidth + 1),
                            aligned: rows.every(row => {
                                const checkbox = row.querySelector("input").getBoundingClientRect();
                                const identity = row.querySelector("span").getBoundingClientRect();
                                return Math.abs((checkbox.top + checkbox.bottom - identity.top - identity.bottom) / 2) <= 1;
                            }),
                            minRowHeight: Math.min(...rows.map(row => row.getBoundingClientRect().height)),
                        };
                    });
                    assert.ok(geometry.searchGap >= 16, `Profile list has search spacing at ${width}`);
                    assert.ok(geometry.fits && geometry.aligned && geometry.minRowHeight >= 60, `Profile rows fit and align at ${width}`);
                    assert.equal(await access.getByRole("checkbox").count(), 3);
                    assert.equal(await access.getByRole("checkbox", { checked: true }).count(), 2);
                    await access.screenshot({ path: path.join(directory, `client-editor-access-${width}.png`) });
                    checks++;
                    continue;
                }
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
                if (file === "release-notes.html") {
                    const releases = page.getByRole("region", { name: "Release history" });
                    await releases.evaluate(element => window.scrollTo(0, element.getBoundingClientRect().top + window.scrollY - 210));
                    await page.locator(":focus").evaluate(element => element.blur());
                    await page.screenshot({ path: path.join(directory, `release-notes-${width}.png`) });
                }
                if (file === "hacking.html") {
                    const boards = page.getByRole("region", { name: /^Board difficulties/ });
                    assert.equal(await boards.locator(".se-board-card").count(), 3);
                    assert.equal(await boards.locator("input").count(), 21);
                    assert.equal(await boards.locator(".se-board-default").count(), 1);
                    assert.equal(await boards.getByRole("article", { name: "Secure", exact: true }).getByText("Default", { exact: true }).count(), 1);
                    assert.equal(await boards.locator(".se-board-card").evaluateAll(elements => elements.some(element => element.scrollWidth > element.clientWidth + 1)), false, `Board cards fit at ${width}`);
                    await boards.evaluate(element => window.scrollTo(0, element.getBoundingClientRect().top + window.scrollY - 88));
                    await page.locator(":focus").evaluate(element => element.blur());
                    await page.screenshot({ path: path.join(directory, `boards-${width}.png`) });
                }
                if (file === "signals-intelligence.html") {
                    const locations = page.getByRole("region", { name: /^Cache locations/ });
                    assert.equal(await locations.locator(".se-location-map").count(), 2);
                    assert.equal(await locations.locator(".se-location-row").count(), 24);
                    assert.equal(await locations.getByRole("region", { name: "Customs cache locations", exact: true }).locator(".se-location-row").count(), 12);
                    assert.equal(await locations.getByRole("region", { name: "Woods cache locations", exact: true }).locator(".se-location-row").count(), 12);
                    await locations.evaluate(element => window.scrollTo(0, element.getBoundingClientRect().top + window.scrollY - 88));
                    await page.locator(":focus").evaluate(element => element.blur());
                    await page.screenshot({ path: path.join(directory, `locations-${width}.png`) });
                    // Static fixtures have no circuit; reveal a real editor for layout checks only.
                    const locationEditor = locations.locator(".se-location-editor").first();
                    await locationEditor.evaluate(element => element.hidden = false);
                    await locationEditor.scrollIntoViewIfNeeded();
                    assert.equal(await locationEditor.evaluate(element => element.scrollWidth > element.clientWidth + 1), false, `Location fields fit at ${width}`);
                    assert.ok(await locationEditor.getByRole("spinbutton", { name: "X", exact: true }).isVisible());
                    await page.screenshot({ path: path.join(directory, `location-editor-${width}.png`) });
                    await locationEditor.evaluate(element => element.hidden = true);
                    const treasure = page.getByRole("region", { name: /Treasure tables/ });
                    await treasure.evaluate(element => window.scrollTo(0, element.getBoundingClientRect().top + window.scrollY - 88));
                    assert.equal(await treasure.locator(".se-reward-row").count(), 60);
                    assert.ok(await treasure.getByText("Graphics card", { exact: true }).isVisible());
                    assert.ok(await treasure.getByRole("searchbox", { name: "Search server items" }).isVisible());
                    const overflow = await treasure.locator(".se-reward-row, .se-reward-picker").evaluateAll(elements =>
                        elements.some(element => element.scrollWidth > element.clientWidth + 1));
                    assert.equal(overflow, false, `Treasure controls fit at ${width}`);
                    await page.screenshot({ path: path.join(directory, `treasure-${width}.png`), fullPage: false });
                }
                checks++;
            }

            // Show the real drawer in its open CSS state; event handling is verified separately in Blazor checks.
            if (width === 390 && !accessOnly) {
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
