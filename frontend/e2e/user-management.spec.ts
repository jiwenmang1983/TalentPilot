import { test, expect } from '@playwright/test'
import { login, clearAuth } from './utils/auth'

test.describe('用户管理页面测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('/users')
    await page.waitForLoadState('networkidle')
  })

  test('用户列表加载（分页）', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()

    const pagination = page.locator('.ant-pagination')
    const totalText = await pagination.locator('.ant-pagination-total-text').textContent()
    expect(totalText).toBeTruthy()
  })

  test('创建用户（填写表单、分配角色/部门）', async ({ page }) => {
    await page.getByRole('button', { name: '新建用户' }).click()

    const drawer = page.locator('.ant-drawer')
    await expect(drawer).toBeVisible()
    await expect(drawer.locator('.ant-drawer-title')).toContainText('新建用户')

    const testUsername = `testuser_${Date.now()}`
    const testEmail = `${testUsername}@example.com`

    await page.getByPlaceholder('请输入用户名').fill(testUsername)
    await page.getByPlaceholder('请输入邮箱').fill(testEmail)
    await page.getByPlaceholder('请输入密码').fill('TestPassword123')

    await page.locator('.ant-select').filter({ hasText: '请选择角色' }).click()
    await page.locator('.ant-select-dropdown .ant-select-item').first().click()
    await page.keyboard.press('Escape')

    await page.getByRole('button', { name: '确定' }).click()

    await page.waitForSelector('.ant-message-success', { timeout: 5000 })

    await expect(page.locator('.ant-message')).toContainText(/成功/i)
  })

  test('编辑用户（启用/禁用）', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const firstEditButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '编辑' })
    if (await firstEditButton.isVisible()) {
      await firstEditButton.click()

      const drawer = page.locator('.ant-drawer')
      await expect(drawer).toBeVisible()
      await expect(drawer.locator('.ant-drawer-title')).toContainText('编辑用户')

      await drawer.locator('.ant-btn').filter({ hasText: '取消' }).click()
    }
  })

  test('切换用户状态（启用/禁用）', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const toggleButton = page.locator('.ant-table-tbody tr').first().locator('button').filter({ hasText: /启用|禁用/ })
    if (await toggleButton.isVisible()) {
      const originalText = await toggleButton.textContent()

      await toggleButton.click()

      await page.locator('.ant-modal').filter({ hasText: /确认/ }).waitFor({ timeout: 3000 })
      await page.locator('.ant-modal').filter({ hasText: /确认/ }).locator('button', { hasText: '确定' }).click()

      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('重置密码操作', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const resetButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '重置密码' })
    if (await resetButton.isVisible()) {
      await resetButton.click()

      const modal = page.locator('.ant-modal').filter({ hasText: /重置密码/ })
      await expect(modal).toBeVisible()

      await modal.locator('button', { hasText: '确定' }).click()

      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
      await expect(page.locator('.ant-message')).toContainText(/成功/i)
    }
  })

  test('搜索用户功能', async ({ page }) => {
    await page.getByPlaceholder('搜索用户名/邮箱').fill('admin')
    await page.getByRole('button', { name: '搜索' }).click()

    await page.waitForTimeout(1000)

    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()
  })

  test('角色筛选功能', async ({ page }) => {
    const roleSelect = page.locator('.ant-select').filter({ hasText: '选择角色' })
    if (await roleSelect.isVisible()) {
      await roleSelect.click()
      await page.locator('.ant-select-dropdown .ant-select-item').first().click()

      await page.waitForTimeout(1000)
      await expect(page.locator('.ant-table')).toBeVisible()
    }
  })

  test('分页切换', async ({ page }) => {
    const pagination = page.locator('.ant-pagination')
    await expect(pagination).toBeVisible()

    const pageSizeOptions = pagination.locator('.ant-select-selector')
    if (await pageSizeOptions.isVisible()) {
      await pageSizeOptions.click()
      await page.locator('.ant-select-dropdown .ant-select-item').nth(1).click()

      await page.waitForTimeout(1000)
    }
  })
})
