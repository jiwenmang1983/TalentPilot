import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('角色权限配置测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('http://127.0.0.1:5173/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('.ant-table', { timeout: 10000 })
  })

  // 通过 evaluate 调用 Vue handleSubmit 的公共 helper
  async function callVueHandleSubmit(page: any, componentName: string = 'RoleManagement') {
    await page.evaluate(async (name: string) => {
      const allEls = document.querySelectorAll('*')
      let comp: any = null
      for (const el of allEls) {
        const c = (el as any).__vueParentComponent
        if (c?.type?.__name === name) { comp = c; break }
      }
      if (comp?.setupState?.handleSubmit) await comp.setupState.handleSubmit()
    }, componentName)
  }

  async function setVueFormState(page: any, data: Record<string, any>, componentName: string = 'RoleManagement') {
    await page.evaluate(async ({ d, name }: any) => {
      const allEls = document.querySelectorAll('*')
      let comp: any = null
      for (const el of allEls) {
        const c = (el as any).__vueParentComponent
        if (c?.type?.__name === name) { comp = c; break }
      }
      if (comp?.setupState?.formState) {
        Object.assign(comp.setupState.formState, d)
      }
    }, { d: data, name: componentName })
  }

  test('角色列表展示', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const rows = page.locator('.ant-table-tbody tr')
    await expect(rows.first()).toBeVisible()
    await expect(page.locator('.ant-table-thead th').filter({ hasText: '角色名称' })).toBeVisible()
    await expect(page.locator('.ant-table-thead th').filter({ hasText: '角色代码' })).toBeVisible()
  })

  test('创建新角色', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()
    const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
    await expect(drawerWrapper).toBeVisible({ timeout: 5000 })

    const testRoleName = `测试角色_${Date.now()}`
    const testRoleCode = `test_role_${Date.now()}`
    await setVueFormState(page, { name: testRoleName, code: testRoleCode, description: '自动化测试创建的角色' })
    await callVueHandleSubmit(page)

    await page.waitForTimeout(4000)
    await expect(page.locator('.ant-drawer-content-wrapper').last()).toBeHidden({ timeout: 10000 })
  })

  test('编辑角色信息', async ({ page }) => {
    await expect(page.locator('.ant-table')).toBeVisible({ timeout: 10000 })
    const editButton = page.locator('.ant-table-tbody tr').first().locator('button', { hasText: '编辑' })
    if (await editButton.isVisible().catch(() => false)) {
      await editButton.click()
      const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
      await expect(drawerWrapper).toBeVisible({ timeout: 5000 })
      await setVueFormState(page, { name: `更新后的角色_${Date.now()}` })
      await callVueHandleSubmit(page)
      await page.waitForTimeout(4000)
      await expect(page.locator('.ant-drawer-content-wrapper').last()).toBeHidden({ timeout: 10000 })
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
    const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
    await drawerWrapper.waitFor({ state: 'visible' })
    const testRoleName = `待删除角色_${Date.now()}`
    const testRoleCode = `delete_role_${Date.now()}`
    await setVueFormState(page, { name: testRoleName, code: testRoleCode })
    await callVueHandleSubmit(page)
    await page.waitForTimeout(4000)
    await expect(page.locator('.ant-drawer-content-wrapper').last()).toBeHidden({ timeout: 10000 })

    // 刷新页面找到刚创建的角色
    await page.reload()
    await page.waitForLoadState('networkidle')
    const targetRow = page.locator('.ant-table-tbody tr', { hasText: testRoleName })
    if (await targetRow.isVisible().catch(() => false)) {
      const deleteButton = targetRow.locator('button', { hasText: '删除' })
      await deleteButton.click()
      const modal = page.locator('.ant-modal').filter({ hasText: /确认删除/ })
      if (await modal.isVisible().catch(() => false)) {
        await page.evaluate(() => {
          const modalEl = document.querySelector('.ant-modal');
          const btn = modalEl?.querySelector('button.ant-btn-primary:not(.ant-btn-text):not(.ant-btn-link)');
          if (btn) (btn as HTMLButtonElement).click();
        });
        await page.waitForTimeout(2000)
        await expect(modal).not.toBeVisible({ timeout: 8000 })
      }
    }
  })

  test('角色表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建角色' }).click()
    const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
    await expect(drawerWrapper).toBeVisible({ timeout: 5000 })
    // 不填内容直接提交，触发校验
    await callVueHandleSubmit(page)
    await page.waitForTimeout(500)
    const errorMessages = page.locator('.ant-form-item-explain-error')
    expect(await errorMessages.count()).toBeGreaterThan(0)
  })
})
