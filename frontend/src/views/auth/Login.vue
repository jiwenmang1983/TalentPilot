<template>
  <div class="login-page">
    <!-- Left Panel -->
    <div class="left-panel">
      <img
        class="logo"
        src="/talentpilot-logo.svg"
        alt="TalentPilot Logo"
        @error="e => e.target.style.display='none'"
      >
      <h1 class="title">TalentPilot<br>AI 智能招聘平台</h1>

      <ul class="feature-list">
        <li>
          <span class="check-icon">✓</span>
          <span>AI 智能简历解析与候选人匹配，提升招聘效率</span>
        </li>
        <li>
          <span class="check-icon">✓</span>
          <span>全流程面试管理，结构化评估与协作</span>
        </li>
        <li>
          <span class="check-icon">✓</span>
          <span>多维度数据看板，实时追踪招聘进展</span>
        </li>
        <li>
          <span class="check-icon">✓</span>
          <span>灵活的权限体系，支持多角色协同</span>
        </li>
        <li>
          <span class="check-icon">✓</span>
          <span>自动化工作流，减少重复性操作</span>
        </li>
      </ul>

      <div class="footer">
        © 2026 TalentPilot. All rights reserved.
      </div>
    </div>

    <!-- Right Panel -->
    <div class="right-panel">
      <div class="login-card">
        <h2>统一身份登录</h2>
        <p class="subtitle">通过公司账号密码登录 TalentPilot</p>

        <a-form
          :model="formState"
          :rules="rules"
          @finish="handleLogin"
          layout="vertical"
        >
          <a-form-item label="用户名" name="username">
            <a-input
              v-model:value="formState.username"
              placeholder="请输入用户名"
              size="large"
            >
              <template #prefix>
                <UserOutlined />
              </template>
            </a-input>
          </a-form-item>

          <a-form-item label="密码" name="password">
            <a-input-password
              v-model:value="formState.password"
              placeholder="请输入密码"
              size="large"
            >
              <template #prefix>
                <LockOutlined />
              </template>
            </a-input-password>
          </a-form-item>

          <a-form-item>
            <a-button
              type="primary"
              html-type="submit"
              size="large"
              block
              :loading="loading"
            >
              登 录
            </a-button>
          </a-form-item>
        </a-form>

        <div class="forgot-password">
          忘记密码？联系 <a href="mailto:admin@talentpilot.com">系统管理员</a>
        </div>

        <a-divider />

        <p class="disclaimer">
          登录即代表您同意平台信息安全与数据合规政策。
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { UserOutlined, LockOutlined } from '@ant-design/icons-vue'
import { message } from 'ant-design-vue'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const loading = ref(false)

const formState = reactive({
  username: '',
  password: ''
})

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function handleLogin() {
  loading.value = true
  try {
    await authStore.login(formState.username, formState.password)
    message.success('登录成功')
    router.push('/')
  } catch (error) {
    const errMsg = error?.response?.data?.message || error?.message || '登录失败，请稍后重试'
    message.error(errMsg)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  display: flex;
  height: 100vh;
  width: 100%;
}

.left-panel {
  width: 45%;
  background-color: #0D3D92;
  padding: 48px;
  display: flex;
  flex-direction: column;
  color: #FFFFFF;
  position: relative;
}

.left-panel .logo {
  width: 120px;
  height: auto;
  margin-bottom: 48px;
}

.left-panel .title {
  font-size: 28px;
  font-weight: 600;
  line-height: 1.4;
  margin-bottom: 48px;
  color: #FFFFFF;
}

.feature-list {
  list-style: none;
  flex: 1;
}

.feature-list li {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  font-size: 15px;
  line-height: 1.6;
  color: rgba(255, 255, 255, 0.9);
  margin-bottom: 24px;
}

.check-icon {
  font-size: 18px;
  color: #F5A623;
  flex-shrink: 0;
  margin-top: 2px;
}

.left-panel .footer {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.5);
  border-top: 1px solid rgba(255, 255, 255, 0.15);
  padding-top: 20px;
}

.right-panel {
  width: 55%;
  background-color: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 48px 32px;
}

.login-card {
  width: 100%;
  max-width: 400px;
}

.login-card h2 {
  font-size: 24px;
  font-weight: 600;
  color: #1F2937;
  text-align: center;
  margin-bottom: 8px;
}

.login-card .subtitle {
  font-size: 14px;
  color: #6B7280;
  text-align: center;
  margin-bottom: 32px;
}

.forgot-password {
  text-align: center;
  font-size: 13px;
  color: #6B7280;
  margin-bottom: 20px;
}

.forgot-password a {
  color: #0D3D92;
  text-decoration: none;
}

.forgot-password a:hover {
  text-decoration: underline;
}

.disclaimer {
  font-size: 12px;
  color: #6B7280;
  text-align: center;
  line-height: 1.6;
}

@media (max-width: 768px) {
  .login-page {
    flex-direction: column;
  }

  .left-panel {
    width: 100%;
    min-height: auto;
    padding: 32px 24px;
  }

  .left-panel .title {
    font-size: 22px;
    margin-bottom: 24px;
  }

  .left-panel .logo {
    width: 100px;
    margin-bottom: 24px;
  }

  .feature-list li {
    font-size: 14px;
    margin-bottom: 16px;
  }

  .right-panel {
    width: 100%;
    padding: 32px 24px;
  }
}
</style>
