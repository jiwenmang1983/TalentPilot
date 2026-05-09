import { test, expect } from '@playwright/test'
import { clearAuth, getToken, login, TEST_USERNAME, TEST_PASSWORD } from './utils/auth'

const BASE = 'http://127.0.0.1:5173'

test.describe('登录流程测试', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page)
  })

  test('用户名+密码正确登录 -> 进入首页', async ({ page }) => {
    await page.goto(`${BASE}/login`)
    await page.waitForLoadState('domcontentloaded')

    await expect(page.locator('h1')).toContainText('TalentPilot')

    await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
    await page.getByPlaceholder('请输入密码').fill(TEST_PASSWORD)
    await page.locator('button[type="submit"]').click()

    await page.waitForURL('**/dashboard', { timeout: 15000 })

    await expect(page).toHaveURL(/\/dashboard/)
  })

  test('用户名+密码错误 -> 显示错误提示', async ({ page }) => {
    await page.goto(`${BASE}/login`)
    await page.waitForLoadState('domcontentloaded')

    await page.getByPlaceholder('请输入用户名').fill('wronguser')
    await page.getByPlaceholder('请输入密码').fill('wrongpassword')
    await page.locator('button[type="submit"]').click()

    await page.waitForSelector('.ant-message-error', { timeout: 5000 })

    await expect(page.locator('.ant-message')).toContainText(/失败|错误|incorrect|invalid/i)

    await expect(page).toHaveURL(/\/login/)
  })

  test.skip('登录失败5次后锁定 -> 显示锁定倒计时', async ({ page }) => {
    // 注意：此测试需要独立测试账号，避免锁定主账号影响其他测试
    // TODO: 创建专用测试账号 test_lock@talentpilot.com 后重新启用
    await page.goto(`${BASE}/login`)
    await page.waitForLoadState('domcontentloaded')

    const wrongPassword = 'TalentPilot2026_Wrong'
    for (let i = 0; i < 5; i++) {
      await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
      await page.getByPlaceholder('请输入密码').fill(wrongPassword)
      await page.locator('button[type="submit"]').click()
      await page.waitForTimeout(500)
    }

    const lockMessage = page.locator('.ant-message').filter({ hasText: /锁定|lock|请.*秒|等待/i })
    await expect(lockMessage).toBeVisible({ timeout: 5000 })
  })

  test('JWT token 存储验证', async ({ page }) => {
    await page.goto(`${BASE}/login`)
    await page.waitForLoadState('domcontentloaded')

    await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
    await page.getByPlaceholder('请输入密码').fill(TEST_PASSWORD)
    await page.locator('button[type="submit"]').click()

    await page.waitForURL('**/dashboard', { timeout: 15000 })

    const token = await getToken(page)
    expect(token).toBeTruthy()
    expect(token!.length).toBeGreaterThan(20)

    await page.evaluate((t) => {
      const stored = localStorage.getItem('accessToken')
      return stored === t
    }, token!)
  })

  test('登录页面元素验证', async ({ page }) => {
    await page.goto(`${BASE}/login`)
    await page.waitForLoadState('domcontentloaded')

    await expect(page.locator('h1')).toContainText('TalentPilot')
    await expect(page.locator('.subtitle')).toContainText('TalentPilot')
    await expect(page.getByPlaceholder('请输入用户名')).toBeVisible()
    await expect(page.getByPlaceholder('请输入密码')).toBeVisible()
    await expect(page.locator('button[type="submit"]')).toBeVisible()
  })

  test('未登录用户访问受保护页面重定向到登录页', async ({ page }) => {
    await clearAuth(page)
    await page.goto(`${BASE}/users`)
    await page.waitForURL(/\/login/)

    await expect(page).toHaveURL(/\/login/)
  })
})
