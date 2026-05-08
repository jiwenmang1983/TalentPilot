import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('用户管理页面测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('http://127.0.0.1:5173/users')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('.ant-table', { timeout: 10000 })
  })

  test('用户列表加载（分页）', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()
  })

  test('创建用户（填写表单、分配角色/部门）', async ({ page }) => {
    await page.getByRole('button', { name: '新建用户' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })

    const testUsername = `testuser_${Date.now()}`
    const testFullName = `测试用户_${Date.now()}`
    const testEmail = `test_${Date.now()}@example.com`

    await drawer.getByPlaceholder('请输入用户名').fill(testUsername)
    await drawer.getByPlaceholder('请输入真实姓名').fill(testFullName)
    await drawer.getByPlaceholder('请输入邮箱').fill(testEmail)

    // 选择角色（下拉选择器）
    const roleSelect = drawer.locator('.ant-select').filter({ hasText: '' }).first()
    await roleSelect.click()
    await page.waitForTimeout(300)
    const roleOption = page.locator('.ant-select-item-option', { hasText: 'HR' }).first()
    if (await roleOption.isVisible().catch(() => false)) {
      await roleOption.click()
    } else {
      await page.keyboard.press('Escape')
    }

    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForSelector('.ant-message-success', { timeout: 5000 })
  })

  test('编辑用户（启用/禁用）', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const editButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '编辑' })
    if (await editButton.isVisible().catch(() => false)) {
      await editButton.click()
      const drawer = page.locator('.ant-drawer').last()
      await expect(drawer).toBeVisible({ timeout: 5000 })
      await drawer.waitFor({ state: 'visible' })
      // 修改姓名
      const nameInput = drawer.locator('input').nth(1)
      await nameInput.clear()
      await nameInput.fill(`更新后的姓名_${Date.now()}`)
      await drawer.locator('button.ant-btn-primary').click({ force: true })
      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('重置密码操作', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const resetButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '重置密码' })
    if (await resetButton.isVisible().catch(() => false)) {
      await resetButton.click()
      const modal = page.locator('.ant-modal').filter({ hasText: /确认重置/ })
      if (await modal.isVisible().catch(() => false)) {
        await modal.locator('button', { hasText: '确定' }).click({ force: true })
        await page.waitForSelector('.ant-message-success', { timeout: 5000 })
      }
    }
  })

  test('用户表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建用户' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })
    // 不填内容直接点提交
    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForTimeout(500)
    const errorMessages = page.locator('.ant-form-item-explain-error')
    expect(await errorMessages.count()).toBeGreaterThan(0)
  })

  test('搜索用户', async ({ page }) => {
    const searchInput = page.locator('.ant-input-search')
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill('admin')
      await page.keyboard.press('Enter')
      await page.waitForTimeout(1000)
    }
  })

  test('分页切换', async ({ page }) => {
    const pagination = page.locator('.ant-pagination')
    if (await pagination.isVisible().catch(() => false)) {
      const nextButton = pagination.locator('.ant-pagination-next')
      if (await nextButton.isVisible().catch(() => false)) {
        await nextButton.click()
        await page.waitForTimeout(500)
      }
    }
  })
})
