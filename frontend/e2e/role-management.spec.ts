import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('角色权限配置测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('/roles')
    await page.waitForLoadState('domcontentloaded')
  })

  test('角色列表展示', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()

    await expect(page.locator('.ant-table-thead th').filter({ hasText: '角色名称' })).toBeVisible()
    await expect(page.locator('.ant-table-thead th').filter({ hasText: '角色代码' })).toBeVisible()
    await expect(page.locator('.ant-table-thead th').filter({ hasText: '类型' })).toBeVisible()
  })

  test('创建新角色', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()

    const drawer = page.locator('.ant-drawer')
    await expect(drawer).toBeVisible()
    await expect(drawer.locator('.ant-drawer-title')).toContainText('新建角色')

    const testRoleName = `测试角色_${Date.now()}`
    const testRoleCode = `test_role_${Date.now()}`

    await page.getByPlaceholder('请输入角色名称').fill(testRoleName)
    await page.getByPlaceholder('请输入角色代码').fill(testRoleCode)
    await page.getByPlaceholder('请输入描述').fill('自动化测试创建的角色')

    await page.getByRole('button', { name: '确定' }).click()

    await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    await expect(page.locator('.ant-message')).toContainText(/成功/i)
  })

  test('编辑角色信息', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const editButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '编辑' })
    if (await editButton.isVisible()) {
      await editButton.click()

      const drawer = page.locator('.ant-drawer')
      await expect(drawer).toBeVisible()
      await expect(drawer.locator('.ant-drawer-title')).toContainText('编辑角色')

      const nameInput = page.getByPlaceholder('请输入角色名称')
      await nameInput.clear()
      await nameInput.fill(`更新后的角色_${Date.now()}`)

      await drawer.getByRole('button', { name: '确定' }).click()

      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('角色权限树勾选配置', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const expandButton = page.locator('.ant-table-tbody tr').first().locator('.ant-table-row-expand-icon')
    if (await expandButton.isVisible()) {
      await expandButton.click()

      await page.waitForTimeout(500)

      const permissionTree = page.locator('.permission-config .ant-tree')
      await expect(permissionTree).toBeVisible({ timeout: 5000 })

      const checkboxes = permissionTree.locator('.ant-tree-checkbox')
      if (await checkboxes.count() > 0) {
        await checkboxes.first().click()

        const saveButton = page.locator('.permission-header button', { hasText: '保存权限' })
        if (await saveButton.isVisible()) {
          await saveButton.click()
          await page.waitForSelector('.ant-message-success', { timeout: 5000 })
        }
      }
    }
  })

  test('预置角色（Admin/HR/用人经理）不可删除验证', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })

    const rows = page.locator('.ant-table-tbody tr')

    for (let i = 0; i < await rows.count(); i++) {
      const row = rows.nth(i)
      const systemTag = row.locator('.ant-tag', { hasText: '系统角色' })

      if (await systemTag.isVisible()) {
        const deleteButton = row.locator('button', { hasText: '删除' })
        await expect(deleteButton).toBeDisabled()
      }
    }
  })

  test('删除自定义角色', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()

    const testRoleName = `待删除角色_${Date.now()}`
    const testRoleCode = `delete_role_${Date.now()}`

    await page.getByPlaceholder('请输入角色名称').fill(testRoleName)
    await page.getByPlaceholder('请输入角色代码').fill(testRoleCode)
    await page.getByRole('button', { name: '确定' }).click()

    await page.waitForSelector('.ant-message-success', { timeout: 5000 })

    await page.reload()
    await page.waitForLoadState('domcontentloaded')

    const targetRow = page.locator('.ant-table-tbody tr', { hasText: testRoleName })
    if (await targetRow.isVisible()) {
      const deleteButton = targetRow.locator('button', { hasText: '删除' })
      await deleteButton.click()

      const modal = page.locator('.ant-modal').filter({ hasText: /确认删除/ })
      await expect(modal).toBeVisible()
      await modal.locator('button', { hasText: '确定' }).click()

      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('角色表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()

    const drawer = page.locator('.ant-drawer')
    await expect(drawer).toBeVisible()

    await page.getByRole('button', { name: '确定' }).click()

    await page.waitForTimeout(500)

    const errorMessages = page.locator('.ant-form-item-explain-error')
    expect(await errorMessages.count()).toBeGreaterThan(0)
  })
})
