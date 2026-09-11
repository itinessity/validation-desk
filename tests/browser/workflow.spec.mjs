import { test, expect } from '@playwright/test';
test.beforeEach(async ({page})=>{
 await page.route('https://fonts.googleapis.com/**',r=>r.fulfill({contentType:'text/css',body:''}));
 await page.route('https://tile.openstreetmap.org/**',r=>r.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#1d3440"/></svg>'}));
});
test('complete the main workflow',async({page})=>{
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('/');

await page.locator('#vat').fill('123456789');await page.locator('#iban').fill('DE89370400440532013000');await page.locator('#check').click();
await expect(page.locator('#result')).toContainText('confirms VAT registration');
await expect(page.locator('#result')).toContainText('checksum pass');
await page.locator('#vat').fill('999999999');await page.locator('#check').click();
await expect(page.locator('#result')).toContainText('provider_unavailable');
await expect(page.locator('#history button')).not.toHaveCount(0);

 expect(errors).toEqual([]);
});
test('mobile layout stays within the viewport',async({page})=>{
 await page.setViewportSize({width:390,height:844});await page.goto('/');
 await expect(page.locator('h1')).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBeTruthy();
});
