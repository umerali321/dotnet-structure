import { chromium } from 'playwright';
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' });
await page.fill('#email', 'akearney@gwlisk.com');
await page.fill('#password input', '9490');
await page.click('button[type="submit"]');
await page.waitForURL(/select-company|dashboard/, { timeout: 15000 });
if (page.url().includes('select-company')) {
  await page.click('button:has-text("G.W. Lisk Company")');
  await page.waitForURL(/dashboard/, { timeout: 15000 });
}
await page.waitForTimeout(1000);
const html = await page.locator('aside').innerHTML();
console.log(html.slice(0, 3000));
await browser.close();
