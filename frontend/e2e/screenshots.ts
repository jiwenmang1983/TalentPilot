import { chromium } from '@playwright/test'

const BASE = 'http://127.0.0.1:5173'
const OUT = 'e2e/screenshots'
const USER = 'admin'
const PASS = 'TalentPilot2026'

async function main() {
  const browser = await chromium.launch()
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } })
  const page = await context.newPage()

  // Login
  await page.goto(`${BASE}/login`)
  await page.waitForLoadState('domcontentloaded')
  await page.getByPlaceholder('请输入用户名').fill(USER)
  await page.getByPlaceholder('请输入密码').fill(PASS)
  await page.locator('button[type="submit"]').click()
  await page.waitForURL('**/dashboard', { timeout: 15000 })
  console.log('✓ Logged in')

  const routes = [
    ['01-login',        '/login'],
    ['02-dashboard',    '/dashboard'],
    ['03-users',        '/users'],
    ['04-roles',        '/roles'],
    ['05-departments',  '/departments'],
    ['06-candidates',   '/candidates'],
    ['07-jobs',         '/jobs'],
    ['08-interviews',   '/interviews'],
  ]

  for (const [name, path] of routes) {
    await page.goto(`${BASE}${path}`)
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(1500)
    await page.screenshot({ path: `${OUT}/${name}.png`, fullPage: false })
    console.log(`✓ ${name}`)
  }

  await browser.close()
  console.log('\nAll screenshots saved to e2e/screenshots/')
}

main().catch(e => { console.error(e.message); process.exit(1) })
