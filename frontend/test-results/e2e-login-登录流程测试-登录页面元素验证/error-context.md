# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e/login.spec.ts >> 登录流程测试 >> 登录页面元素验证
- Location: e2e/login.spec.ts:79:3

# Error details

```
Error: expect(locator).toContainText(expected) failed

Locator: locator('.login-subtitle')
Expected substring: "人才管理系统"
Timeout: 5000ms
Error: element(s) not found

Call log:
  - Expect "toContainText" with timeout 5000ms
  - waiting for locator('.login-subtitle')

```

# Page snapshot

```yaml
- generic [ref=e5]:
  - generic [ref=e6]:
    - heading "TalentPilot" [level=1] [ref=e7]
    - paragraph [ref=e8]: AI 智能招聘平台
  - generic [ref=e9]:
    - generic [ref=e11]:
      - generic "用户名" [ref=e13]: "* 用户名"
      - generic [ref=e17]:
        - img "user" [ref=e19]:
          - img [ref=e20]
        - textbox "* 用户名" [ref=e22]:
          - /placeholder: 请输入用户名
    - generic [ref=e24]:
      - generic "密码" [ref=e26]: "* 密码"
      - generic [ref=e30]:
        - img "lock" [ref=e32]:
          - img [ref=e33]
        - textbox "* 密码" [ref=e35]:
          - /placeholder: 请输入密码
        - img "eye-invisible" [ref=e37] [cursor=pointer]:
          - img [ref=e38]
    - link "忘记密码？" [ref=e42] [cursor=pointer]:
      - /url: "#"
    - button "登 录" [ref=e43] [cursor=pointer]:
      - generic [ref=e44]: 登 录
  - generic [ref=e45]: © 2026 TalentPilot 版权所有
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test'
  2  | import { clearAuth, getToken, login, TEST_USERNAME, TEST_PASSWORD } from './utils/auth'
  3  | 
  4  | const BASE = 'http://127.0.0.1:5173'
  5  | 
  6  | test.describe('登录流程测试', () => {
  7  |   test.beforeEach(async ({ page }) => {
  8  |     await clearAuth(page)
  9  |   })
  10 | 
  11 |   test('用户名+密码正确登录 -> 进入首页', async ({ page }) => {
  12 |     await page.goto(`${BASE}/login`)
  13 |     await page.waitForLoadState('domcontentloaded')
  14 | 
  15 |     await expect(page.locator('h1')).toContainText('TalentPilot')
  16 | 
  17 |     await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
  18 |     await page.getByPlaceholder('请输入密码').fill(TEST_PASSWORD)
  19 |     await page.locator('button[type="submit"]').click()
  20 | 
  21 |     await page.waitForURL('**/users', { timeout: 15000 })
  22 | 
  23 |     await expect(page).toHaveURL(/\/users/)
  24 |   })
  25 | 
  26 |   test('用户名+密码错误 -> 显示错误提示', async ({ page }) => {
  27 |     await page.goto(`${BASE}/login`)
  28 |     await page.waitForLoadState('domcontentloaded')
  29 | 
  30 |     await page.getByPlaceholder('请输入用户名').fill('wronguser')
  31 |     await page.getByPlaceholder('请输入密码').fill('wrongpassword')
  32 |     await page.locator('button[type="submit"]').click()
  33 | 
  34 |     await page.waitForSelector('.ant-message-error', { timeout: 5000 })
  35 | 
  36 |     await expect(page.locator('.ant-message')).toContainText(/失败|错误|incorrect|invalid/i)
  37 | 
  38 |     await expect(page).toHaveURL(/\/login/)
  39 |   })
  40 | 
  41 |   test.skip('登录失败5次后锁定 -> 显示锁定倒计时', async ({ page }) => {
  42 |     // 注意：此测试需要独立测试账号，避免锁定主账号影响其他测试
  43 |     // TODO: 创建专用测试账号 test_lock@talentpilot.com 后重新启用
  44 |     await page.goto(`${BASE}/login`)
  45 |     await page.waitForLoadState('domcontentloaded')
  46 | 
  47 |     const wrongPassword = 'TalentPilot2026_Wrong'
  48 |     for (let i = 0; i < 5; i++) {
  49 |       await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
  50 |       await page.getByPlaceholder('请输入密码').fill(wrongPassword)
  51 |       await page.locator('button[type="submit"]').click()
  52 |       await page.waitForTimeout(500)
  53 |     }
  54 | 
  55 |     const lockMessage = page.locator('.ant-message').filter({ hasText: /锁定|lock|请.*秒|等待/i })
  56 |     await expect(lockMessage).toBeVisible({ timeout: 5000 })
  57 |   })
  58 | 
  59 |   test('JWT token 存储验证', async ({ page }) => {
  60 |     await page.goto(`${BASE}/login`)
  61 |     await page.waitForLoadState('domcontentloaded')
  62 | 
  63 |     await page.getByPlaceholder('请输入用户名').fill(TEST_USERNAME)
  64 |     await page.getByPlaceholder('请输入密码').fill(TEST_PASSWORD)
  65 |     await page.locator('button[type="submit"]').click()
  66 | 
  67 |     await page.waitForURL('**/users', { timeout: 15000 })
  68 | 
  69 |     const token = await getToken(page)
  70 |     expect(token).toBeTruthy()
  71 |     expect(token!.length).toBeGreaterThan(20)
  72 | 
  73 |     await page.evaluate((t) => {
  74 |       const stored = localStorage.getItem('accessToken')
  75 |       return stored === t
  76 |     }, token!)
  77 |   })
  78 | 
  79 |   test('登录页面元素验证', async ({ page }) => {
  80 |     await page.goto(`${BASE}/login`)
  81 |     await page.waitForLoadState('domcontentloaded')
  82 | 
  83 |     await expect(page.locator('h1')).toContainText('TalentPilot')
> 84 |     await expect(page.locator('.login-subtitle')).toContainText('人才管理系统')
     |                                                   ^ Error: expect(locator).toContainText(expected) failed
  85 |     await expect(page.getByPlaceholder('请输入用户名')).toBeVisible()
  86 |     await expect(page.getByPlaceholder('请输入密码')).toBeVisible()
  87 |     await expect(page.locator('button[type="submit"]')).toBeVisible()
  88 |   })
  89 | 
  90 |   test('未登录用户访问受保护页面重定向到登录页', async ({ page }) => {
  91 |     await clearAuth(page)
  92 |     await page.goto(`${BASE}/users`)
  93 |     await page.waitForURL(/\/login/)
  94 | 
  95 |     await expect(page).toHaveURL(/\/login/)
  96 |   })
  97 | })
  98 | 
```