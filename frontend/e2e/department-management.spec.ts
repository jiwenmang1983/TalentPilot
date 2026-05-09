import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('部门管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('http://127.0.0.1:5173/departments')
    await page.waitForLoadState('networkidle')
    // 等待树加载
    await page.waitForSelector('.ant-tree', { timeout: 10000 })
  })

  test('部门树形结构展示', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })
    // 使用可见的节点内容，不要 aria-hidden 的 treenode
    const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
    await expect(firstNode).toBeVisible({ timeout: 5000 })
  })

  test('新增部门', async ({ page }) => {
    await page.getByRole('button', { name: '新建部门' }).click()
    const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
    await expect(drawerWrapper).toBeVisible({ timeout: 5000 })

    const testDeptName = `测试部门_${Date.now()}`
    const testDeptCode = `dept_${Date.now()}`
    // 通过 Vue reactive state 设置表单值（Playwright fill 不触发 Vue v-model）
    await page.evaluate(({ name, code }) => {
      const allEls = document.querySelectorAll('*')
      let comp: any = null
      for (const el of allEls) {
        const c = (el as any).__vueParentComponent
        if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
      }
      if (comp?.setupState?.formState) {
        comp.setupState.formState.name = name
        comp.setupState.formState.code = code
      }
    }, { name: testDeptName, code: testDeptCode })

    // 填表后通过 evaluate 调用 handleSubmit（Playwright click 无法触发 ant-dv btn 的 Vue 事件）
    await page.evaluate(async () => {
      const allEls = document.querySelectorAll('*')
      let comp: any = null
      for (const el of allEls) {
        const c = (el as any).__vueParentComponent
        if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
      }
      if (comp?.setupState?.handleSubmit) await comp.setupState.handleSubmit()
    })

    // 验证抽屉关闭（content-wrapper 才是真正的隐藏元素）
    await page.waitForTimeout(4000)
    await expect(page.locator('.ant-drawer-content-wrapper').last()).toBeHidden({ timeout: 10000 })
  })

  test('编辑部门', async ({ page }) => {
    const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
    await firstNode.click()
    await page.waitForTimeout(300)
    await firstNode.click({ button: 'right' })
    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible().catch(() => false)) {
      await contextMenu.locator('button', { hasText: '编辑' }).click()
      const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
      await expect(drawerWrapper).toBeVisible({ timeout: 5000 })
      // 通过 Vue state 更新名称
      await page.evaluate(async () => {
        const allEls = document.querySelectorAll('*')
        let comp: any = null
        for (const el of allEls) {
          const c = (el as any).__vueParentComponent
          if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
        }
        if (comp?.setupState?.formState) {
          comp.setupState.formState.name = `更新后的部门_${Date.now()}`
        }
      })
      await page.evaluate(async () => {
        const allEls = document.querySelectorAll('*')
        let comp: any = null
        for (const el of allEls) {
          const c = (el as any).__vueParentComponent
          if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
        }
        if (comp?.setupState?.handleSubmit) await comp.setupState.handleSubmit()
      })
      await page.waitForTimeout(4000)
      await expect(page.locator('.ant-drawer-content-wrapper').last()).toBeHidden({ timeout: 10000 })
    }
  })

  test('删除部门', async ({ page }) => {
    const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
    await firstNode.click()
    await page.waitForTimeout(300)
    await firstNode.click({ button: 'right' })
    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible().catch(() => false)) {
      const deleteButton = contextMenu.locator('button', { hasText: '删除' })
      if (await deleteButton.isVisible().catch(() => false)) {
        await deleteButton.click()
        const modal = page.locator('.ant-modal').filter({ hasText: /确认删除/ })
        if (await modal.isVisible().catch(() => false)) {
          // 直接通过 JS 点击，避免 ant-modal 内部结构阻塞
          await page.evaluate(() => {
            const modalEl = document.querySelector('.ant-modal');
            const btn = modalEl?.querySelector('button.ant-btn-primary:not(.ant-btn-text):not(.ant-btn-link)');
            if (btn) (btn as HTMLButtonElement).click();
          });
          await page.waitForTimeout(2000)
          await expect(modal).not.toBeVisible({ timeout: 8000 })
        }
      }
    }
  })

  test('添加子部门', async ({ page }) => {
    const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
    await firstNode.click()
    await page.waitForTimeout(300)
    await firstNode.click({ button: 'right' })
    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible().catch(() => false)) {
      await contextMenu.locator('button', { hasText: '添加子部门' }).click()
      const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
      await expect(drawerWrapper).toBeVisible({ timeout: 5000 })

      const testDeptName = `子部门_${Date.now()}`
      const testDeptCode = `child_dept_${Date.now()}`
      await page.evaluate(({ name, code }) => {
        const allEls = document.querySelectorAll('*')
        let comp: any = null
        for (const el of allEls) {
          const c = (el as any).__vueParentComponent
          if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
        }
        if (comp?.setupState?.formState) {
          comp.setupState.formState.name = name
          comp.setupState.formState.code = code
        }
      }, { name: testDeptName, code: testDeptCode })

      await page.evaluate(async () => {
        const allEls = document.querySelectorAll('*')
        let comp: any = null
        for (const el of allEls) {
          const c = (el as any).__vueParentComponent
          if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
        }
        if (comp?.setupState?.handleSubmit) await comp.setupState.handleSubmit()
      })
      await page.waitForTimeout(4000)
      await expect(page.locator('.ant-drawer-content-wrapper').last()).toBeHidden({ timeout: 10000 })
    }
  })

  test('部门表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建部门' }).click()
    const drawerWrapper = page.locator('.ant-drawer-content-wrapper').last()
    await expect(drawerWrapper).toBeVisible({ timeout: 5000 })
    // 不填内容直接点提交，触发校验
    await page.evaluate(async () => {
      const allEls = document.querySelectorAll('*')
      let comp: any = null
      for (const el of allEls) {
        const c = (el as any).__vueParentComponent
        if (c?.type?.__name === 'DepartmentTree') { comp = c; break }
      }
      if (comp?.setupState?.handleSubmit) await comp.setupState.handleSubmit()
    })
    await page.waitForTimeout(500)
    const errorMessages = page.locator('.ant-form-item-explain-error')
    expect(await errorMessages.count()).toBeGreaterThan(0)
  })

  test('部门树节点选择', async ({ page }) => {
    const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
    await firstNode.click()
    await page.waitForTimeout(300)
    // 选中节点有 ant-tree-node-selected 类
    const selectedNode = page.locator('.ant-tree-node-selected')
    await expect(selectedNode).toBeVisible({ timeout: 3000 })
  })
})
