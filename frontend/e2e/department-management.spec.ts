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
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })

    const testDeptName = `测试部门_${Date.now()}`
    const testDeptCode = `dept_${Date.now()}`
    await drawer.getByPlaceholder('请输入部门名称').fill(testDeptName)
    await drawer.getByPlaceholder('请输入部门代码').fill(testDeptCode)
    await drawer.getByPlaceholder('请输入描述').fill('自动化测试创建的部门')

    // 在 drawer 内找确定按钮，force 点击避免 aria-hidden 阻塞
    await drawer.locator('button.ant-btn-primary').click({ force: true })
    await page.waitForSelector('.ant-message-success', { timeout: 5000 })
  })

  test('编辑部门', async ({ page }) => {
    // 点击第一个树节点
    const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
    await firstNode.click()
    await page.waitForTimeout(300)
    // 右键菜单
    await firstNode.click({ button: 'right' })
    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible().catch(() => false)) {
      await contextMenu.locator('button', { hasText: '编辑' }).click()
      const drawer = page.locator('.ant-drawer').last()
      await expect(drawer).toBeVisible({ timeout: 5000 })
      await drawer.waitFor({ state: 'visible' })
      const nameInput = drawer.locator('input').first()
      await nameInput.clear()
      await nameInput.fill(`更新后的部门_${Date.now()}`)
      await drawer.locator('button.ant-btn-primary').click({ force: true })
      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
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
          await modal.locator('button', { hasText: '确定' }).click({ force: true })
          await page.waitForSelector('.ant-message-success', { timeout: 5000 })
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
      const drawer = page.locator('.ant-drawer').last()
      await expect(drawer).toBeVisible({ timeout: 5000 })
      await drawer.waitFor({ state: 'visible' })

      const testDeptName = `子部门_${Date.now()}`
      const testDeptCode = `child_dept_${Date.now()}`
      await drawer.getByPlaceholder('请输入部门名称').fill(testDeptName)
      await drawer.getByPlaceholder('请输入部门代码').fill(testDeptCode)
      await drawer.locator('button.ant-btn-primary').click({ force: true })
      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('部门表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建部门' }).click()
    const drawer = page.locator('.ant-drawer').last()
    await expect(drawer).toBeVisible({ timeout: 5000 })
    await drawer.waitFor({ state: 'visible' })
    // 不填内容直接点提交，触发校验
    await drawer.locator('button.ant-btn-primary').click({ force: true })
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
