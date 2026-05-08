import { test, expect } from '@playwright/test'
import { clearAuth, getToken, login, TEST_USERNAME, TEST_PASSWORD } from './utils/auth'

test.describe('登录流程测试', () => {
  test.beforeEach(async ({ page }) => {
    await clearAuth(page)
  })

  test('用户名+密码正确登录 -> 进入首页', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    await expect(page.locator('h1')).toContainText('TalentPilot')

    await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
    await page.getByPlaceholder('请输入密码').fill(TEST_PASSWORD)
    await page.getByRole('button', { name: '登录' }).click()

    await page.waitForURL('**/users', { timeout: 15000 })

    await expect(page).toHaveURL(/\/users/)
  })

  test('用户名+密码错误 -> 显示错误提示', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    await page.getByPlaceholder('请输入用户名').fill('wronguser')
    await page.getByPlaceholder('请输入密码').fill('wrongpassword')
    await page.getByRole('button', { name: '登录' }).click()

    await page.waitForSelector('.ant-message-error', { timeout: 5000 })

    await expect(page.locator('.ant-message')).toContainText(/失败|错误|incorrect|invalid/i)

    await expect(page).toHaveURL(/\/login/)
  })

  test('登录失败5次后锁定 -> 显示锁定倒计时', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    const wrongPassword = 'TalentPilot2026_Wrong'
    for (let i = 0; i < 5; i++) {
      await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
      await page.getByPlaceholder('请输入密码').fill(wrongPassword)
      await page.getByRole('button', { name: '登录' }).click()
      await page.waitForTimeout(500)
    }

    const lockMessage = page.locator('.ant-message').filter({ hasText: /锁定|lock|请.*秒|等待/i })
    await expect(lockMessage).toBeVisible({ timeout: 5000 })
  })

  test('JWT token 存储验证', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
    await page.getByPlaceholder('请输入密码').fill(TEST_PASSWORD)
    await page.getByRole('button', { name: '登录' }).click()

    await page.waitForURL('**/users', { timeout: 15000 })

    const token = await getToken(page)
    expect(token).toBeTruthy()
    expect(token!.length).toBeGreaterThan(20)

    await page.evaluate((t) => {
      const stored = localStorage.getItem('accessToken')
      return stored === t
    }, token!)
  })

  test('登录页面元素验证', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')

    await expect(page.locator('h1')).toContainText('TalentPilot')
    await expect(page.locator('.login-subtitle')).toContainText('人才管理系统')
    await expect(page.getByPlaceholder('请输入用户名')).toBeVisible()
    await expect(page.getByPlaceholder('请输入密码')).toBeVisible()
    await expect(page.getByRole('button', { name: '登录' })).toBeVisible()
  })

  test('未登录用户访问受保护页面重定向到登录页', async ({ page }) => {
    await clearAuth(page)
    await page.goto('/users')
    await page.waitForURL(/\/login/)

    await expect(page).toHaveURL(/\/login/)
  })
})
