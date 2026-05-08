# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e/department-management.spec.ts >> 部门管理测试 >> 新增部门
- Location: e2e/department-management.spec.ts:20:3

# Error details

```
TimeoutError: page.waitForSelector: Timeout 5000ms exceeded.
Call log:
  - waiting for locator('.ant-message-success') to be visible

```

# Page snapshot

```yaml
- generic [ref=e1]:
  - generic [ref=e3]:
    - complementary [ref=e4]:
      - generic [ref=e5]:
        - generic [ref=e7]: TalentPilot
        - menu [ref=e8]:
          - menuitem "用户管理" [ref=e9] [cursor=pointer]:
            - generic [ref=e10]: 用户管理
          - menuitem "角色管理" [ref=e11] [cursor=pointer]:
            - generic [ref=e12]: 角色管理
          - menuitem "部门管理" [ref=e13] [cursor=pointer]:
            - generic [ref=e14]: 部门管理
          - menuitem "招聘管理" [ref=e15] [cursor=pointer]:
            - generic [ref=e16]: 招聘管理
          - menuitem "数据报表" [ref=e17] [cursor=pointer]:
            - generic [ref=e18]: 数据报表
    - generic [ref=e19]:
      - generic [ref=e20]:
        - img "menu-fold" [ref=e21] [cursor=pointer]:
          - img [ref=e22]
        - generic [ref=e24]:
          - generic [ref=e25]: admin
          - img "user" [ref=e27] [cursor=pointer]:
            - img [ref=e28]
      - main [ref=e30]:
        - generic [ref=e31]:
          - button "plus 新建部门" [ref=e33] [cursor=pointer]:
            - img "plus" [ref=e34]:
              - img [ref=e35]
            - generic [ref=e38]: 新建部门
          - tree [ref=e39]:
            - generic:
              - textbox "for screen reader"
            - generic [ref=e43]:
              - img "file" [ref=e46]:
                - img [ref=e47]
              - img "plus-square" [ref=e54] [cursor=pointer]:
                - img [ref=e55]
  - generic [ref=e63]:
    - generic [ref=e65]:
      - button "Close" [ref=e66] [cursor=pointer]:
        - img "close" [ref=e67]:
          - img [ref=e68]
      - generic [ref=e70]: 新建部门
    - generic [ref=e72]:
      - generic [ref=e74]:
        - generic "部门名称" [ref=e76]: "* 部门名称"
        - textbox "* 部门名称" [ref=e80]:
          - /placeholder: 请输入部门名称
          - text: 测试部门_1778230030987
      - generic [ref=e82]:
        - generic "部门代码" [ref=e84]: "* 部门代码"
        - textbox "* 部门代码" [ref=e88]:
          - /placeholder: 请输入部门代码
          - text: dept_1778230030987
      - generic [ref=e90]:
        - generic "上级部门" [ref=e92]
        - generic [ref=e96] [cursor=pointer]:
          - generic [ref=e97]:
            - combobox "上级部门" [ref=e99]
            - generic: 请选择上级部门
          - generic:
            - img:
              - img
      - generic [ref=e101]:
        - generic "描述" [ref=e103]
        - textbox "描述" [ref=e107]:
          - /placeholder: 请输入描述
          - text: 自动化测试创建的部门
    - generic [ref=e109]:
      - button "取 消" [ref=e111] [cursor=pointer]:
        - generic [ref=e112]: 取 消
      - button "确 定" [active] [ref=e114] [cursor=pointer]:
        - generic [ref=e115]: 确 定
```

# Test source

```ts
  1   | import { test, expect } from '@playwright/test'
  2   | import { login } from './utils/auth'
  3   | 
  4   | test.describe('部门管理测试', () => {
  5   |   test.beforeEach(async ({ page }) => {
  6   |     await login(page)
  7   |     await page.goto('http://127.0.0.1:5173/departments')
  8   |     await page.waitForLoadState('networkidle')
  9   |     // 等待树加载
  10  |     await page.waitForSelector('.ant-tree', { timeout: 10000 })
  11  |   })
  12  | 
  13  |   test('部门树形结构展示', async ({ page }) => {
  14  |     await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 10000 })
  15  |     // 使用可见的节点内容，不要 aria-hidden 的 treenode
  16  |     const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
  17  |     await expect(firstNode).toBeVisible({ timeout: 5000 })
  18  |   })
  19  | 
  20  |   test('新增部门', async ({ page }) => {
  21  |     await page.getByRole('button', { name: '新建部门' }).click()
  22  |     const drawer = page.locator('.ant-drawer').last()
  23  |     await expect(drawer).toBeVisible({ timeout: 5000 })
  24  |     await drawer.waitFor({ state: 'visible' })
  25  | 
  26  |     const testDeptName = `测试部门_${Date.now()}`
  27  |     const testDeptCode = `dept_${Date.now()}`
  28  |     await drawer.getByPlaceholder('请输入部门名称').fill(testDeptName)
  29  |     await drawer.getByPlaceholder('请输入部门代码').fill(testDeptCode)
  30  |     await drawer.getByPlaceholder('请输入描述').fill('自动化测试创建的部门')
  31  | 
  32  |     // 在 drawer 内找确定按钮，force 点击避免 aria-hidden 阻塞
  33  |     await drawer.locator('button.ant-btn-primary').click({ force: true })
> 34  |     await page.waitForSelector('.ant-message-success', { timeout: 5000 })
      |                ^ TimeoutError: page.waitForSelector: Timeout 5000ms exceeded.
  35  |   })
  36  | 
  37  |   test('编辑部门', async ({ page }) => {
  38  |     // 点击第一个树节点
  39  |     const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
  40  |     await firstNode.click()
  41  |     await page.waitForTimeout(300)
  42  |     // 右键菜单
  43  |     await firstNode.click({ button: 'right' })
  44  |     await page.waitForTimeout(500)
  45  | 
  46  |     const contextMenu = page.locator('.context-menu')
  47  |     if (await contextMenu.isVisible().catch(() => false)) {
  48  |       await contextMenu.locator('button', { hasText: '编辑' }).click()
  49  |       const drawer = page.locator('.ant-drawer').last()
  50  |       await expect(drawer).toBeVisible({ timeout: 5000 })
  51  |       await drawer.waitFor({ state: 'visible' })
  52  |       const nameInput = drawer.locator('input').first()
  53  |       await nameInput.clear()
  54  |       await nameInput.fill(`更新后的部门_${Date.now()}`)
  55  |       await drawer.locator('button.ant-btn-primary').click({ force: true })
  56  |       await page.waitForSelector('.ant-message-success', { timeout: 5000 })
  57  |     }
  58  |   })
  59  | 
  60  |   test('删除部门', async ({ page }) => {
  61  |     const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
  62  |     await firstNode.click()
  63  |     await page.waitForTimeout(300)
  64  |     await firstNode.click({ button: 'right' })
  65  |     await page.waitForTimeout(500)
  66  | 
  67  |     const contextMenu = page.locator('.context-menu')
  68  |     if (await contextMenu.isVisible().catch(() => false)) {
  69  |       const deleteButton = contextMenu.locator('button', { hasText: '删除' })
  70  |       if (await deleteButton.isVisible().catch(() => false)) {
  71  |         await deleteButton.click()
  72  |         const modal = page.locator('.ant-modal').filter({ hasText: /确认删除/ })
  73  |         if (await modal.isVisible().catch(() => false)) {
  74  |           await modal.locator('button', { hasText: '确定' }).click({ force: true })
  75  |           await page.waitForSelector('.ant-message-success', { timeout: 5000 })
  76  |         }
  77  |       }
  78  |     }
  79  |   })
  80  | 
  81  |   test('添加子部门', async ({ page }) => {
  82  |     const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
  83  |     await firstNode.click()
  84  |     await page.waitForTimeout(300)
  85  |     await firstNode.click({ button: 'right' })
  86  |     await page.waitForTimeout(500)
  87  | 
  88  |     const contextMenu = page.locator('.context-menu')
  89  |     if (await contextMenu.isVisible().catch(() => false)) {
  90  |       await contextMenu.locator('button', { hasText: '添加子部门' }).click()
  91  |       const drawer = page.locator('.ant-drawer').last()
  92  |       await expect(drawer).toBeVisible({ timeout: 5000 })
  93  |       await drawer.waitFor({ state: 'visible' })
  94  | 
  95  |       const testDeptName = `子部门_${Date.now()}`
  96  |       const testDeptCode = `child_dept_${Date.now()}`
  97  |       await drawer.getByPlaceholder('请输入部门名称').fill(testDeptName)
  98  |       await drawer.getByPlaceholder('请输入部门代码').fill(testDeptCode)
  99  |       await drawer.locator('button.ant-btn-primary').click({ force: true })
  100 |       await page.waitForSelector('.ant-message-success', { timeout: 5000 })
  101 |     }
  102 |   })
  103 | 
  104 |   test('部门表单验证', async ({ page }) => {
  105 |     await page.getByRole('button', { name: '新建部门' }).click()
  106 |     const drawer = page.locator('.ant-drawer').last()
  107 |     await expect(drawer).toBeVisible({ timeout: 5000 })
  108 |     await drawer.waitFor({ state: 'visible' })
  109 |     // 不填内容直接点提交，触发校验
  110 |     await drawer.locator('button.ant-btn-primary').click({ force: true })
  111 |     await page.waitForTimeout(500)
  112 |     const errorMessages = page.locator('.ant-form-item-explain-error')
  113 |     expect(await errorMessages.count()).toBeGreaterThan(0)
  114 |   })
  115 | 
  116 |   test('部门树节点选择', async ({ page }) => {
  117 |     const firstNode = page.locator('.ant-tree-node-content-wrapper').first()
  118 |     await firstNode.click()
  119 |     await page.waitForTimeout(300)
  120 |     // 选中节点有 ant-tree-node-selected 类
  121 |     const selectedNode = page.locator('.ant-tree-node-selected')
  122 |     await expect(selectedNode).toBeVisible({ timeout: 3000 })
  123 |   })
  124 | })
  125 | 
```