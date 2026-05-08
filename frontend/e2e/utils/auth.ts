import { test as base, Page } from '@playwright/test'

const TEST_USERNAME = 'admin'
const TEST_PASSWORD = 'TalentPilot2026'

export { TEST_USERNAME, TEST_PASSWORD }

export async function login(page: Page, username: string = TEST_USERNAME, password: string = TEST_PASSWORD): Promise<void> {
  await page.goto('/login')
  await page.waitForLoadState('networkidle')

  await page.getByPlaceholder('请输入用户名').fill(username)
  await page.getByPlaceholder('请输入密码').fill(password)
  await page.getByRole('button', { name: '登录' }).click()

  await page.waitForURL('**/users', { timeout: 10000 })
}

export async function logout(page: Page): Promise<void> {
  await page.evaluate(() => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('userInfo')
  })
  await page.goto('/login')
}

export async function clearAuth(page: Page): Promise<void> {
  await page.evaluate(() => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('userInfo')
  })
}

export async function getToken(page: Page): Promise<string | null> {
  return page.evaluate(() => localStorage.getItem('accessToken'))
}

export async function setToken(page: Page, token: string): Promise<void> {
  await page.evaluate((t) => {
    localStorage.setItem('accessToken', t)
  }, token)
}

export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ page }, use) => {
    await login(page)
    await use(page)
  },
})
