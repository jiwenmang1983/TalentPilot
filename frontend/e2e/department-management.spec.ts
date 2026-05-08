import { test, expect } from '@playwright/test'
import { login } from './utils/auth'

test.describe('部门管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page)
    await page.goto('/departments')
    await page.waitForLoadState('domcontentloaded')
  })

  test('部门树形结构展示', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })

    const treeNodes = page.locator('.ant-tree-treenode')
    await expect(treeNodes.first()).toBeVisible()
  })

  test('新增部门', async ({ page }) => {
    await page.getByRole('button', { name: '新建部门' }).click()

    const drawer = page.locator('.ant-drawer')
    await expect(drawer).toBeVisible()
    await expect(drawer.locator('.ant-drawer-title')).toContainText('新建部门')

    const testDeptName = `测试部门_${Date.now()}`
    const testDeptCode = `dept_${Date.now()}`

    await page.getByPlaceholder('请输入部门名称').fill(testDeptName)
    await page.getByPlaceholder('请输入部门代码').fill(testDeptCode)
    await page.getByPlaceholder('请输入描述').fill('自动化测试创建的部门')

    await page.getByRole('button', { name: '确定' }).click()

    await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    await expect(page.locator('.ant-message')).toContainText(/成功/i)
  })

  test('编辑部门', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })

    const treeNode = page.locator('.ant-tree-treenode').first()
    await treeNode.click({ position: { x: 10, y: 10 } })

    await treeNode.click({ button: 'right' })

    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible()) {
      await contextMenu.locator('button', { hasText: '编辑' }).click()

      const drawer = page.locator('.ant-drawer')
      await expect(drawer).toBeVisible()
      await expect(drawer.locator('.ant-drawer-title')).toContainText('编辑部门')

      const nameInput = page.getByPlaceholder('请输入部门名称')
      await nameInput.clear()
      await nameInput.fill(`更新后的部门_${Date.now()}`)

      await drawer.getByRole('button', { name: '确定' }).click()

      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('删除部门', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })

    const treeNode = page.locator('.ant-tree-treenode').first()
    await treeNode.click({ position: { x: 10, y: 10 } })

    await treeNode.click({ button: 'right' })

    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible()) {
      const deleteButton = contextMenu.locator('button', { hasText: '删除' })
      if (await deleteButton.isVisible()) {
        await deleteButton.click()

        const modal = page.locator('.ant-modal').filter({ hasText: /确认删除/ })
        await expect(modal).toBeVisible()
        await modal.locator('button', { hasText: '确定' }).click()

        await page.waitForSelector('.ant-message-success', { timeout: 5000 })
      }
    }
  })

  test('添加子部门', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })

    const treeNode = page.locator('.ant-tree-treenode').first()
    await treeNode.click({ position: { x: 10, y: 10 } })

    await treeNode.click({ button: 'right' })

    await page.waitForTimeout(500)

    const contextMenu = page.locator('.context-menu')
    if (await contextMenu.isVisible()) {
      await contextMenu.locator('button', { hasText: '添加子部门' }).click()

      const drawer = page.locator('.ant-drawer')
      await expect(drawer).toBeVisible()
      await expect(drawer.locator('.ant-drawer-title')).toContainText('添加子部门')

      const testDeptName = `子部门_${Date.now()}`
      const testDeptCode = `child_dept_${Date.now()}`

      await page.getByPlaceholder('请输入部门名称').fill(testDeptName)
      await page.getByPlaceholder('请输入部门代码').fill(testDeptCode)

      await drawer.getByRole('button', { name: '确定' }).click()

      await page.waitForSelector('.ant-message-success', { timeout: 5000 })
    }
  })

  test('拖拽调整部门顺序', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })

    const treeNodes = page.locator('.ant-tree-treenode')
    const nodeCount = await treeNodes.count()

    if (nodeCount >= 2) {
      const sourceNode = treeNodes.nth(0)
      const targetNode = treeNodes.nth(1)

      const sourceBox = await sourceNode.boundingBox()
      const targetBox = await targetNode.boundingBox()

      if (sourceBox && targetBox) {
        await page.mouse.move(sourceBox.x + sourceBox.width / 2, sourceBox.y + sourceBox.height / 2)
        await page.mouse.down()
        await page.mouse.move(targetBox.x + targetBox.width / 2, targetBox.y + targetBox.height / 2, { steps: 10 })
        await page.mouse.up()

        await page.waitForTimeout(1000)
      }
    }
  })

  test('部门表单验证', async ({ page }) => {
    await page.getByRole('button', { name: '新建部门' }).click()

    const drawer = page.locator('.ant-drawer')
    await expect(drawer).toBeVisible()

    await page.getByRole('button', { name: '确定' }).click()

    await page.waitForTimeout(500)

    const errorMessages = page.locator('.ant-form-item-explain-error')
    expect(await errorMessages.count()).toBeGreaterThan(0)
  })

  test('部门树节点选择', async ({ page }) => {
    await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })

    const treeNode = page.locator('.ant-tree-treenode').first()
    await treeNode.click()

    await page.waitForTimeout(300)

    const selectedNode = page.locator('.ant-tree-node-selected')
    await expect(selectedNode).toBeVisible()
  })

  test('选择上级部门', async ({ page }) => {
    await page.getByRole('button', { name: '新建部门' }).click()

    const drawer = page.locator('.ant-drawer')
    await expect(drawer).toBeVisible()

    await page.getByPlaceholder('请输入部门名称').fill(`测试部门_${Date.now()}`)
    await page.getByPlaceholder('请输入部门代码').fill(`dept_${Date.now()}`)

    const parentSelect = page.locator('.ant-tree-select')
    await parentSelect.click()

    await page.waitForTimeout(500)

    const treeNodes = page.locator('.ant-select-tree-treenode')
    if (await treeNodes.count() > 0) {
      await treeNodes.first().click()
    }

    await page.keyboard.press('Escape')
  })
})
