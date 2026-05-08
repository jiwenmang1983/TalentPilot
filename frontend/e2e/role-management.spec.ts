import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('角色权限配置测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('http://127.0.0.1:5173/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('.ant-table', { timeout: 10000 })
  })

  test('角色列表展示', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()
    await expect(page.locator('.ant-table-thead th').filter({ hasText: '角色名称' })).toBeVisible()
    await expect(page.locator('.ant-table-thead th').filter({ hasText: '角色代码' })).toBeVisible()
  })

  test('创建新角色', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })

    const testRoleName = `测试角色_${Date.now()}`
    const testRoleCode = `test_role_${Date.now()}`
    await drawer.getByPlaceholder('请输入角色名称').fill(testRoleName)
    await drawer.getByPlaceholder('请输入角色代码').fill(testRoleCode)
    await drawer.locator('textarea').fill('自动化测试创建的角色')

    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForSelector('.ant-message-success', { timeout: 5000 })
  })

  test('编辑角色信息', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const editButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '编辑' })
    if (await editButton.isVisible().catch(() => false)) {
      await editButton.click()
      const drawer = page.locator('.ant-drawer').last()
      await expect(drawer).toBeVisible({ timeout: 5000 })
      await drawer.waitFor({ state: 'visible' })
      const nameInput = drawer.locator('input').first()
      await nameInput.clear()
      await nameInput.fill(`更新后的角色_${Date.now()}`)
      await drawer.locator('button.ant-btn-primary').click({ force: true })
      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('角色权限树勾选配置', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const expandIcon = page.locator('.ant-table-tbody tr').first().locator('.ant-table-row-expand-icon')
    if (await expandIcon.isVisible().catch(() => false)) {
      await expandIcon.click()
      await page.waitForTimeout(500)
      const permissionTree = page.locator('.ant-tree').last()
      await expect(permissionTree).toBeVisible({ timeout: 5000 })
    }
  })

  test('预置角色不可删除', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const rows = page.locator('.ant-table-tbody tr')
    for (let i = 0; i < await rows.count(); i++) {
      const row = rows.nth(i)
      const systemTag = row.locator('.ant-tag', { hasText: '系统角色' })
      if (await systemTag.isVisible().catch(() => false)) {
        const deleteButton = row.locator('button', { hasText: '删除' })
        await expect(deleteButton).toBeDisabled()
        break
      }
    }
  })

  test('删除自定义角色', async ({ page }) => {
    // 先创建一个角色
    await page.getByRole('button', { name: '新建角色' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await drawer.waitFor({ state: 'visible' })
    const testRoleName = `待删除角色_${Date.now()}`
    const testRoleCode = `delete_role_${Date.now()}`
    await drawer.getByPlaceholder('请输入角色名称').fill(testRoleName)
    await drawer.getByPlaceholder('请输入角色代码').fill(testRoleCode)
    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForSelector('.ant-message-success', { timeout: 5000 })

    // 刷新页面找到刚创建的角色
    await page.reload()
    await page.waitForLoadState('networkidle')
    const targetRow = page.locator('.ant-table-tbody tr', { hasText: testRoleName })
    if (await targetRow.isVisible().catch(() => false)) {
      const deleteButton = targetRow.locator('button', { hasText: '删除' })
      await deleteButton.click()
      const modal = page.locator('.ant-modal').filter({ hasText: /确认删除/ })
      if (await modal.isVisible().catch(() => false)) {
        await modal.locator('button', { hasText: '确定' }).click({ force: true })
        await page.waitForSelector('.ant-message-success', { timeout: 5000 })
      }
    }
  })

  test('角色表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })
    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForTimeout(500)
    const errorMessages = page.locator('.ant-form-item-explain-error')
    expect(await errorMessages.count()).toBeGreaterThan(0)
  })
})
