import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('用户管理页面测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('http://127.0.0.1:5173/users')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForSelector('.ant-table', { timeout: 10000 })
  })

  test('用户列表加载', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()
  })

  test('创建用户', async ({ page }) => {
    await page.getByRole('button', { name: '新建用户' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })

    const testUsername = `e2euser_${Date.now()}`
    const testEmail = `e2e_${Date.now()}@test.com`
    const testPassword = 'Test@12345'

    // 填写表单（用户名、邮箱、密码）
    await drawer.getByPlaceholder('请输入用户名').fill(testUsername)
    await drawer.getByPlaceholder('请输入邮箱').fill(testEmail)
    await drawer.getByPlaceholder('请输入密码').fill(testPassword)

    // 选择角色
    await drawer.locator('.ant-select').nth(0).click()
    await page.waitForTimeout(500)
    await page.locator('.ant-select-dropdown').locator('.ant-select-item', { hasText: 'HR' }).click()
    await page.waitForTimeout(300)

    // 提交
    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForSelector('.ant-message-success', { timeout: 8000 })
  })

  test('编辑用户', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const editButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '编辑' })
    if (await editButton.isVisible().catch(() => false)) {
      await editButton.click()
      const drawer = page.locator('.ant-drawer').last()
      await expect(drawer).toBeVisible({ timeout: 5000 })
      await drawer.waitFor({ state: 'visible' })
      // 修改变量名（用户名不可编辑，只需改邮箱）
      const emailInput = drawer.locator('input').nth(1)
      await emailInput.clear()
      await emailInput.fill(`updated_${Date.now()}@test.com`)
      await drawer.locator('button.ant-btn-primary').click({ force: true })
      await page.waitForSelector('.ant-message-success', { timeout: 8000 })
    }
  })

  test('搜索用户', async ({ page }) => {
    await page.getByPlaceholder('搜索用户名/邮箱').fill('admin')
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1000)
  })

  test('分页切换', async ({ page }) => {
    const pager = page.locator('.ant-pagination').first()
    if (await pager.isVisible().catch(() => false)) {
      const page2 = pager.locator('.ant-pagination-item', { hasText: '2' })
      if (await page2.isVisible().catch(() => false)) {
        await page2.click()
        await page.waitForTimeout(500)
      }
    }
  })

  test('用户表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建用户' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    // 不填内容直接点确定，验证必填提示
    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForTimeout(500)
    // ant-design 表单验证失败时不会提交，检查抽屉是否仍开着
    await expect(drawer).toBeVisible({ timeout: 3000 })
  })
})
